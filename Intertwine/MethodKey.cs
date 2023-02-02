namespace MikeNakis.Intertwine
{
	using SysReflect = System.Reflection;

	///<summary>Represents a method of an interface.</summary>
	///<author>michael.gr</author>
	public interface MethodKey
	{
		///<summary>Gets the <see cref="Intertwine"/> that created this <see cref="MethodKey"/>.</summary>
		Intertwine Intertwine();

		///<summary>Gets the index of the method represented by this <see cref="MethodKey"/>.</summary>
		int MethodIndex();

		///<summary>Gets the <see cref="SysReflect.MethodInfo"/> of the method represented by this <see cref="MethodKey"/>.</summary>
		SysReflect.MethodInfo MethodInfo();
	}

	///<summary>Represents a method of an interface.</summary>
	///<author>michael.gr</author>
	public interface MethodKey<T> : MethodKey where T : class
	{
		///<summary>Gets the <see cref="Intertwine{T}"/> that created this <see cref="MethodKey{T}"/>.</summary>
		new Intertwine<T> Intertwine();
	}
}
