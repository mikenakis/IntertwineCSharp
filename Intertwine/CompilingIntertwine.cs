//Author MikeNakis (michael.gr)

#nullable enable
namespace MikeNakis.Intertwine
{
	using System.Collections.Generic;
	using System.Linq;
	using Sys = System;
	using SysIo = System.IO;
	using SysDiag = System.Diagnostics;
	using SysReflect = System.Reflection;
	using SysReflectEmit = System.Reflection.Emit;

	///<summary>An implementation of <see cref="IntertwineFactory" /> which uses code generation.</summary>
	///<author>michael.gr</author>
	public class CompilingIntertwineFactory : IntertwineFactory
	{
		private delegate Entwiner EntwinerFactory( AnyCall any_call );

		private delegate Untwiner UntwinerFactory( object target );

		public bool IsSaving { get; set; } = false;

		public Intertwine<T> GetIntertwine<T>() where T : class
		{
			return new CompilingIntertwine<T>( IsSaving );
		}

		private class CompilingMethodKey : MethodKey
		{
			private readonly CompilingIntertwine compiling_intertwine;
			private readonly int method_index;
			private readonly SysReflect.MethodInfo method_info;

			protected CompilingMethodKey( CompilingIntertwine compiling_intertwine, int method_index, SysReflect.MethodInfo method_info )
			{
				this.compiling_intertwine = compiling_intertwine;
				this.method_index = method_index;
				this.method_info = method_info;
			}

			public Intertwine Intertwine() => compiling_intertwine;

			public int MethodIndex() => method_index;

			public SysReflect.MethodInfo MethodInfo() => method_info;
		}

		private sealed class CompilingMethodKey<T> : CompilingMethodKey, MethodKey<T> where T : class
		{
			public CompilingMethodKey( CompilingIntertwine compiling_intertwine, int method_index, SysReflect.MethodInfo method_info )
					: base( compiling_intertwine, method_index, method_info )
			{ }

			public new Intertwine<T> Intertwine() => (Intertwine<T>)base.Intertwine();
		}

		private abstract class CompilingIntertwine : Intertwine
		{
			private readonly EntwinerFactory entwiner_factory;
			private readonly UntwinerFactory untwiner_factory;
			private Sys.Type interface_type { get; }
			protected readonly SysReflect.MethodInfo[] MethodInfos;
			public Sys.Type InterfaceType => interface_type;
			public abstract IReadOnlyList<MethodKey> MethodKeys { get; }
			public abstract MethodKey MethodKeyByMethodInfo( SysReflect.MethodInfo method_info );

			protected CompilingIntertwine( bool save, Sys.Type interface_type )
			{
				this.interface_type = interface_type;

				Dbg.Assert( interface_type.IsInterface );

				Sys.Type[] interfaces = collect_interfaces( interface_type );
				MethodInfos = collect_method_infos( interfaces );
				SysReflect.ParameterInfo[][] parameter_infos = new SysReflect.ParameterInfo[MethodInfos.Length][];
				for( int i = 0; i < MethodInfos.Length; i++ )
					parameter_infos[i] = MethodInfos[i].GetParameters();

				string name = Dbg.NotNull( interface_type.FullName ) //
						.Replace( "[", "" )
						.Replace( "]", "" )
						.Replace( " ", "" )
						.Replace( ",", "" )
						.Replace( "mscorlib", "" )
						.Replace( "Culture=neutral", "" )
						.Replace( "PublicKeyToken=", "" )
						.Replace( "Version=", "_" );
				using( ModuleBuilderWrapper module_builder_wrapper = get_module_builder_wrapper( save, name ) )
				{
					entwiner_factory = create_entwiner( module_builder_wrapper.ModuleBuilder, interface_type, MethodInfos, parameter_infos, interfaces );
					untwiner_factory = create_untwiner( module_builder_wrapper.ModuleBuilder, interface_type, MethodInfos, parameter_infos );
				}
			}

			private abstract class ModuleBuilderWrapper : Sys.IDisposable
			{
				public abstract SysReflectEmit.ModuleBuilder ModuleBuilder { get; }
				public abstract void Dispose();

