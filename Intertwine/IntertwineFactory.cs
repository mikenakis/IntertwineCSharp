namespace MikeNakis.Intertwine
{
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
