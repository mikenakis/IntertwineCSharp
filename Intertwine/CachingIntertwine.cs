//Author MikeNakis (michael.gr)

#nullable enable
namespace MikeNakis.Intertwine
{
	using System.Collections.Generic;
	using Sys = System;

	/// <summary> A decorator of <see cref="IntertwineFactory" /> which adds caching.</summary>
	public class CachingIntertwineFactory : IntertwineFactory
	{
		private readonly IntertwineFactory decoree;

		public CachingIntertwineFactory( IntertwineFactory decoree )
		{
			this.decoree = decoree;
		}

		public Intertwine<T> GetIntertwine<T>() where T : class
		{
			if( !cache.TryGetValue( typeof(T), out Intertwine intertwine ) )
			{
				intertwine = decoree.GetIntertwine<T>();
				cache.Add( typeof(T), intertwine );
			}
			return (Intertwine<T>)intertwine;
		}

		private readonly Dictionary<Sys.Type, Intertwine> cache = new Dictionary<Sys.Type, Intertwine>();
	}
}
