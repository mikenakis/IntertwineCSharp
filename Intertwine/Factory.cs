//Author MikeNakis (michael.gr)
namespace MikeNakis.Intertwine
{
    using Diagnostics = System.Diagnostics;
    using Emit = System.Reflection.Emit;
    using Generic = System.Collections.Generic;
    using Reflection = System.Reflection;

    /// <summary>
    /// Creates instances of <see cref="EntwinerFactory"/> and <see cref="UntwinerFactory"/>.
    /// </summary>
    public class Factory
    {
        private delegate Entwiner EntwinerFactory( AnyCall anycall );
        private delegate Untwiner UntwinerFactory( object target );

        private static CacheEntry get_cache_entry( System.Type twinee )
        {
            if( !IsCaching )
                return create_cache_entry( twinee );

            CacheEntry entry;
            if( !Cache.TryGetValue( twinee, out entry ) )
            {
                entry = create_cache_entry( twinee );
                Cache.Add( twinee, entry );
            }
            return entry;
        }

        /// <summary>
        /// <para>Creates a new <see cref="Entwiner"/> for the given interface type, instantiated for the given anycall delegate.</para>
        /// <para>(Non-generic version.)</para>
        /// <para>If the entwiner class has already been generated, it is fetched from a cache; otherwise, it is generated on
        /// the spot and added to the cache.</para>
        /// </summary>
        /// <param name="twinee">The type for which to create the entwiner. It must be an interface type.</param>
        /// <param name="anycall">The anycall delegate for which to instantiate the entwiner.</param>
        /// <returns>A new entwiner for the given interface type, instantiated for the given anycall delegate.</returns>
        public static Entwiner NewEntwiner( System.Type twinee, AnyCall anycall )
        {
            Entwiner entwiner = get_cache_entry( twinee ).EntwinerFactory.Invoke( anycall );
            Dbg.Assert( entwiner.Twinee == twinee );
            Dbg.Assert( entwiner.AnyCall == anycall );
            return entwiner;
        }

        /// <summary>
        /// <para>Creates a new <see cref="Untwiner"/> for the given interface type, instantiated for the given target interface implementation.</para>
        /// <para>(Non-generic version.)</para>
        /// <para>If the untwiner class has already been generated, it is fetched from a cache; otherwise, it is generated on
        /// the spot and added to the cache.</para>
        /// </summary>
        /// <param name="twinee">The type for which to create the untwiner. It must be an interface type.</param>
        /// <param name="anycall">The anycall delegate with which to instantiate the entwiner.</param>
        /// <returns>A new untwiner for the given interface type, instantiated for the given target object.</returns>
        public static Untwiner NewUntwiner( System.Type twinee, object target )
        {
            Untwiner untwiner = get_cache_entry( twinee ).UntwinerFactory.Invoke( target );
            Dbg.Assert( untwiner.Twinee == twinee );
            Dbg.Assert( untwiner.Target == target );
            return untwiner;
        }

        /// <summary>
        /// <para>Creates a new entwiner for a certain interface type, instantiated for the given anycall delegate.</para>
        /// <para>(Generic version.)</para>
        /// <para>If the entwiner class has already been generated, it is fetched from a cache; otherwise, it is generated on
        /// the spot and added to the cache.</para>
        /// </summary>
        /// <typeparam name="T">The type for which to create the entwiner. It must be an interface type.</typeparam>
        /// <param name="anycall">The anycall delegate for which to instantiate the entwiner.</param>
        /// <returns>An entwiner for the given interface type instantiated for the given anycall delegate.</returns>
        public static T NewEntwiner<T>( AnyCall anycall )
            where T: class //actually, where T: interface
        {
            return (T)(object)NewEntwiner( typeof(T), anycall );
        }

        /// <summary>
        /// <para>Creates a new untwiner for a certain interface type, instantiated for the given target interface implementation.</para>
        /// <para>(Generic version.)</para>
        /// <para>If the untwiner class has already been generated, it is fetched from a cache; otherwise, it is generated on
        /// the spot and added to the cache.</para>
        /// </summary>
        /// <typeparam name="T">The type for which to create the entwiner. It must be an interface type.</typeparam>
        /// <param name="target">The target interface implementation for which to instantiate the untwiner.</param>
        /// <returns>An untwiner for the given interface type instantiated for the given target interface implmentation.
        /// </returns>
        public static AnyCall NewUntwiner<T>( T target ) 
            where T: class //actually, where T: interface
        {
            return NewUntwiner( typeof(T), target ).AnyCall;
        }

