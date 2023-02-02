//Author MikeNakis (michael.gr)

namespace MikeNakis.Intertwine
{
	///<summary>Base class for untwiners.</summary>
	///<author>michael.gr</author>
	public abstract class Untwiner //must be public because it is accessed by another (dynamically generated) assembly.
	{
		public readonly System.Type InterfaceType;

		/// <summary>Constructor.</summary>
		/// <param name="interface_type">The type of the interface untwined by this untwiner.</param>
		protected Untwiner( System.Type interface_type )
		{
			InterfaceType = interface_type;
		}

		/// <summary>The AnyCall method of this untwiner.</summary>
		public abstract object AnyCall( int selector, object[] args );
	}
}
