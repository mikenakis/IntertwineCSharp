#nullable enable
namespace MikeNakis.Intertwine
{
	using System.Collections.Generic;
	using Sys = System;
	using SysReflect = System.Reflection;

	public interface Intertwine
	{
		///<summary>Gets the type of the interface.</summary>
		Sys.Type InterfaceType { get; }

		///<summary>Gets all the keys of the interface.</summary>
		IReadOnlyList<MethodKey> MethodKeys { get; }

		///<summary>Obtains a <see cref="MethodKey"/> given a <see cref="SysReflect.MethodInfo"/>.</summary>
		MethodKey MethodKeyByMethodInfo( SysReflect.MethodInfo method_info );

		///<summary>Creates a new implementation of the target interface which delegates to the given instance of <see cref="AnyCall"/>.</summary>
		/// <param name="any_call">The <see cref="AnyCall"/> delegate for which to instantiate the entwiner.</param>
		/// <returns>A new entwiner for the given interface type, instantiated for the given <see cref="AnyCall"/> delegate.</returns>
		Entwiner NewEntwiner( AnyCall any_call );

		///<summary>Creates a new implementation of <see cref="AnyCall"/> which delegates to the given instance of the target interface.</summary>
		/// <param name="target">The target.</param>
		/// <returns>A new untwiner for the given interface type, instantiated for the given target object.</returns>
		Untwiner NewUntwiner( object target );
	}

	public interface Intertwine<T> : Intertwine where T : class //actually, where T: interface
	{
		///<summary>Creates a new implementation of the target interface <typeparamref name="T"/> which delegates to the given instance of <see cref="AnyCall"/>.</summary>
		/// <typeparam name="T">The type for which to create the entwiner. It must be an interface type.</typeparam>
		/// <param name="any_call">The <see cref="AnyCall"/> delegate for which to instantiate the entwiner.</param>
		/// <returns>An entwiner for the given interface type instantiated for the given <see cref="AnyCall"/> delegate.</returns>
		T NewEntwiningInstance( AnyCall any_call );

		///<summary>Creates a new implementation of <see cref="AnyCall"/> which delegates to the given instance of the target interface <typeparamref name="T"/>.</summary>
		/// <typeparam name="T">The type for which to create the entwiner. It must be an interface type.</typeparam>
		/// <param name="target">The target interface implementation for which to instantiate the untwiner.</param>
		/// <returns>An untwiner for the given interface type instantiated for the given target interface implementation.
		/// </returns>
		AnyCall NewUntwiningInstance( T target );
	}
}
