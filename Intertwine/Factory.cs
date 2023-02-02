//Author MikeNakis (michael.gr)

#nullable enable
namespace MikeNakis.Intertwine
{
	using System.Collections.Generic;
	using Sys = System;
	using SysDiag = System.Diagnostics;
	using Emit = System.Reflection.Emit;
	using SysReflect = System.Reflection;

	/// <summary>
	/// Creates instances of <see cref="EntwinerFactory"/> and <see cref="UntwinerFactory"/>.
	/// </summary>
	public class Factory
	{
		private delegate Entwiner EntwinerFactory( AnyCall any_call );

		private delegate Untwiner UntwinerFactory( object target );

		private static CacheEntry get_cache_entry( Sys.Type twinee )
		{
			if( !IsCaching )
				return create_cache_entry( twinee );

			if( !cache.TryGetValue( twinee, out CacheEntry entry ) )
			{
				entry = create_cache_entry( twinee );
				cache.Add( twinee, entry );
			}
			return entry;
		}

		/// <summary>
		/// <para>Creates a new <see cref="Entwiner"/> for the given interface type, instantiated for the given <see cref="AnyCall"/> delegate.</para>
		/// <para>(Non-generic version.)</para>
		/// <para>If the entwiner class has already been generated, it is fetched from a cache; otherwise, it is generated on
		/// the spot and added to the cache.</para>
		/// </summary>
		/// <param name="twinee">The type for which to create the entwiner. It must be an interface type.</param>
		/// <param name="any_call">The <see cref="AnyCall"/> delegate for which to instantiate the entwiner.</param>
		/// <returns>A new entwiner for the given interface type, instantiated for the given <see cref="AnyCall"/> delegate.</returns>
		public static Entwiner NewEntwiner( Sys.Type twinee, AnyCall any_call )
		{
			Entwiner entwiner = get_cache_entry( twinee ).EntwinerFactory.Invoke( any_call );
			Dbg.Assert( entwiner.Twinee == twinee );
			Dbg.Assert( entwiner.AnyCall == any_call );
			return entwiner;
		}

		/// <summary>
		/// <para>Creates a new <see cref="Untwiner"/> for the given interface type, instantiated for the given target interface implementation.</para>
		/// <para>(Non-generic version.)</para>
		/// <para>If the untwiner class has already been generated, it is fetched from a cache; otherwise, it is generated on
		/// the spot and added to the cache.</para>
		/// </summary>
		/// <param name="twinee">The type for which to create the untwiner. It must be an interface type.</param>
		/// <param name="target">The target.</param>
		/// <returns>A new untwiner for the given interface type, instantiated for the given target object.</returns>
		public static Untwiner NewUntwiner( Sys.Type twinee, object target )
		{
			Untwiner untwiner = get_cache_entry( twinee ).UntwinerFactory.Invoke( target );
			Dbg.Assert( untwiner.Twinee == twinee );
			Dbg.Assert( untwiner.Target == target );
			return untwiner;
		}