        private sealed class CacheEntry
        {
            public readonly System.Type     TypeOfTwinee;
            public readonly EntwinerFactory EntwinerFactory;
            public readonly UntwinerFactory UntwinerFactory;

            public CacheEntry( System.Type type_of_twinee, EntwinerFactory entwiner_factory, UntwinerFactory untwiner_factory )
            {
                TypeOfTwinee    = type_of_twinee;
                EntwinerFactory = entwiner_factory;
                UntwinerFactory = untwiner_factory;
            }

            public override bool Equals( object obj )
            {
                CacheEntry other = obj as CacheEntry;
                if( other != null )
                    return TypeOfTwinee == other.TypeOfTwinee;
                Dbg.Breakpoint();
                return false;
            }

            public override int GetHashCode()
            {
                return TypeOfTwinee.GetHashCode();
            }
        }

        private static Generic.Dictionary<System.Type,CacheEntry> Cache = new Generic.Dictionary<System.Type,CacheEntry>();

        public static bool IsCaching = true;
        public static bool IsSaving  = true;

        private static CacheEntry create_cache_entry( System.Type twinee )
        {
            Dbg.Assert( twinee.IsInterface );

            System.Type[] interfaces = collect_interfaces( twinee );
            Reflection.MethodInfo[] methodinfos = collect_methodinfos( interfaces );
            Reflection.ParameterInfo[][] parameterinfos = new Reflection.ParameterInfo[methodinfos.Length][];
            for( int i = 0;  i < methodinfos.Length; i++ )
                parameterinfos[i] = methodinfos[i].GetParameters();

            Emit.ModuleBuilder modulebuilder;
            {
                string name = twinee.FullName.Replace( "[", "" ).Replace( "]", "" ).Replace( " ", "" ).Replace( ",", "" )
                    .Replace( "mscorlib", "" ).Replace( "Culture=neutral", "" ).Replace( "PublicKeyToken=", "" )
                    .Replace( "Version=", "_" );
                string outfilename = name + ".dll";
                Reflection.AssemblyName assemblyname = new Reflection.AssemblyName( "AssemblyFor" + name );
                Emit.AssemblyBuilder assemblybuilder = System.AppDomain.CurrentDomain.DefineDynamicAssembly( assemblyname, 
                    IsSaving? Emit.AssemblyBuilderAccess.RunAndSave : Emit.AssemblyBuilderAccess.Run, 
                    dir:null );

                if( IsSaving )
                {
                    // Mark generated code as debuggable. 
                    // See http://blogs.msdn.com/rmbyers/archive/2005/06/26/432922.aspx for explanation.        
                    Reflection.ConstructorInfo constructor_info_for_debuggable_attribute = 
                        typeof(Diagnostics.DebuggableAttribute).GetConstructor( new System.Type[] 
                            { typeof(Diagnostics.DebuggableAttribute.DebuggingModes) });
                    Emit.CustomAttributeBuilder attribute_builder_for_debuggable = 
                        new Emit.CustomAttributeBuilder( constructor_info_for_debuggable_attribute, new object[] { 
                            Diagnostics.DebuggableAttribute.DebuggingModes.DisableOptimizations | 
                            Diagnostics.DebuggableAttribute.DebuggingModes.Default } );
                    assemblybuilder.SetCustomAttribute( attribute_builder_for_debuggable );

                    // Create the module builder so that the module can be saved.
                    modulebuilder = assemblybuilder.DefineDynamicModule( assemblyname.Name, outfilename, true );
                }
                else
                {
                    // Create the module builder without provision for saving.
                    modulebuilder = assemblybuilder.DefineDynamicModule( assemblyname.Name );
                }
            }

            var factory_of_entwiner = create_entwiner( modulebuilder, twinee, methodinfos, parameterinfos, interfaces );
            var factory_of_untwiner = create_untwiner( modulebuilder, twinee, methodinfos, parameterinfos );
            CacheEntry entry = new CacheEntry( twinee, factory_of_entwiner, factory_of_untwiner );

            if( IsSaving )
            {
                var assemblybuilder = (Emit.AssemblyBuilder)modulebuilder.Assembly;
                assemblybuilder.Save( System.IO.Path.GetFileName( modulebuilder.FullyQualifiedName ) );
            }
            return entry;
        }