				protected static void MakeDebuggable( SysReflectEmit.AssemblyBuilder assembly_builder )
				{
					// Mark generated code as debuggable. See http://blogs.msdn.com/rmbyers/archive/2005/06/26/432922.aspx for explanation.
					SysReflect.ConstructorInfo constructor_info_for_debuggable_attribute = Dbg.NotNull( typeof(SysDiag.DebuggableAttribute).GetConstructor( new[] { typeof(SysDiag.DebuggableAttribute.DebuggingModes) } ) );
					SysReflectEmit.CustomAttributeBuilder attribute_builder_for_debuggable = new SysReflectEmit.CustomAttributeBuilder( constructor_info_for_debuggable_attribute, new object[] { SysDiag.DebuggableAttribute.DebuggingModes.DisableOptimizations | SysDiag.DebuggableAttribute.DebuggingModes.Default } );
					assembly_builder.SetCustomAttribute( attribute_builder_for_debuggable );
				}
			}

			private sealed class NetFrameworkCompatibleModuleBuilderWrapper : ModuleBuilderWrapper
			{
				private readonly bool save;
				public override SysReflectEmit.ModuleBuilder ModuleBuilder { get; }

				public NetFrameworkCompatibleModuleBuilderWrapper( bool save, string name )
				{
					this.save = save;
					string output_filename = name + ".dll";
					SysReflect.AssemblyName assembly_name = new SysReflect.AssemblyName( "AssemblyFor" + name );
					SysReflectEmit.AssemblyBuilder assembly_builder = Sys.AppDomain.CurrentDomain.DefineDynamicAssembly( assembly_name, save ? SysReflectEmit.AssemblyBuilderAccess.RunAndSave : SysReflectEmit.AssemblyBuilderAccess.Run, dir: null );
					MakeDebuggable( assembly_builder );
					ModuleBuilder = save ? assembly_builder.DefineDynamicModule( assembly_name.Name, output_filename, emitSymbolInfo: true ) : assembly_builder.DefineDynamicModule( assembly_name.Name );
				}

				public override void Dispose()
				{
					if( save )
					{
						var assembly_builder = (SysReflectEmit.AssemblyBuilder)ModuleBuilder.Assembly;
						assembly_builder.Save( SysIo.Path.GetFileName( ModuleBuilder.FullyQualifiedName ) );
					}
				}
			}

			private sealed class NetCoreCompatibleModuleBuilderWrapper : ModuleBuilderWrapper
			{
				public override SysReflectEmit.ModuleBuilder ModuleBuilder { get; }

				public NetCoreCompatibleModuleBuilderWrapper( string name )
				{
					SysReflect.AssemblyName assembly_name = new SysReflect.AssemblyName( "AssemblyFor" + name );
					SysReflectEmit.AssemblyBuilder assembly_builder = SysReflectEmit.AssemblyBuilder.DefineDynamicAssembly( assembly_name, SysReflectEmit.AssemblyBuilderAccess.Run );
					MakeDebuggable( assembly_builder );
					ModuleBuilder = assembly_builder.DefineDynamicModule( assembly_name.Name );
				}

				public override void Dispose()
				{
					/* nothing to do */
				}
			}

			private static ModuleBuilderWrapper get_module_builder_wrapper( bool save, string name )
			{
				if( save )
				{
					if( is_net_core() )
						throw new Sys.InvalidOperationException(); //you cannot save in net core.
					return new NetFrameworkCompatibleModuleBuilderWrapper( save, name );
				}
				else
					return new NetCoreCompatibleModuleBuilderWrapper( name );
			}

			private static bool is_net_core() => typeof(SysReflectEmit.AssemblyBuilder).GetMethod( "Save", new[] { typeof(string) } ) == null;

			public object NewEntwiner( AnyCall any_call )
			{
				Entwiner entwiner = entwiner_factory.Invoke( any_call );
				Dbg.Assert( entwiner.AnyCall == any_call );
				return entwiner;
			}

			public AnyCall NewUntwiner( object target )
			{
				Untwiner untwiner = untwiner_factory.Invoke( target );
				Dbg.Assert( untwiner.InterfaceType == interface_type );
				return untwiner.AnyCall;
			}