		/// <summary>
		/// <para>Creates a new entwiner for a certain interface type, instantiated for the given <see cref="AnyCall"/> delegate.</para>
		/// <para>(Generic version.)</para>
		/// <para>If the entwiner class has already been generated, it is fetched from a cache; otherwise, it is generated on
		/// the spot and added to the cache.</para>
		/// </summary>
		/// <typeparam name="T">The type for which to create the entwiner. It must be an interface type.</typeparam>
		/// <param name="any_call">The <see cref="AnyCall"/> delegate for which to instantiate the entwiner.</param>
		/// <returns>An entwiner for the given interface type instantiated for the given <see cref="AnyCall"/> delegate.</returns>
		public static T NewEntwiner<T>( AnyCall any_call ) where T : class //actually, where T: interface
		{
			return (T)(object)NewEntwiner( typeof(T), any_call );
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
		public static AnyCall NewUntwiner<T>( T target ) where T : class //actually, where T: interface
		{
			return NewUntwiner( typeof(T), target ).AnyCall;
		}

		private sealed class CacheEntry
		{
			private readonly Sys.Type type_of_twinee;
			public readonly EntwinerFactory EntwinerFactory;
			public readonly UntwinerFactory UntwinerFactory;

			public CacheEntry( Sys.Type type_of_twinee, EntwinerFactory entwiner_factory, UntwinerFactory untwiner_factory )
			{
				this.type_of_twinee = type_of_twinee;
				EntwinerFactory = entwiner_factory;
				UntwinerFactory = untwiner_factory;
			}

			public override bool Equals( object obj )
			{
				if( obj is CacheEntry other )
					return type_of_twinee == other.type_of_twinee;
				Dbg.Breakpoint();
				return false;
			}

			public override int GetHashCode()
			{
				return type_of_twinee.GetHashCode();
			}
		}

		private static readonly Dictionary<Sys.Type, CacheEntry> cache = new Dictionary<Sys.Type, CacheEntry>();

		public static bool IsCaching = true;
		public static bool IsSaving = true;

		private static CacheEntry create_cache_entry( Sys.Type twinee )
		{
			Dbg.Assert( twinee.IsInterface );

			Sys.Type[] interfaces = collect_interfaces( twinee );
			SysReflect.MethodInfo[] method_infos = collect_method_infos( interfaces );
			SysReflect.ParameterInfo[][] parameter_infos = new SysReflect.ParameterInfo[method_infos.Length][];
			for( int i = 0; i < method_infos.Length; i++ )
				parameter_infos[i] = method_infos[i].GetParameters();

			Emit.ModuleBuilder module_builder;
			{
				string name = Dbg.NotNull( twinee.FullName ).Replace( "[", "" ).Replace( "]", "" ).Replace( " ", "" ).Replace( ",", "" ).Replace( "mscorlib", "" ).Replace( "Culture=neutral", "" ).Replace( "PublicKeyToken=", "" ).Replace( "Version=", "_" );
				string output_filename = name + ".dll";
				SysReflect.AssemblyName assembly_name = new SysReflect.AssemblyName( "AssemblyFor" + name );
				Emit.AssemblyBuilder assembly_builder = Sys.AppDomain.CurrentDomain.DefineDynamicAssembly( assembly_name, IsSaving ? Emit.AssemblyBuilderAccess.RunAndSave : Emit.AssemblyBuilderAccess.Run, dir: null );

				if( IsSaving )
				{
					// Mark generated code as debuggable.
					// See http://blogs.msdn.com/rmbyers/archive/2005/06/26/432922.aspx for explanation.
					SysReflect.ConstructorInfo constructor_info_for_debuggable_attribute = Dbg.NotNull( typeof(SysDiag.DebuggableAttribute).GetConstructor( new[] { typeof(SysDiag.DebuggableAttribute.DebuggingModes) } ) );
					Emit.CustomAttributeBuilder attribute_builder_for_debuggable = new Emit.CustomAttributeBuilder( constructor_info_for_debuggable_attribute, new object[] { SysDiag.DebuggableAttribute.DebuggingModes.DisableOptimizations | SysDiag.DebuggableAttribute.DebuggingModes.Default } );
					assembly_builder.SetCustomAttribute( attribute_builder_for_debuggable );

					// Create the module builder so that the module can be saved.
					module_builder = assembly_builder.DefineDynamicModule( assembly_name.Name, output_filename, true );
				}
				else
				{
					// Create the module builder without provision for saving.
					module_builder = assembly_builder.DefineDynamicModule( assembly_name.Name );
				}
			}

			var factory_of_entwiner = create_entwiner( module_builder, twinee, method_infos, parameter_infos, interfaces );
			var factory_of_untwiner = create_untwiner( module_builder, twinee, method_infos, parameter_infos );
			CacheEntry entry = new CacheEntry( twinee, factory_of_entwiner, factory_of_untwiner );

			if( IsSaving )
			{
				var assembly_builder = (Emit.AssemblyBuilder)module_builder.Assembly;
				assembly_builder.Save( Sys.IO.Path.GetFileName( module_builder.FullyQualifiedName ) );
			}
			return entry;
		}

		private static readonly SysReflect.MethodInfo method_info_for_get_type_from_handle = Dbg.NotNull( typeof(Sys.Type).GetMethod( "GetTypeFromHandle", new[] { typeof(Sys.RuntimeTypeHandle) } ) );

		private static readonly SysReflect.ConstructorInfo constructor_info_for_entwiner = Dbg.NotNull( typeof(Entwiner).GetConstructor( SysReflect.BindingFlags.NonPublic | SysReflect.BindingFlags.Instance, null, SysReflect.CallingConventions.Any, new[] { typeof(Sys.Type), typeof(AnyCall) }, null ) );
		private static readonly SysReflect.MethodInfo method_info_for_any_call_invoke = Dbg.NotNull( typeof(AnyCall).GetMethod( "Invoke", new[] { typeof(int), typeof(object[]) } ) );
		private static readonly SysReflect.FieldInfo field_info_for_entwiner_any_call = Dbg.NotNull( typeof(Entwiner).GetField( "AnyCall" ) );

		private static readonly SysReflect.ConstructorInfo constructor_info_for_untwiner = Dbg.NotNull( typeof(Untwiner).GetConstructor( SysReflect.BindingFlags.NonPublic | SysReflect.BindingFlags.Instance, null, SysReflect.CallingConventions.Any, new[] { typeof(Sys.Type) }, null ) );
		private static readonly SysReflect.MethodInfo method_info_for_untwiner_get_target = Dbg.NotNull( typeof(Untwiner).GetMethod( "get_Target" ) );
		private static readonly SysReflect.MethodInfo method_info_for_untwiner_any_call = Dbg.NotNull( typeof(Untwiner).GetMethod( "AnyCall" ) );
		private static readonly SysReflect.ConstructorInfo constructor_info_for_invalid_operation_exception = Dbg.NotNull( typeof(Sys.InvalidOperationException).GetConstructor( Sys.Type.EmptyTypes ) );

		private const string factory_method_name = "Factory";

		private const SysReflect.TypeAttributes attributes_for_type = SysReflect.TypeAttributes.AutoClass | SysReflect.TypeAttributes.Class | SysReflect.TypeAttributes.Public | SysReflect.TypeAttributes.Sealed | SysReflect.TypeAttributes.BeforeFieldInit;
		private const SysReflect.MethodAttributes attributes_for_constructor = SysReflect.MethodAttributes.Public | SysReflect.MethodAttributes.HideBySig | SysReflect.MethodAttributes.SpecialName | SysReflect.MethodAttributes.RTSpecialName;

		private static EntwinerFactory create_entwiner( Emit.ModuleBuilder module_builder, Sys.Type twinee, IReadOnlyList<SysReflect.MethodInfo> method_infos, SysReflect.ParameterInfo[][] parameter_infos_s, Sys.Type[] interfaces )
		{
			// Start creating the type
			Emit.TypeBuilder type_builder = module_builder.DefineType( "EntwinerFor" + twinee.Name, attributes_for_type, typeof(Entwiner), interfaces );

			// Create the constructor
			Emit.ConstructorBuilder builder_for_constructor = type_builder.DefineConstructor( attributes_for_constructor, SysReflect.CallingConventions.Standard, new[] { typeof(AnyCall) } );
			{
				Emit.ILGenerator gen = builder_for_constructor.GetILGenerator();
				gen.Emit( Emit.OpCodes.Ldarg_0 );
				gen.Emit( Emit.OpCodes.Ldtoken, twinee );
				gen.Emit( Emit.OpCodes.Call, method_info_for_get_type_from_handle );
				gen.Emit( Emit.OpCodes.Ldarg_1 );
				gen.Emit( Emit.OpCodes.Call, constructor_info_for_entwiner );
				gen.Emit( Emit.OpCodes.Ret );
			}

			// Create each method
			for( int selector = 0; selector < method_infos.Count; selector++ )
			{
				var method_info = method_infos[selector];

				// Get parameter infos
				SysReflect.ParameterInfo[] parameter_infos = parameter_infos_s[selector];

				// Define method and get method builder
				Emit.MethodBuilder method_builder = type_builder.DefineMethod( method_info.Name, SysReflect.MethodAttributes.Private | SysReflect.MethodAttributes.Virtual | SysReflect.MethodAttributes.Final | SysReflect.MethodAttributes.HideBySig | SysReflect.MethodAttributes.NewSlot, //can someone explain to me why NewSlot works here?
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
				Emit.ILGenerator gen = method_builder.GetILGenerator();

				// Emit code to allocate the array
				Emit.LocalBuilder local_builder_for_array = gen.DeclareLocal( typeof(object[]) );
				//local_builder_for_array.SetLocalSymInfo( "args" );
				emit_ldc_i4( gen, parameter_infos.Length );
				gen.Emit( Emit.OpCodes.Newarr, typeof(object) );
				gen.Emit( Emit.OpCodes.Stloc, local_builder_for_array );

				// Emit code to copy each argument to the array
				foreach( var parameter_info in parameter_infos )
				{
					// Emit code to select the current element of the array (used by the store-element instruction in the end)
					gen.Emit( Emit.OpCodes.Ldloc, local_builder_for_array );
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
							gen.Emit( Emit.OpCodes.Ldnull );
						}
						// Otherwise, this is not an 'out' argument, so...
						else
						{
							// Emit code to get the address of the argument
							gen.Emit( Emit.OpCodes.Ldarg, parameter_info.Position + 1 );

							// Emit code to fetch the argument from its address
							gen.Emit( Emit.OpCodes.Ldobj, actual_parameter_type );
						}
					}
					else
					{
						actual_parameter_type = Dbg.NotNull( parameter_info.ParameterType );

						// Emit code to get the value of the argument
						gen.Emit( Emit.OpCodes.Ldarg, parameter_info.Position + 1 );
					}

					// If the argument is a value type, box it.
					if( actual_parameter_type.IsValueType )
						gen.Emit( Emit.OpCodes.Box, actual_parameter_type );

					// Store the argument in the array.
					gen.Emit( Emit.OpCodes.Stelem_Ref );
				}

				// Emit code to invoke AnyCall
				gen.Emit( Emit.OpCodes.Ldarg_0 );
				gen.Emit( Emit.OpCodes.Ldfld, field_info_for_entwiner_any_call );
				emit_ldc_i4( gen, selector );
				gen.Emit( Emit.OpCodes.Ldloc_S, local_builder_for_array );
				gen.Emit( Emit.OpCodes.Callvirt, method_info_for_any_call_invoke );

				// Emit code to update any arguments that were passed by ref
				foreach( SysReflect.ParameterInfo parameter_info in parameter_infos )
				{
					// If this argument was not passed by ref, skip it.
					if( !parameter_info.ParameterType.IsByRef )
						continue;

					// Get actual type of argument
					var actual_parameter_type = Dbg.NotNull( parameter_info.ParameterType.GetElementType() );

					// Emit code to load the address of the argument (will be used later by the store instruction.)
					gen.Emit( Emit.OpCodes.Ldarg, parameter_info.Position + 1 );

					// Emit code to load the value from the array
					gen.Emit( Emit.OpCodes.Ldloc_0 );
					emit_ldc_i4( gen, parameter_info.Position );
					gen.Emit( Emit.OpCodes.Ldelem_Ref );

					// If this is a value type...
					if( actual_parameter_type.IsValueType )
					{
						// Emit code to unbox the value and store it back into the argument
						gen.Emit( Emit.OpCodes.Unbox, actual_parameter_type );
						gen.Emit( Emit.OpCodes.Cpobj, actual_parameter_type );
					}
					// Otherwise, this is a reference type, so...
					else
					{
						// Emit code to store the value back into the argument
						gen.Emit( Emit.OpCodes.Stind_Ref );
					}
				}

				// If the method has no return value...
				if( method_info.ReturnType == typeof(void) )
				{
					// Emit code to discard the return value from AnyCall.
					gen.Emit( Emit.OpCodes.Pop );
				}
				// Otherwise, the method has a return value, so...
				else
				{
					// Emit code to unbox the return value.
					gen.Emit( Emit.OpCodes.Unbox_Any, method_info.ReturnType );
				}

				// Emit return instruction.
				gen.Emit( Emit.OpCodes.Ret );
			}

			// Create the factory method
			{
				// Define the method.
				Emit.MethodBuilder method_builder_for_factory = type_builder.DefineMethod( factory_method_name, SysReflect.MethodAttributes.Public | SysReflect.MethodAttributes.Static | SysReflect.MethodAttributes.Final, SysReflect.CallingConventions.Standard, typeof(Entwiner), new[] { typeof(AnyCall) } );
				Emit.ILGenerator gen = method_builder_for_factory.GetILGenerator();

				// Emit code to load the one argument that we have been passed
				gen.Emit( Emit.OpCodes.Ldarg_0 );

				// Emit code to create a new object.
				gen.Emit( Emit.OpCodes.Newobj, builder_for_constructor );

				// Emit return instruction.
				gen.Emit( Emit.OpCodes.Ret );
			}

			// Create the type.
			Sys.Type entwiner = type_builder.CreateType();

			// Find and return the factory.
			// Note: we cannot use the method builder here because it wants a 'runtime method info'.
			SysReflect.MethodInfo factory_method_info = Dbg.NotNull( entwiner.GetMethod( factory_method_name, SysReflect.BindingFlags.Public | SysReflect.BindingFlags.Static ) );
			return (EntwinerFactory)Sys.Delegate.CreateDelegate( typeof(EntwinerFactory), factory_method_info );
		}

