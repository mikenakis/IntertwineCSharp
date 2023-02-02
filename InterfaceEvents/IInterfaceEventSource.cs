//Author MikeNakis (michael.gr)

namespace MikeNakis.Intertwine.InterfaceEvents
{
	/// <summary>
	/// Defines the operations that can be performed on the source of an interface-event.
	/// </summary>
	/// <typeparam name="I">The type of interface of the event.</typeparam>
	public interface IInterfaceEventSource<in I> where I : class //actually, interface.
	{
		/// <summary>
		/// Registers or unregisters an observer.
		/// </summary>
		/// <param name="register">Indicates whether the observer should be registered or unregistered.</param>
		/// <param name="observer">The observer to register or unregister.</param>
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
		public static class InterfaceEventsExtensions
		{
			/// <summary>
			/// Registers an observer.
			/// </summary>
			/// <typeparam name="I">The type of interface of the event.</typeparam>
			/// <param name="source">The event-source with which to register the observer.</param>
			/// <param name="observer">The observer to register.</param>
			public static void RegisterObserver<I>( this IInterfaceEventSource<I> source, I observer ) where I : class //actually, interface.
			{
				source.RegisterObserver( true, observer );
			}

			/// <summary>
			/// Unregisters an observer.
			/// </summary>
			/// <typeparam name="I">The type of interface of the event.</typeparam>
			/// <param name="source">The event-source with which the observer has been registered.</param>
			/// <param name="observer">The observer to unregister.</param>
			public static void UnregisterObserver<I>( this IInterfaceEventSource<I> source, I observer ) where I : class //actually, interface.
			{
				source.RegisterObserver( false, observer );
			}
		}
	}
}
