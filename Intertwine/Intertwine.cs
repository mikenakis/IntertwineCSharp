#nullable enable
namespace MikeNakis.Intertwine
{
	public interface Intertwine
	{
		/// <summary>
		/// <para>Creates a new <see cref="Entwiner"/> for the given interface type, instantiated for the given <see cref="AnyCall"/> delegate.</para>
		/// <para>(Non-generic version.)</para>
		/// <para>If the entwiner class has already been generated, it is fetched from a cache; otherwise, it is generated on
		/// the spot and added to the cache.</para>
		/// </summary>
		/// <param name="twinee">The type for which to create the entwiner. It must be an interface type.</param>
		/// <param name="any_call">The <see cref="AnyCall"/> delegate for which to instantiate the entwiner.</param>
		/// <returns>A new entwiner for the given interface type, instantiated for the given <see cref="AnyCall"/> delegate.</returns>
		Entwiner NewEntwiner( System.Type twinee, AnyCall any_call );

		/// <summary>
		/// <para>Creates a new <see cref="Untwiner"/> for the given interface type, instantiated for the given target interface implementation.</para>
		/// <para>(Non-generic version.)</para>
		/// <para>If the untwiner class has already been generated, it is fetched from a cache; otherwise, it is generated on
		/// the spot and added to the cache.</para>
		/// </summary>
		/// <param name="twinee">The type for which to create the untwiner. It must be an interface type.</param>
		/// <param name="target">The target.</param>
		/// <returns>A new untwiner for the given interface type, instantiated for the given target object.</returns>
		Untwiner NewUntwiner( System.Type twinee, object target );
	}

	public interface Intertwine<T> : Intertwine where T : class //actually, where T: interface
	{
		/// <summary>
		/// <para>Creates a new entwiner for a certain interface type, instantiated for the given <see cref="AnyCall"/> delegate.</para>
		/// <para>(Generic version.)</para>
		/// <para>If the entwiner class has already been generated, it is fetched from a cache; otherwise, it is generated on
		/// the spot and added to the cache.</para>
		/// </summary>
		/// <typeparam name="T">The type for which to create the entwiner. It must be an interface type.</typeparam>
		/// <param name="any_call">The <see cref="AnyCall"/> delegate for which to instantiate the entwiner.</param>
		/// <returns>An entwiner for the given interface type instantiated for the given <see cref="AnyCall"/> delegate.</returns>
		T NewEntwiner( AnyCall any_call );

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
		AnyCall NewUntwiner( T target );
	}
}
