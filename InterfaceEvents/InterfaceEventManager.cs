//Author MikeNakis (michael.gr)

namespace MikeNakis.Intertwine.InterfaceEvents
{
	using System.Linq;
	using MikeNakis.Intertwine;
	using Collections = System.Collections;
	using Generic = System.Collections.Generic;

	/// <summary>
	/// Manages an interface-event.
	/// </summary>
	/// <typeparam name="I">The type of interface of the event.</typeparam>
	public sealed class InterfaceEventManager<I> : IInterfaceEventSource<I> where I : class
	{
		private readonly Generic.List<Untwiner> untwiners = new Generic.List<Untwiner>();

		/// <summary>
		/// The trigger of the event. (Invoke this to cause the event to be triggered.)
		/// </summary>
		public readonly I Trigger;

		/// <summary>
		/// The event-source interface of the manager. (Use this to register/unregister observers of the event)
		/// </summary>
		public IInterfaceEventSource<I> Source => this;

		/// <summary>
		/// Constructor
		/// </summary>
		public InterfaceEventManager()
		{
			Trigger = Factory.NewEntwiner<I>( any_call );
		}

		public override string ToString() //Debug only
		{
#if DEBUG
			return untwiners.Count + " observers";
#else
            return null;
#endif
		}

		/// <inheritdoc/>
		void IInterfaceEventSource<I>.RegisterObserver( bool register, I observer )
		{
			if( register )
			{
				var untwiner = Factory.NewUntwiner( typeof(I), observer );
				lock( untwiners )
				{
					Dbg.Assert( !Source.IsObserverRegistered( observer ) ); //observer is already registered.
					untwiners.Add( untwiner );
				}
			}
			else
			{
				lock( untwiners )
				{
					var untwiner = find_untwiner_by_observer( observer );
					bool ok = untwiners.Remove( untwiner );
					Dbg.Assert( ok ); //observer was not registered.
				}
			}
		}

		/// <inheritdoc/>
		bool IInterfaceEventSource<I>.IsObserverRegistered( I observer )
		{
			return find_untwiner_by_observer( observer ) != null;
		}

		private Untwiner find_untwiner_by_observer( I observer )
		{
			foreach( var untwiner in untwiners )
				if( untwiner.Target == observer )
					return untwiner;
			return null;
		}

		private object any_call( int selector, object[] args )
		{
#if DEBUG
			/* On the debug build we do not catch exceptions, so as to have the debugger stop at the throwing statement. */
			int hashcode = get_hashcode( args );
			foreach( var untwiner in get_untwiners() )
			{
				untwiner.AnyCall( selector, args );
				Dbg.Assert( get_hashcode( args ) == hashcode ); // Ensure that the untwiner did not alter any arguments.
			}
			if( Dbg.False )
#endif
			{
				/* On the release build we catch all exceptions and discard them, because failure of one observer is no reason
				to prevent all subsequent observers from observing the event, let alone to cause the issuer of the event to fail. */
				foreach( var untwiner in get_untwiners() )
				{
					try
					{
						untwiner.AnyCall( selector, args );
					}
					catch( System.Exception e )
					{
						System.Diagnostics.Debug.WriteLine( e.ToString() );
					}
				}
			}
			return null; //events do not return anything.
		}

		private static int get_hashcode<T>( T arg )
		{
			if( arg.GetType().IsArray )
				return ((Collections.IStructuralEquatable)arg).GetHashCode( Generic.EqualityComparer<object>.Default );
			return arg.GetHashCode();
		}

		private Generic.List<Untwiner> get_untwiners()
		{
			lock( untwiners )
				return new Generic.List<Untwiner>( untwiners );
		}

#if DEBUG
		[System.Diagnostics.DebuggerBrowsable( System.Diagnostics.DebuggerBrowsableState.RootHidden )] public object[] DebugView => untwiners.Cast<object>().ToArray();
#endif
	}
}
