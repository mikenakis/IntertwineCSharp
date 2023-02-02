namespace MikeNakis.Intertwine
{
	///<summary>Creates (or obtains if cached) an <see cref="Intertwine"/> for an interface.</summary>
	///<author>michael.gr</author>
	public interface IntertwineFactory
	{
		///<summary>Obtains an <see cref="Intertwine"/> for a given interface type.</summary>
		Intertwine<T> GetIntertwine<T>() where T : class;
	}

	//workaround to C# 7's lack of support for static interface methods
	public static class IntertwineFactoryInstance
	{
		/**
      	 * The global instance of intertwine.
	     */
		public static readonly IntertwineFactory Value = new CachingIntertwineFactory( new CompilingIntertwineFactory() );
	}
}