        private static readonly Reflection.MethodInfo MethodInfoForGetTypeFromHandle = 
            typeof(System.Type).GetMethod( "GetTypeFromHandle", new System.Type[]{ typeof(System.RuntimeTypeHandle) } );

        private static readonly Reflection.ConstructorInfo ConstructorInfoForEntwiner = 
            typeof( Entwiner ).GetConstructor( new System.Type[] { typeof(System.Type), typeof(AnyCall) } );
        private static readonly Reflection.MethodInfo MethodInfoForAnyCallInvoke = 
            typeof(AnyCall).GetMethod( "Invoke", new System.Type[]{ typeof(int), typeof(object[]) } );
        private static readonly Reflection.FieldInfo FieldInfoForEntwinerAnyCall = typeof(Entwiner).GetField( "AnyCall" );

        private static readonly Reflection.ConstructorInfo ConstructorInfoForUntwiner = 
            typeof(Untwiner).GetConstructor( new System.Type[] { typeof(System.Type) } );
        private static readonly Reflection.MethodInfo MethodInfoForUntwinerGetTarget = 
            typeof(Untwiner).GetMethod( "get_Target" );
        private static readonly Reflection.MethodInfo MethodInfoForUntwinerAnycall = typeof(Untwiner).GetMethod( "AnyCall" );
        private static readonly Reflection.ConstructorInfo ConstructorInfoForInvalidOperationException = 
            typeof(System.InvalidOperationException).GetConstructor( System.Type.EmptyTypes );

        private static readonly string FactoryMethodName = "Factory";

        private const Reflection.TypeAttributes AttributesForType = Reflection.TypeAttributes.AutoClass | 
                Reflection.TypeAttributes.Class | Reflection.TypeAttributes.Public | Reflection.TypeAttributes.Sealed | 
                Reflection.TypeAttributes.BeforeFieldInit;
        private const Reflection.MethodAttributes AttributesForConstructor = Reflection.MethodAttributes.Public |
                Reflection.MethodAttributes.HideBySig | Reflection.MethodAttributes.SpecialName |
                Reflection.MethodAttributes.RTSpecialName;

