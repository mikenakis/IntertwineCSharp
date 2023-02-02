//Author MikeNakis (michael.gr)

namespace MikeNakis.Intertwine.InterfaceEvents
{
	using System.Collections.Generic;
	using MikeNakis.Intertwine;
	using Collections = System.Collections;
	using SysDiag = System.Diagnostics;

	/// <summary>
	/// Manages an interface-event.
	/// </summary>
	/// <typeparam name="I">The type of interface of the event.</typeparam>
	[SysDiag.DebuggerDisplay( "{" + nameof(DebugView) + ",nq}" )]
	public sealed class InterfaceEventManager<I> : IInterfaceEventSource<I> where I : class
	{
		[SysDiag.DebuggerBrowsable( SysDiag.DebuggerBrowsableState.RootHidden )] public AnyCall[] DebugView => untwiners.ToArray();
		private readonly IntertwineFactory intertwine_factory;
		private readonly List<AnyCall> untwiners = new List<AnyCall>();

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
		public InterfaceEventManager( IntertwineFactory intertwine_factory )
		{
			this.intertwine_factory = intertwine_factory;
			Trigger = intertwine_factory.GetIntertwine<I>().NewEntwiningInstance( any_call );
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
				AnyCall untwiner = intertwine_factory.GetIntertwine<I>().NewUntwiningInstance( observer );
				Dbg.Assert( !Source.IsObserverRegistered( observer ) ); //observer is already registered.
				untwiners.Add( untwiner );
			}
			else
			{
				var untwiner = find_untwiner_by_observer( observer );
				bool ok = untwiners.Remove( untwiner );
				Dbg.Assert( ok ); //observer was not registered.
			}
		}

		/// <inheritdoc/>
		bool IInterfaceEventSource<I>.IsObserverRegistered( I observer )
		{
			return find_untwiner_by_observer( observer ) != null;
		}

		private AnyCall find_untwiner_by_observer( I observer )
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
			foreach( var untwiner in untwiners.ToArray() )
			{
				untwiner.Invoke( selector, args );
				Dbg.Assert( get_hashcode( args ) == hashcode ); // Ensure that the untwiner did not alter any arguments.
			}
			if( Dbg.False )
#endif
			{
				/* On the release build we catch all exceptions and discard them, because failure of one observer is no reason
				to prevent all subsequent observers from observing the event, let alone to cause the issuer of the event to fail. */
				foreach( var untwiner in untwiners.ToArray() )
				{
					try
					{
						untwiner.Invoke( selector, args );
					}
					catch( System.Exception e )
					{
						SysDiag.Debug.WriteLine( e.ToString() );
					}
				}
			}
			return null; //events do not return anything.
		}

		private static int get_hashcode( object[] arg )
		{
			return ((Collections.IStructuralEquatable)arg).GetHashCode( EqualityComparer<object>.Default );
		}
	}
}