		private static UntwinerFactory create_untwiner( Emit.ModuleBuilder module_builder, Sys.Type twinee, IReadOnlyList<SysReflect.MethodInfo> method_infos, IReadOnlyList<SysReflect.ParameterInfo[]> parameter_infos_s )
		{
			// Start creating the type
			Emit.TypeBuilder type_builder = module_builder.DefineType( "UntwinerFor" + twinee.Name, attributes_for_type, typeof(Untwiner) );

			// Create the _Target field
			Emit.FieldBuilder field_builder_for_target = type_builder.DefineField( "_Target", twinee, SysReflect.FieldAttributes.Private | SysReflect.FieldAttributes.InitOnly );

			// Create the constructor
			Emit.ConstructorBuilder constructor_builder = type_builder.DefineConstructor( attributes_for_constructor, SysReflect.CallingConventions.Standard, new[] { typeof(object) } );
			{
				Emit.ILGenerator gen = constructor_builder.GetILGenerator();
				gen.Emit( Emit.OpCodes.Ldarg_0 );
				gen.Emit( Emit.OpCodes.Ldtoken, twinee );
				gen.Emit( Emit.OpCodes.Call, method_info_for_get_type_from_handle );
				gen.Emit( Emit.OpCodes.Call, constructor_info_for_untwiner );
				gen.Emit( Emit.OpCodes.Ldarg_0 );
				gen.Emit( Emit.OpCodes.Ldarg_1 );
				gen.Emit( Emit.OpCodes.Stfld, field_builder_for_target );
				gen.Emit( Emit.OpCodes.Ret );
			}

			// Create the get_Target method
			Emit.MethodBuilder method_builder_for_get_target = type_builder.DefineMethod( "get_Target", SysReflect.MethodAttributes.Public | SysReflect.MethodAttributes.Virtual | SysReflect.MethodAttributes.Final | SysReflect.MethodAttributes.SpecialName, SysReflect.CallingConventions.HasThis, typeof(object), Sys.Type.EmptyTypes );
			type_builder.DefineMethodOverride( method_builder_for_get_target, method_info_for_untwiner_get_target );
			{
				Emit.ILGenerator gen = method_builder_for_get_target.GetILGenerator();
				gen.Emit( Emit.OpCodes.Ldarg_0 );
				gen.Emit( Emit.OpCodes.Ldfld, field_builder_for_target );
				gen.Emit( Emit.OpCodes.Ret );
			}

			// Create the AnyCall method
			Emit.MethodBuilder method_builder_for_any_call = type_builder.DefineMethod( "AnyCall", SysReflect.MethodAttributes.Public | SysReflect.MethodAttributes.Virtual | SysReflect.MethodAttributes.Final, SysReflect.CallingConventions.HasThis, typeof(object), new[] { typeof(int), typeof(object[]) } );
			type_builder.DefineMethodOverride( method_builder_for_any_call, method_info_for_untwiner_any_call ); //XXX unnecessary?
			{
				// Get the IL generator
				Emit.ILGenerator gen = method_builder_for_any_call.GetILGenerator();

				// Define one label for each target of the switch statement.
				Emit.Label[] labels = new Emit.Label[method_infos.Count];
				for( int i = 0; i < method_infos.Count; i++ )
					labels[i] = gen.DefineLabel();

				// Optimization: the following is common to all case statements, so it was moved before the switch statement.
				// Emit code to load the _Target field.
				gen.Emit( Emit.OpCodes.Ldarg_0 );
				gen.Emit( Emit.OpCodes.Ldfld, field_builder_for_target );

				// Emit a switch statement on argument 1 (selector)
				gen.Emit( Emit.OpCodes.Ldarg_1 );
				gen.Emit( Emit.OpCodes.Switch, labels );

				// Emit code for switch statement fall-through (the selector was invalid): throw invalid operation exception.
				gen.Emit( Emit.OpCodes.Newobj, constructor_info_for_invalid_operation_exception );
				gen.Emit( Emit.OpCodes.Throw );

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
						gen.Emit( Emit.OpCodes.Ldarg_2 );
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
								// old box, we simply issue a single unbox opcode, which, according to MSDN, “[…] is not required to
								// copy the value type from the object. Typically it simply computes the address of the value type 
								// that is already present inside of the boxed object.” Thus, after executing unbox, the reference
								// to the boxed value type is ready in the evaluation stack for the target interface method to 
								// operate upon. In this case, when the method is going to be assigning a value to the byref argument,
								// it will be altering the actual contents of the boxing object, something which is quite impossible 
								// at the C# level!
								gen.Emit( Emit.OpCodes.Ldelem_Ref );
								gen.Emit( Emit.OpCodes.Unbox, actual_parameter_type );
							}
							// Otherwise, if this is a reference-type...
							else
							{
								// Emit code to load the address of the array element.
								gen.Emit( Emit.OpCodes.Ldelema, typeof(object) );
							}
						}
						// Otherwise, if this parameter is a value-type...
						else if( parameter_info.ParameterType.IsValueType )
						{
							// Emit code to load the element from the array and unbox it.
							gen.Emit( Emit.OpCodes.Ldelem_Ref );
							gen.Emit( Emit.OpCodes.Unbox_Any, parameter_info.ParameterType );
						}
						// Otherwise, this parameter is a reference-type, so...
						else
						{
							// Emit code to load the element from the array.
							gen.Emit( Emit.OpCodes.Ldelem_Ref );
						}
					}