        private static EntwinerFactory create_entwiner( Emit.ModuleBuilder modulebuilder, System.Type twinee, 
            Reflection.MethodInfo[] methodinfos, Reflection.ParameterInfo[][] parameterinfoses, System.Type[] interfaces )
        {
            // Start creating the type
            Emit.TypeBuilder typebuilder = modulebuilder.DefineType( "EntwinerFor" + twinee.Name, AttributesForType, 
                typeof(Entwiner), interfaces );

            // Create the constructor
            Emit.ConstructorBuilder builder_for_constructor = typebuilder.DefineConstructor( AttributesForConstructor, 
                Reflection.CallingConventions.Standard, new System.Type[] { typeof(AnyCall) } );
            {
                Emit.ILGenerator ilgen = builder_for_constructor.GetILGenerator();
                ilgen.Emit( Emit.OpCodes.Ldarg_0 );
                ilgen.Emit( Emit.OpCodes.Ldtoken, twinee );
                ilgen.Emit( Emit.OpCodes.Call, MethodInfoForGetTypeFromHandle );
                ilgen.Emit( Emit.OpCodes.Ldarg_1 );
                ilgen.Emit( Emit.OpCodes.Call, ConstructorInfoForEntwiner );
                ilgen.Emit( Emit.OpCodes.Ret );
            }

            // Create each method
            for( int selector = 0;  selector < methodinfos.Length;  selector++ )
            {
                var methodinfo = methodinfos[selector];

                // Get parameter infos
                Reflection.ParameterInfo[] parameterinfos = parameterinfoses[selector];

                // Define method and get method builder
                Emit.MethodBuilder methodbuilder = typebuilder.DefineMethod( methodinfo.Name, 
                    Reflection.MethodAttributes.Private | Reflection.MethodAttributes.Virtual | 
                    Reflection.MethodAttributes.Final | Reflection.MethodAttributes.HideBySig | 
                    Reflection.MethodAttributes.NewSlot, //can someone explain to me why NewSlot works here?
                    Reflection.CallingConventions.HasThis, methodinfo.ReturnType, types_from_parameterinfos( parameterinfos ) );
                typebuilder.DefineMethodOverride( methodbuilder, methodinfo );

                // Define generic parameters, if any
                System.Type[] generic_arguments = methodinfo.GetGenericArguments();
                if( generic_arguments.Length > 0 )
                {
                    string[] generic_type_names = new string[generic_arguments.Length];
                    for( int index = 0;  index < generic_arguments.Length;  index++ )
                        generic_type_names[index] = "T" + index;
                    methodbuilder.DefineGenericParameters( generic_type_names );
                }

                // Get the code generator
                Emit.ILGenerator ilgen = methodbuilder.GetILGenerator();

                // Emit code to allocate the array
                Emit.LocalBuilder localbuilder_for_array = ilgen.DeclareLocal( typeof(object[]) );
                //localbuilder_for_array.SetLocalSymInfo( "args" );
                emit_ldc_i4( ilgen, parameterinfos.Length );
                ilgen.Emit( Emit.OpCodes.Newarr, typeof(object) );
                ilgen.Emit( Emit.OpCodes.Stloc, localbuilder_for_array );

                // Emit code to copy each argument to the array
                foreach( var parameterinfo in parameterinfos )
                {
                    // Emit code to select the current element of the array (used by Stelem_Ref in the end)
                    ilgen.Emit( Emit.OpCodes.Ldloc, localbuilder_for_array );
                    emit_ldc_i4( ilgen, parameterinfo.Position );

                    // If the argument is passed with 'ref'...
                    System.Type actual_paramtype;
                    if( parameterinfo.ParameterType.IsByRef )
                    {
                        actual_paramtype = parameterinfo.ParameterType.GetElementType();

                        // If this is an 'out' argument...
                        if( parameterinfo.IsOut )
                        {
                            //Emit code to load a zero in its place.
                            ilgen.Emit( Emit.OpCodes.Ldnull );
                        }
                        // Otherwise, this is not an 'out' argument, so...
                        else
                        {
                            // Emit code to get the address of the argument
                            ilgen.Emit( Emit.OpCodes.Ldarg, parameterinfo.Position + 1 );

                            // Emit code to fetch the argument from its address
                            ilgen.Emit( Emit.OpCodes.Ldobj, actual_paramtype );
                        }
                    }
                    else
                    {
                        actual_paramtype = parameterinfo.ParameterType;

                        // Emit code to get the value of the argument
                        ilgen.Emit( Emit.OpCodes.Ldarg, parameterinfo.Position + 1 );
                    }

                    // If the argument is a value type, box it.
                    if( actual_paramtype.IsValueType )
                        ilgen.Emit( Emit.OpCodes.Box, actual_paramtype );

                    // Store the argument in the array.
                    ilgen.Emit( Emit.OpCodes.Stelem_Ref );
                }

                // Emit code to invoke AnyCall
                ilgen.Emit( Emit.OpCodes.Ldarg_0 );
                ilgen.Emit( Emit.OpCodes.Ldfld, FieldInfoForEntwinerAnyCall );
                emit_ldc_i4( ilgen, selector );
                ilgen.Emit( Emit.OpCodes.Ldloc_S, localbuilder_for_array );
                ilgen.Emit( Emit.OpCodes.Callvirt, MethodInfoForAnyCallInvoke );

                // Emit code to update any arguments that were passed by ref
                foreach( Reflection.ParameterInfo parameterinfo in parameterinfos )
                {
                    // If this argument was not passed by ref, skip it.
                    if( !parameterinfo.ParameterType.IsByRef )
                        continue;

                    // Get actual type of argument
                    var actual_paramtype = parameterinfo.ParameterType.GetElementType();

                    // Emit code to load the address of the argument (will be used later by the Stind or Stobj instruction.)
                    ilgen.Emit( Emit.OpCodes.Ldarg, parameterinfo.Position + 1 );

                    // Emit code to load the value from the array
                    ilgen.Emit( Emit.OpCodes.Ldloc_0 );
                    emit_ldc_i4( ilgen, parameterinfo.Position );
                    ilgen.Emit( Emit.OpCodes.Ldelem_Ref );

                    // If this is a value type...
                    if( actual_paramtype.IsValueType )
                    {
                        // Emit code to unbox the value and store it back into the argument
                        ilgen.Emit( Emit.OpCodes.Unbox, actual_paramtype );
                        ilgen.Emit( Emit.OpCodes.Cpobj, actual_paramtype );
                    }
                    // Otherwise, this is a reference type, so...
                    else
                    {
                        // Emit code to store the value back into the argument
                        ilgen.Emit( Emit.OpCodes.Stind_Ref );
                    }
                }

                // If the method has no return value...
                if( methodinfo.ReturnType == typeof(void) )
                {
                    // Emit code to discard the return value from AnyCall.
                    ilgen.Emit( Emit.OpCodes.Pop );
                }
                // Otherwise, the method has a return value, so...
                else
                {
                    // Emit code to unbox the return value.
                    ilgen.Emit( Emit.OpCodes.Unbox_Any, methodinfo.ReturnType );
                }

                // Emit return instruction.
                ilgen.Emit( Emit.OpCodes.Ret );
            }

            // Create the factory method
            {
                // Define the method.
                Emit.MethodBuilder methodbuilder_for_factory = typebuilder.DefineMethod( FactoryMethodName, 
                    Reflection.MethodAttributes.Public | Reflection.MethodAttributes.Static | Reflection.MethodAttributes.Final, 
                    Reflection.CallingConventions.Standard, typeof(Entwiner), new System.Type[]{ typeof(AnyCall) } );
                Emit.ILGenerator ilgen = methodbuilder_for_factory.GetILGenerator();

                // Emit code to load the one argument that we have been passed
                ilgen.Emit( Emit.OpCodes.Ldarg_0 );

                // Emit code to create a new object.
                ilgen.Emit( Emit.OpCodes.Newobj, builder_for_constructor );

                // Emit return instruction.
                ilgen.Emit( Emit.OpCodes.Ret );
            }
            
            // Create the type.
            System.Type entwiner = typebuilder.CreateType();

            // Find and return the factory.
            // Note: we cannot use the methodbuilder here because it wants a 'runtime method info'.
            Reflection.MethodInfo factorymethodinfo = entwiner.GetMethod( FactoryMethodName, Reflection.BindingFlags.Public | 
                Reflection.BindingFlags.Static );
            return (EntwinerFactory)System.Delegate.CreateDelegate( typeof(EntwinerFactory), factorymethodinfo );
        }