			private static readonly SysReflect.MethodInfo method_info_for_get_type_from_handle = Dbg.NotNull( typeof(Sys.Type).GetMethod( "GetTypeFromHandle", new[] { typeof(Sys.RuntimeTypeHandle) } ) );

			private static readonly SysReflect.ConstructorInfo constructor_info_for_entwiner = Dbg.NotNull( typeof(Entwiner).GetConstructor( SysReflect.BindingFlags.NonPublic | SysReflect.BindingFlags.Instance, null, SysReflect.CallingConventions.Any, new[] { typeof(Sys.Type), typeof(AnyCall) }, null ) );
			private static readonly SysReflect.MethodInfo method_info_for_any_call_invoke = Dbg.NotNull( typeof(AnyCall).GetMethod( "Invoke", new[] { typeof(int), typeof(object[]) } ) );
			private static readonly SysReflect.FieldInfo field_info_for_entwiner_any_call = Dbg.NotNull( typeof(Entwiner).GetField( "AnyCall" ) );

			private static readonly SysReflect.ConstructorInfo constructor_info_for_untwiner = Dbg.NotNull( typeof(Untwiner).GetConstructor( SysReflect.BindingFlags.NonPublic | SysReflect.BindingFlags.Instance, null, SysReflect.CallingConventions.Any, new[] { typeof(Sys.Type) }, null ) );
			private static readonly SysReflect.MethodInfo method_info_for_untwiner_any_call = Dbg.NotNull( typeof(Untwiner).GetMethod( "AnyCall" ) );
			private static readonly SysReflect.ConstructorInfo constructor_info_for_invalid_operation_exception = Dbg.NotNull( typeof(Sys.InvalidOperationException).GetConstructor( Sys.Type.EmptyTypes ) );

			private const string factory_method_name = "Factory";

			private const SysReflect.TypeAttributes attributes_for_type = SysReflect.TypeAttributes.AutoClass | SysReflect.TypeAttributes.Class | SysReflect.TypeAttributes.Public | SysReflect.TypeAttributes.Sealed | SysReflect.TypeAttributes.BeforeFieldInit;
			private const SysReflect.MethodAttributes attributes_for_constructor = SysReflect.MethodAttributes.Public | SysReflect.MethodAttributes.HideBySig | SysReflect.MethodAttributes.SpecialName | SysReflect.MethodAttributes.RTSpecialName;

