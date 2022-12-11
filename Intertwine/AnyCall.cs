//Author MikeNakis (michael.gr)
namespace MikeNakis.Intertwine
{
    /// <summary>
    /// Defines a universal method suitable for expressing any interface method call.
    /// </summary>
    /// <param name="selector">The ordinal number of the interface method being called.</param>
    /// <param name="arguments">An array of objects containing the arguments of the call.</param>
    /// <returns>An object representing the return value of the method call.</returns>
    public delegate object AnyCall( int selector, object[] arguments );
}