        private static UntwinerFactory create_untwiner( Emit.ModuleBuilder modulebuilder, System.Type twinee,
            Reflection.MethodInfo[] methodinfos, Reflection.ParameterInfo[][] parameterinfoses )
        {
            // Start creating the type
            Emit.TypeBuilder typebuilder = modulebuilder.DefineType( "UntwinerFor" + twinee.Name, AttributesForType, 
                typeof(Untwiner) );

            // Create the _Target field
            Emit.FieldBuilder fieldbuilder_for_target = typebuilder.DefineField( "_Target", twinee, 
                Reflection.FieldAttributes.Private | Reflection.FieldAttributes.InitOnly );

            // Create the constructor
            Emit.ConstructorBuilder constructorbuilder = typebuilder.DefineConstructor( AttributesForConstructor, 
                Reflection.CallingConventions.Standard, new System.Type[] { typeof(object) } );
            {
                Emit.ILGenerator ilgen = constructorbuilder.GetILGenerator();
                ilgen.Emit( Emit.OpCodes.Ldarg_0 );
                ilgen.Emit( Emit.OpCodes.Ldtoken, twinee );
                ilgen.Emit( Emit.OpCodes.Call, MethodInfoForGetTypeFromHandle );
                ilgen.Emit( Emit.OpCodes.Call, ConstructorInfoForUntwiner );
                ilgen.Emit( Emit.OpCodes.Ldarg_0 );
                ilgen.Emit( Emit.OpCodes.Ldarg_1 );
                ilgen.Emit( Emit.OpCodes.Stfld, fieldbuilder_for_target );
                ilgen.Emit( Emit.OpCodes.Ret );
            }

            // Create the get_Target method
            Emit.MethodBuilder methodbuilder_for_get_target = typebuilder.DefineMethod( "get_Target", 
                Reflection.MethodAttributes.Public | Reflection.MethodAttributes.Virtual | Reflection.MethodAttributes.Final | 
                Reflection.MethodAttributes.SpecialName, Reflection.CallingConventions.HasThis, typeof(object), 
                System.Type.EmptyTypes );
            typebuilder.DefineMethodOverride( methodbuilder_for_get_target, MethodInfoForUntwinerGetTarget );
            {
                Emit.ILGenerator ilgen = methodbuilder_for_get_target.GetILGenerator();
                ilgen.Emit( Emit.OpCodes.Ldarg_0 );
                ilgen.Emit( Emit.OpCodes.Ldfld, fieldbuilder_for_target );
                ilgen.Emit( Emit.OpCodes.Ret );
            }

            // Create the AnyCall method
            Emit.MethodBuilder methodbuilder_for_anycall = typebuilder.DefineMethod( "AnyCall", 
                Reflection.MethodAttributes.Public | Reflection.MethodAttributes.Virtual | Reflection.MethodAttributes.Final, 
                Reflection.CallingConventions.HasThis, typeof(object), new System.Type[]{ typeof(int), typeof(object[]) } );
            typebuilder.DefineMethodOverride( methodbuilder_for_anycall, MethodInfoForUntwinerAnycall ); //XXX unnecessary?
            {
                // Get the IL generator
                Emit.ILGenerator ilgen = methodbuilder_for_anycall.GetILGenerator();

                // Define one label for each target of the switch statement.
                Emit.Label[] labels = new Emit.Label[methodinfos.Length];
                for( int i = 0;  i < methodinfos.Length;  i++ )
                    labels[i] = ilgen.DefineLabel();

                // Optimization: the following is common to all case statements, so it was moved before the switch statement.
                // Emit code to load the _Target field.
                ilgen.Emit( Emit.OpCodes.Ldarg_0 );
                ilgen.Emit( Emit.OpCodes.Ldfld, fieldbuilder_for_target );

                // Emit a switch statement on argument 1 (selector)
                ilgen.Emit( Emit.OpCodes.Ldarg_1 );
                ilgen.Emit( Emit.OpCodes.Switch, labels );

                // Emit code for switch statement fall-through (the selector was invalid): throw invalid operation exception.
                ilgen.Emit( Emit.OpCodes.Newobj, ConstructorInfoForInvalidOperationException );
                ilgen.Emit( Emit.OpCodes.Throw );

                // Emit one block of code for each method of the interface being untwined.
                for( int selector = 0;  selector < methodinfos.Length;  selector++ )
                {
                    // Mark the label so that the switch statement can branch here.
                    ilgen.MarkLabel( labels[selector] );

                    // Emit code to pass each parameter to the method
                    Reflection.MethodInfo methodinfo = methodinfos[selector];
                    foreach( var parameterinfo in parameterinfoses[selector] )
                    {
                        // Emit code to select the current parameter in the array
                        ilgen.Emit( Emit.OpCodes.Ldarg_2 );
                        emit_ldc_i4( ilgen, parameterinfo.Position );

                        // If this parameter is passed by ref or out...
                        if( parameterinfo.ParameterType.IsByRef )
                        {
                            // If this is a value-type...
                            System.Type actual_paramtype = parameterinfo.ParameterType.GetElementType();
                            if( actual_paramtype.IsValueType )
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
                                ilgen.Emit( Emit.OpCodes.Ldelem_Ref );
                                ilgen.Emit( Emit.OpCodes.Unbox, actual_paramtype );
                            }
                            // Otherwise, if this is a reference-type...
                            else
                            {
                                // Emit code to load the address of the array element.
                                ilgen.Emit( Emit.OpCodes.Ldelema, typeof(object) );
                            }
                        }
                        // Otherwise, if this parameter is a value-type...
                        else if( parameterinfo.ParameterType.IsValueType )
                        {
                            // Emit code to load the element from the array and unbox it.
                            ilgen.Emit( Emit.OpCodes.Ldelem_Ref );
                            ilgen.Emit( Emit.OpCodes.Unbox_Any, parameterinfo.ParameterType );
                        }
                        // Otherwise, this parameter is a reference-type, so...
                        else
                        {
                            // Emit code to load the element from the array.
                            ilgen.Emit( Emit.OpCodes.Ldelem_Ref );
                        }
                    }
                    
                    // Emit a call to the method of the twinee interface
                    ilgen.Emit( Emit.OpCodes.Callvirt, methodinfo );

                    // If this method does not return anything...
                    if( methodinfo.ReturnType == typeof(void) )
                    {
                        // We must return something, so emit code to load a null.
                        ilgen.Emit( Emit.OpCodes.Ldnull );
                    }
                    // Otherwise, this method returns something, so... */
                    else
                    {
                        // If the return type is a value type...
                        if( methodinfo.ReturnType.IsValueType )
                        {
                            // We always return an object, so emit code to box the return value.
                            ilgen.Emit( Emit.OpCodes.Box, methodinfo.ReturnType );
                        }
                    }

                    // Emit return statement.
                    ilgen.Emit( Emit.OpCodes.Ret );
                }
            }

