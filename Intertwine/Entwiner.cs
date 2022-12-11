//Author MikeNakis (michael.gr)
namespace MikeNakis.Intertwine
{
    using Diagnostics = System.Diagnostics;
    using Emit = System.Reflection.Emit;
    using Generic = System.Collections.Generic;
    using Reflection = System.Reflection;

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
        /// <param name="anycall">The AnyCall delegate to invoke for each method invokation of the interface.</param>
        public Entwiner( System.Type twinee, AnyCall anycall )
        {
            Twinee  = twinee;
            AnyCall = anycall;
        }
    }
}
