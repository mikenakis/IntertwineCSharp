//Author MikeNakis (michael.gr)

namespace MikeNakis.Intertwine
{
	/// <summary>
	/// Base class for entwiners.
	/// </summary>
	public abstract class Entwiner
	{
		public readonly System.Type Twinee;
		public readonly AnyCall AnyCall;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="twinee">The type of the interface entwined by this entwiner.</param>
		/// <param name="any_call">The AnyCall delegate to invoke for each method invocation of the interface.</param>
		protected Entwiner( System.Type twinee, AnyCall any_call )
		{
			Twinee = twinee;
			AnyCall = any_call;
		}
	}
}