            // Create the factory method
            {
                // Define the method.
                Emit.MethodBuilder methodbuilder_for_factory = typebuilder.DefineMethod( FactoryMethodName, 
                    Reflection.MethodAttributes.Public | Reflection.MethodAttributes.Static | Reflection.MethodAttributes.Final,
                    Reflection.CallingConventions.Standard, typeof(Untwiner), new System.Type[]{ typeof(object) } );
                Emit.ILGenerator ilgen = methodbuilder_for_factory.GetILGenerator();

                // Emit code to load the single argument that we have been given.
                ilgen.Emit( Emit.OpCodes.Ldarg_0 );

                // Emit code create a new object.
                ilgen.Emit( Emit.OpCodes.Newobj, constructorbuilder );

                // Emit return instruction.
                ilgen.Emit( Emit.OpCodes.Ret );
            }
            
            // Create the type.
            System.Type untwiner = typebuilder.CreateType();

            // Find and return the factory.
            // Note: we cannot use the methodbuilder here because it wants a 'runtime method info'.
            Reflection.MethodInfo factorymethodinfo = untwiner.GetMethod( FactoryMethodName, Reflection.BindingFlags.Public | 
                Reflection.BindingFlags.Static );
            return (UntwinerFactory)System.Delegate.CreateDelegate( typeof(UntwinerFactory), factorymethodinfo );
        }

