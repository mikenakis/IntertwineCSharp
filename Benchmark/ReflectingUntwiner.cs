//Author MikeNakis (michael.gr)

namespace MikeNakis.Intertwine.Benchmark
{
	/// <summary>
	/// An untwiner which uses reflection to do its job. Keep for reference. WARNING: SLOW AS MOLASSES.
	/// </summary>
	public class ReflectingUntwiner : Untwiner
	{
		private readonly System.Reflection.MethodInfo[] method_infos;

		public ReflectingUntwiner( System.Type twinee, object target )
				: base( twinee )
		{
			Target = target;
			method_infos = Twinee.GetMethods( System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance );
		}

		public override object Target { get; }

		public override object AnyCall( int selector, object[] arguments )
		{
			return method_infos[selector].Invoke( Target, arguments );
		}
	}
}