			private static EntwinerFactory create_entwiner( SysReflectEmit.ModuleBuilder module_builder, Sys.Type interface_type, IReadOnlyList<SysReflect.MethodInfo> method_infos, SysReflect.ParameterInfo[][] parameter_infos_s, Sys.Type[] interfaces )
			{
				// Start creating the type
				SysReflectEmit.TypeBuilder type_builder = module_builder.DefineType( "EntwinerFor" + interface_type.Name, attributes_for_type, typeof(Entwiner), interfaces );

				// Create the constructor
				SysReflectEmit.ConstructorBuilder builder_for_constructor = type_builder.DefineConstructor( attributes_for_constructor, SysReflect.CallingConventions.Standard, new[] { typeof(AnyCall) } );
				{
					SysReflectEmit.ILGenerator gen = builder_for_constructor.GetILGenerator();
					gen.Emit( SysReflectEmit.OpCodes.Ldarg_0 );
					gen.Emit( SysReflectEmit.OpCodes.Ldtoken, interface_type );
					gen.Emit( SysReflectEmit.OpCodes.Call, method_info_for_get_type_from_handle );
					gen.Emit( SysReflectEmit.OpCodes.Ldarg_1 );
					gen.Emit( SysReflectEmit.OpCodes.Call, constructor_info_for_entwiner );
					gen.Emit( SysReflectEmit.OpCodes.Ret );
				}

				// Create each method
				for( int selector = 0; selector < method_infos.Count; selector++ )
				{
					SysReflect.MethodInfo? method_info = method_infos[selector];

					// Get parameter infos
					SysReflect.ParameterInfo[] parameter_infos = parameter_infos_s[selector];

					// Define method and get method builder
					SysReflectEmit.MethodBuilder method_builder = type_builder.DefineMethod( method_info.Name, SysReflect.MethodAttributes.Private | SysReflect.MethodAttributes.Virtual | SysReflect.MethodAttributes.Final | SysReflect.MethodAttributes.HideBySig | SysReflect.MethodAttributes.NewSlot, //can someone explain to me why NewSlot works here?
							SysReflect.CallingConventions.HasThis, method_info.ReturnType, types_from_parameter_infos( parameter_infos ) );
					type_builder.DefineMethodOverride( method_builder, method_info );

					// Define generic parameters, if any
					Sys.Type[] generic_arguments = method_info.GetGenericArguments();
					if( generic_arguments.Length > 0 )
					{
						string[] generic_type_names = new string[generic_arguments.Length];
						for( int index = 0; index < generic_arguments.Length; index++ )
							generic_type_names[index] = "T" + index;
						method_builder.DefineGenericParameters( generic_type_names );
					}

					// Get the code generator
					SysReflectEmit.ILGenerator gen = method_builder.GetILGenerator();

					// Emit code to allocate the array
					SysReflectEmit.LocalBuilder local_builder_for_array = gen.DeclareLocal( typeof(object[]) );
					//local_builder_for_array.SetLocalSymInfo( "args" );
					emit_ldc_i4( gen, parameter_infos.Length );
					gen.Emit( SysReflectEmit.OpCodes.Newarr, typeof(object) );
					gen.Emit( SysReflectEmit.OpCodes.Stloc, local_builder_for_array );

					// Emit code to copy each argument to the array
					foreach( var parameter_info in parameter_infos )
					{
						// Emit code to select the current element of the array (used by the store-element instruction in the end)
						gen.Emit( SysReflectEmit.OpCodes.Ldloc, local_builder_for_array );
						emit_ldc_i4( gen, parameter_info.Position );

						// If the argument is passed with 'ref'...
						Sys.Type actual_parameter_type;
						if( parameter_info.ParameterType.IsByRef )
						{
							actual_parameter_type = Dbg.NotNull( parameter_info.ParameterType.GetElementType() );

							// If this is an 'out' argument...
							if( parameter_info.IsOut )
							{
								//Emit code to load a zero in its place.
								gen.Emit( SysReflectEmit.OpCodes.Ldnull );
							}
							// Otherwise, this is not an 'out' argument, so...
							else
							{
								// Emit code to get the address of the argument
								gen.Emit( SysReflectEmit.OpCodes.Ldarg, parameter_info.Position + 1 );

								// Emit code to fetch the argument from its address
								gen.Emit( SysReflectEmit.OpCodes.Ldobj, actual_parameter_type );
							}
						}
						else
						{
							actual_parameter_type = Dbg.NotNull( parameter_info.ParameterType );

							// Emit code to get the value of the argument
							gen.Emit( SysReflectEmit.OpCodes.Ldarg, parameter_info.Position + 1 );
						}

						// If the argument is a value type, box it.
						if( actual_parameter_type.IsValueType )
							gen.Emit( SysReflectEmit.OpCodes.Box, actual_parameter_type );

						// Store the argument in the array.
						gen.Emit( SysReflectEmit.OpCodes.Stelem_Ref );
					}

					// Emit code to invoke AnyCall
					gen.Emit( SysReflectEmit.OpCodes.Ldarg_0 );
					gen.Emit( SysReflectEmit.OpCodes.Ldfld, field_info_for_entwiner_any_call );
					emit_ldc_i4( gen, selector );
					gen.Emit( SysReflectEmit.OpCodes.Ldloc_S, local_builder_for_array );
					gen.Emit( SysReflectEmit.OpCodes.Callvirt, method_info_for_any_call_invoke );

					// Emit code to update any arguments that were passed by ref
					foreach( SysReflect.ParameterInfo parameter_info in parameter_infos )
					{
						// If this argument was not passed by ref, skip it.
						if( !parameter_info.ParameterType.IsByRef )
							continue;

						// Get actual type of argument
						var actual_parameter_type = Dbg.NotNull( parameter_info.ParameterType.GetElementType() );

						// Emit code to load the address of the argument (will be used later by the store instruction.)
						gen.Emit( SysReflectEmit.OpCodes.Ldarg, parameter_info.Position + 1 );

						// Emit code to load the value from the array
						gen.Emit( SysReflectEmit.OpCodes.Ldloc_0 );
						emit_ldc_i4( gen, parameter_info.Position );
						gen.Emit( SysReflectEmit.OpCodes.Ldelem_Ref );

						// If this is a value type...
						if( actual_parameter_type.IsValueType )
						{
							// Emit code to unbox the value and store it back into the argument
							gen.Emit( SysReflectEmit.OpCodes.Unbox, actual_parameter_type );
							gen.Emit( SysReflectEmit.OpCodes.Cpobj, actual_parameter_type );
						}
						// Otherwise, this is a reference type, so...
						else
						{
							// Emit code to store the value back into the argument
							gen.Emit( SysReflectEmit.OpCodes.Stind_Ref );
						}
					}

					// If the method has no return value...
					if( method_info.ReturnType == typeof(void) )
					{
						// Emit code to discard the return value from AnyCall.
						gen.Emit( SysReflectEmit.OpCodes.Pop );
					}
					// Otherwise, the method has a return value, so...
					else
					{
						// Emit code to unbox the return value.
						gen.Emit( SysReflectEmit.OpCodes.Unbox_Any, method_info.ReturnType );
					}

					// Emit return instruction.
					gen.Emit( SysReflectEmit.OpCodes.Ret );
				}

				// Create the factory method
				{
					// Define the method.
					SysReflectEmit.MethodBuilder method_builder_for_factory = type_builder.DefineMethod( factory_method_name, SysReflect.MethodAttributes.Public | SysReflect.MethodAttributes.Static | SysReflect.MethodAttributes.Final, SysReflect.CallingConventions.Standard, typeof(Entwiner), new[] { typeof(AnyCall) } );
					SysReflectEmit.ILGenerator gen = method_builder_for_factory.GetILGenerator();

					// Emit code to load the one argument that we have been passed
					gen.Emit( SysReflectEmit.OpCodes.Ldarg_0 );

					// Emit code to create a new object.
					gen.Emit( SysReflectEmit.OpCodes.Newobj, builder_for_constructor );

					// Emit return instruction.
					gen.Emit( SysReflectEmit.OpCodes.Ret );
				}

				// Create the type.
				Sys.Type entwiner = type_builder.CreateType();

				// Find and return the factory.
				// Note: we cannot use the method builder here because it wants a 'runtime method info'.
				SysReflect.MethodInfo factory_method_info = Dbg.NotNull( entwiner.GetMethod( factory_method_name, SysReflect.BindingFlags.Public | SysReflect.BindingFlags.Static ) );
				return (EntwinerFactory)Sys.Delegate.CreateDelegate( typeof(EntwinerFactory), factory_method_info );
			}