        private static System.Type[] collect_interfaces( System.Type interfacetype )
        {
            var interfaces = new Generic.List<System.Type>();
            collect_interfaces_recursive( interfaces, interfacetype );
            return interfaces.ToArray();
        }

        private static void collect_interfaces_recursive( Generic.List<System.Type> interfaces, System.Type interfacetype )
        {
            interfaces.Add( interfacetype );
            System.Type[] interfaces2 = interfacetype.GetInterfaces();
            foreach( System.Type current in interfaces2 )
                if( !interfaces.Contains( current ) )
                    collect_interfaces_recursive( interfaces, current );
        }

        private static Reflection.MethodInfo[] collect_methodinfos( System.Type[] interfaces )
        {
            var list = new Generic.List<Reflection.MethodInfo>();
            foreach( System.Type @interface in interfaces )
            {
                foreach( Reflection.MethodInfo methodinfo in @interface.GetMethods() )
                    if( !list.Contains( methodinfo ) )
                        list.Add( methodinfo );
            }
            return list.ToArray();
        }

        private static System.Type[] types_from_parameterinfos( Reflection.ParameterInfo[] parameterinfos )
        {
            var types = new System.Type[parameterinfos.Length];
            for( int i = 0;  i < parameterinfos.Length;  i++ )
                types[i] = parameterinfos[i].ParameterType;
            return types;
        }
 
        private static void emit_ldc_i4( Emit.ILGenerator ilgen, int value )
        {
            if( value >= -1 && value <= 8 )
            { 
                Emit.OpCode opcode;
                switch( value )
                {
                    case -1: opcode = Emit.OpCodes.Ldc_I4_M1; break;
                    case 0:  opcode = Emit.OpCodes.Ldc_I4_0 ; break;
                    case 1:  opcode = Emit.OpCodes.Ldc_I4_1 ; break;
                    case 2:  opcode = Emit.OpCodes.Ldc_I4_2 ; break;
                    case 3:  opcode = Emit.OpCodes.Ldc_I4_3 ; break;
                    case 4:  opcode = Emit.OpCodes.Ldc_I4_4 ; break;
                    case 5:  opcode = Emit.OpCodes.Ldc_I4_5 ; break;
                    case 6:  opcode = Emit.OpCodes.Ldc_I4_6 ; break;
                    case 7:  opcode = Emit.OpCodes.Ldc_I4_7 ; break;
                    case 8:  opcode = Emit.OpCodes.Ldc_I4_8 ; break;
                    default: 
                        Dbg.Assert( false );
                        opcode = Emit.OpCodes.Ldc_I4;
                        break;
                }
                ilgen.Emit( opcode );
            }
            else if( value >= sbyte.MinValue && value <= sbyte.MaxValue )
                ilgen.Emit( Emit.OpCodes.Ldc_I4_S, (sbyte)value );
            else
                ilgen.Emit( Emit.OpCodes.Ldc_I4, value );
        }
    }
}
