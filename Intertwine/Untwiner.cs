//Author MikeNakis (michael.gr)

namespace MikeNakis.Intertwine
{
	/// <summary>
	/// Base class for untwiners.
	/// </summary>
	public abstract class Untwiner
	{
		public readonly System.Type InterfaceType;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="interface_type">The type of the interface untwined by this untwiner.</param>
		protected Untwiner( System.Type interface_type )
		{
			InterfaceType = interface_type;
		}

		/// <summary>
		/// The object which implements the interface and receives untwined calls from this untwiner.
		/// </summary>
		public abstract object Target { get; }

		/// <summary>
		/// The AnyCall method of this untwiner.
		/// </summary>
		public abstract object AnyCall( int selector, object[] args );
	}
}