			private static UntwinerFactory create_untwiner( SysReflectEmit.ModuleBuilder module_builder, Sys.Type interface_type, IReadOnlyList<SysReflect.MethodInfo> method_infos, IReadOnlyList<SysReflect.ParameterInfo[]> parameter_infos_s )
			{
				// Start creating the type
				SysReflectEmit.TypeBuilder type_builder = module_builder.DefineType( "UntwinerFor" + interface_type.Name, attributes_for_type, typeof(Untwiner) );

				// Create the _Target field
				SysReflectEmit.FieldBuilder field_builder_for_target = type_builder.DefineField( "_Target", interface_type, SysReflect.FieldAttributes.Private | SysReflect.FieldAttributes.InitOnly );

				// Create the constructor
				SysReflectEmit.ConstructorBuilder constructor_builder = type_builder.DefineConstructor( attributes_for_constructor, SysReflect.CallingConventions.Standard, new[] { typeof(object) } );
				{
					SysReflectEmit.ILGenerator gen = constructor_builder.GetILGenerator();
					gen.Emit( SysReflectEmit.OpCodes.Ldarg_0 );
					gen.Emit( SysReflectEmit.OpCodes.Ldtoken, interface_type );
					gen.Emit( SysReflectEmit.OpCodes.Call, method_info_for_get_type_from_handle );
					gen.Emit( SysReflectEmit.OpCodes.Call, constructor_info_for_untwiner );
					gen.Emit( SysReflectEmit.OpCodes.Ldarg_0 );
					gen.Emit( SysReflectEmit.OpCodes.Ldarg_1 );
					gen.Emit( SysReflectEmit.OpCodes.Stfld, field_builder_for_target );
					gen.Emit( SysReflectEmit.OpCodes.Ret );
				}

				// Create the AnyCall method
				SysReflectEmit.MethodBuilder method_builder_for_any_call = type_builder.DefineMethod( "AnyCall", SysReflect.MethodAttributes.Public | SysReflect.MethodAttributes.Virtual | SysReflect.MethodAttributes.Final, SysReflect.CallingConventions.HasThis, typeof(object), new[] { typeof(int), typeof(object[]) } );
				type_builder.DefineMethodOverride( method_builder_for_any_call, method_info_for_untwiner_any_call ); //XXX unnecessary?
				{
					// Get the IL generator
					SysReflectEmit.ILGenerator gen = method_builder_for_any_call.GetILGenerator();

					// Define one label for each target of the switch statement.
					SysReflectEmit.Label[] labels = new SysReflectEmit.Label[method_infos.Count];
					for( int i = 0; i < method_infos.Count; i++ )
						labels[i] = gen.DefineLabel();

					// Optimization: the following is common to all case statements, so it was moved before the switch statement.
					// Emit code to load the _Target field.
					gen.Emit( SysReflectEmit.OpCodes.Ldarg_0 );
					gen.Emit( SysReflectEmit.OpCodes.Ldfld, field_builder_for_target );

					// Emit a switch statement on argument 1 (selector)
					gen.Emit( SysReflectEmit.OpCodes.Ldarg_1 );
					gen.Emit( SysReflectEmit.OpCodes.Switch, labels );

					// Emit code for switch statement fall-through (the selector was invalid): throw invalid operation exception.
					gen.Emit( SysReflectEmit.OpCodes.Newobj, constructor_info_for_invalid_operation_exception );
					gen.Emit( SysReflectEmit.OpCodes.Throw );

					// Emit one block of code for each method of the interface being untwined.
					for( int selector = 0; selector < method_infos.Count; selector++ )
					{
						// Mark the label so that the switch statement can branch here.
						gen.MarkLabel( labels[selector] );

						// Emit code to pass each parameter to the method
						SysReflect.MethodInfo method_info = method_infos[selector];
						foreach( var parameter_info in parameter_infos_s[selector] )
						{
							// Emit code to select the current parameter in the array
							gen.Emit( SysReflectEmit.OpCodes.Ldarg_2 );
							emit_ldc_i4( gen, parameter_info.Position );

							// If this parameter is passed by ref or out...
							if( parameter_info.ParameterType.IsByRef )
							{
								// If this is a value-type...
								Sys.Type actual_parameter_type = Dbg.NotNull( parameter_info.ParameterType.GetElementType() );
								if( actual_parameter_type.IsValueType )
								{
									// Emit code to load the element from the array and unbox it. This is a nifty little optimization:
									// instead of unboxing the value, storing it into a local variable, passing the local variable byref
									// to the interface method, and then re-boxing the local variable back into the array, discarding the
									// old box, we simply issue a single unbox op-code, which, according to MSDN, “[…] is not required to
									// copy the value type from the object. Typically it simply computes the address of the value type
									// that is already present inside of the boxed object.” Thus, after executing unbox, the reference
									// to the boxed value type is ready in the evaluation stack for the target interface method to
									// operate upon. In this case, when the method is going to be assigning a value to the byref argument,
									// it will be altering the actual contents of the boxing object, something which is quite impossible
									// at the C# level!
									gen.Emit( SysReflectEmit.OpCodes.Ldelem_Ref );
									gen.Emit( SysReflectEmit.OpCodes.Unbox, actual_parameter_type );
								}
								// Otherwise, if this is a reference-type...
								else
								{
									// Emit code to load the address of the array element.
									gen.Emit( SysReflectEmit.OpCodes.Ldelema, typeof(object) );
								}
							}
							// Otherwise, if this parameter is a value-type...
							else if( parameter_info.ParameterType.IsValueType )
							{
								// Emit code to load the element from the array and unbox it.
								gen.Emit( SysReflectEmit.OpCodes.Ldelem_Ref );
								gen.Emit( SysReflectEmit.OpCodes.Unbox_Any, parameter_info.ParameterType );
							}
							// Otherwise, this parameter is a reference-type, so...
							else
							{
								// Emit code to load the element from the array.
								gen.Emit( SysReflectEmit.OpCodes.Ldelem_Ref );
							}
						}

						// Emit a call to the method of the interface
						gen.Emit( SysReflectEmit.OpCodes.Callvirt, method_info );

						// If this method does not return anything...
						if( method_info.ReturnType == typeof(void) )
						{
							// We must return something, so emit code to load a null.
							gen.Emit( SysReflectEmit.OpCodes.Ldnull );
						}
						// Otherwise, this method returns something, so... */
						else
						{
							// If the return type is a value type...
							if( method_info.ReturnType.IsValueType )
							{
								// We always return an object, so emit code to box the return value.
								gen.Emit( SysReflectEmit.OpCodes.Box, method_info.ReturnType );
							}
						}

						// Emit return statement.
						gen.Emit( SysReflectEmit.OpCodes.Ret );
					}
				}

				// Create the factory method
				{
					// Define the method.
					SysReflectEmit.MethodBuilder method_builder_for_factory = type_builder.DefineMethod( factory_method_name, SysReflect.MethodAttributes.Public | SysReflect.MethodAttributes.Static | SysReflect.MethodAttributes.Final, SysReflect.CallingConventions.Standard, typeof(Untwiner), new[] { typeof(object) } );
					SysReflectEmit.ILGenerator gen = method_builder_for_factory.GetILGenerator();

					// Emit code to load the single argument that we have been given.
					gen.Emit( SysReflectEmit.OpCodes.Ldarg_0 );

					// Emit code create a new object.
					gen.Emit( SysReflectEmit.OpCodes.Newobj, constructor_builder );

					// Emit return instruction.
					gen.Emit( SysReflectEmit.OpCodes.Ret );
				}

				// Create the type.
				Sys.Type untwiner = type_builder.CreateType();

				// Find and return the factory.
				// Note: we cannot use the method builder here because it wants a 'runtime method info'.
				SysReflect.MethodInfo factory_method_info = Dbg.NotNull( untwiner.GetMethod( factory_method_name, SysReflect.BindingFlags.Public | SysReflect.BindingFlags.Static ) );
				return (UntwinerFactory)Sys.Delegate.CreateDelegate( typeof(UntwinerFactory), factory_method_info );
			}

