//Author MikeNakis (michael.gr)

namespace MikeNakis.Intertwine
{
	///<summary>Base class for entwiners.</summary>
	///<author>michael.gr</author>
	public abstract class Entwiner //must be public because it is accessed by another (dynamically generated) assembly.
	{
		public readonly System.Type InterfaceType;
		public readonly AnyCall AnyCall;

		/// <summary>Constructor.</summary>
		/// <param name="interface_type">The type of the interface entwined by this entwiner.</param>
		/// <param name="any_call">The AnyCall delegate to invoke for each method invocation of the interface.</param>
		protected Entwiner( System.Type interface_type, AnyCall any_call )
		{
			InterfaceType = interface_type;
			AnyCall = any_call;
		}
	}
}
