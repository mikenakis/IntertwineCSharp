//Author MikeNakis (michael.gr)

namespace MikeNakis.Intertwine.Benchmark
{
	using Sys = System;

	/// <summary>
	/// An untwiner which uses reflection to do its job. Keep for reference. WARNING: SLOW AS MOLASSES.
	/// </summary>
	public class ReflectingUntwiner
	{
		private readonly object target;
		private readonly Sys.Reflection.MethodInfo[] method_infos;

		public ReflectingUntwiner( Sys.Type interface_type, object target )
		{
			this.target = target;
			method_infos = interface_type.GetMethods( System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance );
		}

		public object AnyCall( int selector, object[] arguments )
		{
			return method_infos[selector].Invoke( target, arguments );
		}
	}
}