			private static Sys.Type[] collect_interfaces( Sys.Type interface_type )
			{
				var mutable_list = new List<Sys.Type>();
				collect_interfaces_recursive( mutable_list, interface_type );
				return mutable_list.ToArray();

				static void collect_interfaces_recursive( ICollection<Sys.Type> interfaces, Sys.Type interface_type )
				{
					interfaces.Add( interface_type );
					foreach( Sys.Type child_interface_type in interface_type.GetInterfaces() )
						if( !interfaces.Contains( child_interface_type ) )
							collect_interfaces_recursive( interfaces, child_interface_type );
				}
			}

			private static SysReflect.MethodInfo[] collect_method_infos( IEnumerable<Sys.Type> interface_types )
			{
				var mutable_list = new List<SysReflect.MethodInfo>();
				foreach( Sys.Type interface_type in interface_types )
				{
					foreach( SysReflect.MethodInfo method_info in interface_type.GetMethods() )
						if( !mutable_list.Contains( method_info ) )
							mutable_list.Add( method_info );
				}
				return mutable_list.ToArray();
			}

			private static Sys.Type[] types_from_parameter_infos( IReadOnlyList<SysReflect.ParameterInfo> parameter_infos )
			{
				var types = new Sys.Type[parameter_infos.Count];
				for( int i = 0; i < parameter_infos.Count; i++ )
					types[i] = parameter_infos[i].ParameterType;
				return types;
			}

