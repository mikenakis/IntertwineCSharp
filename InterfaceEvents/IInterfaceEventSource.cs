//Author MikeNakis (michael.gr)
namespace MikeNakis.InterfaceEvents
{
    /// <summary>
    /// Defines the operations that can be performed on the source of an interface-event.
    /// </summary>
    /// <typeparam name="I">The type of interface of the event.</typeparam>
    public interface IInterfaceEventSource<I>
        where I: class //actually, interface.
    {
        /// <summary>
        /// Registers or deregisters an observer.
        /// </summary>
        /// <param name="register">Indicates whether the observer should be registered or deregistered.</param>
        /// <param name="observer">The observer to register or deregister.</param>
        void RegisterObserver( bool register, I observer );

        /// <summary>
        /// Checks whether an observer is registered.
        /// </summary>
        /// <param name="observer">The observer to check.</param>
        /// <returns>true if the observer is registered; false otherwise.</returns>
        bool IsObserverRegistered( I observer );
    }

    namespace Extensions
    {
        public static partial class Extensions
        {
            /// <summary>
            /// Registers an observer.
            /// </summary>
            /// <typeparam name="I">The type of interface of the event.</typeparam>
            /// <param name="source">The event-source with which to register the observer.</param>
            /// <param name="observer">The observer to register.</param>
            public static void RegisterObserver<I>( this IInterfaceEventSource<I> source, I observer )
                where I: class //actually, interface.
            {
                source.RegisterObserver( true, observer );
            }

            /// <summary>
            /// Deregisters an observer.
            /// </summary>
            /// <typeparam name="I">The type of interface of the event.</typeparam>
            /// <param name="source">The event-source with which the observer has been registered.</param>
            /// <param name="observer">The observer to deregister.</param>
            public static void DeregisterObserver<I>( this IInterfaceEventSource<I> source, I observer )
                where I: class //actually, interface.
            {
                source.RegisterObserver( false, observer );
            }
        }
    }
}