					// Emit a call to the method of the twinee interface
					gen.Emit( Emit.OpCodes.Callvirt, method_info );

					// If this method does not return anything...
					if( method_info.ReturnType == typeof(void) )
					{
						// We must return something, so emit code to load a null.
						gen.Emit( Emit.OpCodes.Ldnull );
					}
					// Otherwise, this method returns something, so... */
					else
					{
						// If the return type is a value type...
						if( method_info.ReturnType.IsValueType )
						{
							// We always return an object, so emit code to box the return value.
							gen.Emit( Emit.OpCodes.Box, method_info.ReturnType );
						}
					}

					// Emit return statement.
					gen.Emit( Emit.OpCodes.Ret );
				}
			}

			// Create the factory method
			{
				// Define the method.
				Emit.MethodBuilder method_builder_for_factory = type_builder.DefineMethod( factory_method_name, SysReflect.MethodAttributes.Public | SysReflect.MethodAttributes.Static | SysReflect.MethodAttributes.Final, SysReflect.CallingConventions.Standard, typeof(Untwiner), new[] { typeof(object) } );
				Emit.ILGenerator gen = method_builder_for_factory.GetILGenerator();

				// Emit code to load the single argument that we have been given.
				gen.Emit( Emit.OpCodes.Ldarg_0 );

				// Emit code create a new object.
				gen.Emit( Emit.OpCodes.Newobj, constructor_builder );

				// Emit return instruction.
				gen.Emit( Emit.OpCodes.Ret );
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

		private static void emit_ldc_i4( Emit.ILGenerator gen, int value )
		{
			if( value >= -1 && value <= 8 )
				gen.Emit( value switch
				{
					-1 => Emit.OpCodes.Ldc_I4_M1,
					0 => Emit.OpCodes.Ldc_I4_0,
					1 => Emit.OpCodes.Ldc_I4_1,
					2 => Emit.OpCodes.Ldc_I4_2,
					3 => Emit.OpCodes.Ldc_I4_3,
					4 => Emit.OpCodes.Ldc_I4_4,
					5 => Emit.OpCodes.Ldc_I4_5,
					6 => Emit.OpCodes.Ldc_I4_6,
					7 => Emit.OpCodes.Ldc_I4_7,
					8 => Emit.OpCodes.Ldc_I4_8,
					_ => throw new Sys.Exception()
				} );
			else if( value >= sbyte.MinValue && value <= sbyte.MaxValue )
				gen.Emit( Emit.OpCodes.Ldc_I4_S, (sbyte)value );
			else
				gen.Emit( Emit.OpCodes.Ldc_I4, value );
		}
	}
}