			private static void emit_ldc_i4( SysReflectEmit.ILGenerator gen, int value )
			{
				if( value >= -1 && value <= 8 )
					gen.Emit( value switch
					{
						-1 => SysReflectEmit.OpCodes.Ldc_I4_M1,
						0 => SysReflectEmit.OpCodes.Ldc_I4_0,
						1 => SysReflectEmit.OpCodes.Ldc_I4_1,
						2 => SysReflectEmit.OpCodes.Ldc_I4_2,
						3 => SysReflectEmit.OpCodes.Ldc_I4_3,
						4 => SysReflectEmit.OpCodes.Ldc_I4_4,
						5 => SysReflectEmit.OpCodes.Ldc_I4_5,
						6 => SysReflectEmit.OpCodes.Ldc_I4_6,
						7 => SysReflectEmit.OpCodes.Ldc_I4_7,
						8 => SysReflectEmit.OpCodes.Ldc_I4_8,
						_ => throw new Sys.Exception()
					} );
				else if( value >= sbyte.MinValue && value <= sbyte.MaxValue )
					gen.Emit( SysReflectEmit.OpCodes.Ldc_I4_S, (sbyte)value );
				else
					gen.Emit( SysReflectEmit.OpCodes.Ldc_I4, value );
			}
		}

		/// <summary> An implementation of <see cref="Intertwine{T}" /> which uses code generation.</summary>
		private sealed class CompilingIntertwine<T> : CompilingIntertwine, Intertwine<T> where T : class
		{
			private readonly IReadOnlyDictionary<SysReflect.MethodInfo, MethodKey> method_keys_by_method_info;

			public CompilingIntertwine( bool save )
					: base( save, typeof(T) )
			{
				CompilingMethodKey[] method_keys = new CompilingMethodKey[MethodInfos.Length];
				for( int i = 0; i < MethodInfos.Length; i++ )
					method_keys[i] = new CompilingMethodKey<T>( this, i, MethodInfos[i] );
				MethodKeys = method_keys;
				method_keys_by_method_info = method_keys.ToDictionary( k => k.MethodInfo(), k => (MethodKey)k );
			}

			public override IReadOnlyList<MethodKey> MethodKeys { get; }

			public override MethodKey MethodKeyByMethodInfo( SysReflect.MethodInfo method_info ) => method_keys_by_method_info[method_info];

			/// <summary>
			/// <para>Creates a new entwiner for a certain interface type, instantiated for the given <see cref="AnyCall" /> delegate.</para>
			/// <para>(Generic version.)</para>
			/// <para>If the entwiner class has already been generated, it is fetched from a cache; otherwise, it is generated on
			/// the spot and added to the cache.</para>
			/// </summary>
			/// <typeparam name="T">The type for which to create the entwiner. It must be an interface type.</typeparam>
			/// <param name="any_call">The <see cref="AnyCall" /> delegate for which to instantiate the entwiner.</param>
			/// <returns>An entwiner for the given interface type instantiated for the given <see cref="AnyCall" /> delegate.</returns>
			public T NewEntwiningInstance( AnyCall any_call )
			{
				return (T)NewEntwiner( any_call );
			}

			/// <summary>
			/// <para>Creates a new untwiner for a certain interface type, instantiated for the given target interface implementation.</para>
			/// <para>(Generic version.)</para>
			/// <para>If the untwiner class has already been generated, it is fetched from a cache; otherwise, it is generated on
			/// the spot and added to the cache.</para>
			/// </summary>
			/// <typeparam name="T">The type for which to create the entwiner. It must be an interface type.</typeparam>
			/// <param name="target">The target interface implementation for which to instantiate the untwiner.</param>
			/// <returns>An untwiner for the given interface type instantiated for the given target interface implementation.
			/// </returns>
			public AnyCall NewUntwiningInstance( T target )
			{
				return NewUntwiner( target );
			}
		}
	}
}
