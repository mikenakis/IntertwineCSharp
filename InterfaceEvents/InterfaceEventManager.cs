//Author MikeNakis (michael.gr)
namespace MikeNakis.InterfaceEvents
{
    using Collections = System.Collections;
    using Generic = System.Collections.Generic;

    /// <summary>
    /// Manages an interface-event.
    /// </summary>
    /// <typeparam name="I">The type of interface of the event.</typeparam>
    public sealed class InterfaceEventManager<I>: IInterfaceEventSource<I>
        where I: class
    {
        private readonly Generic.List<Intertwine.Untwiner> Untwiners = new Generic.List<Intertwine.Untwiner>();

        /// <summary>
        /// The trigger of the event. (Invoke this to cause the event to be triggered.)
        /// </summary>
        public readonly I Trigger;

        /// <summary>
        /// The event-source interface of the manager. (Use this to register/deregister observers of the event)
        /// </summary>
        public IInterfaceEventSource<I> Source { get { return this; } }

        /// <summary>
        /// Constructor
        /// </summary>
        public InterfaceEventManager()
        {
            Trigger = Intertwine.Factory.NewEntwiner<I>( anycall );
        }

        public override string ToString() //Debug only
        {
#if DEBUG
            return Untwiners.Count + " observers";
#else
            return null;
#endif
        }

        /// <inheritdoc/>
        void IInterfaceEventSource<I>.RegisterObserver( bool register, I observer )
        {
            if( register )
            {
                var untwiner = Intertwine.Factory.NewUntwiner( typeof(I), observer );
                lock( Untwiners )
                { 
                    Dbg.Assert( !Source.IsObserverRegistered( observer ) ); //observer is already registered.
                    Untwiners.Add( untwiner );
                }
            }
            else
            {
                lock( Untwiners )
                {
                    var untwiner = find_untwiner_by_observer( observer );
                    bool ok = Untwiners.Remove( untwiner );
                    Dbg.Assert( ok ); //observer was not registered.
                }
            }
        }

        /// <inheritdoc/>
        bool IInterfaceEventSource<I>.IsObserverRegistered( I observer )
        {
            return find_untwiner_by_observer( observer ) != null;
        }

        private Intertwine.Untwiner find_untwiner_by_observer( I observer )
        {
            foreach( var untwiner in Untwiners )
                if( untwiner.Target == observer )
                    return untwiner;
            return null;
        }

        private object anycall( int selector, object[] args )
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

        private static int get_hashcode<T>( T[] args )
        {
            int hashcode = 17;
            foreach( T arg in args )
            {
                if( arg == null )
                    continue;
                hashcode += 31 * get_hashcode( arg );
            }
            return hashcode;
        }

        private static int get_hashcode<T>( T arg )
        {
            if( arg.GetType().IsArray )
                return ((Collections.IStructuralEquatable)arg).GetHashCode( Generic.EqualityComparer<object>.Default );
            return arg.GetHashCode();
        }

        private Generic.List<Intertwine.Untwiner> get_untwiners()
        {
            lock( Untwiners )
                return new Generic.List<Intertwine.Untwiner>( Untwiners );
        }

#if DEBUG
        [System.Diagnostics.CodeAnalysis.SuppressMessage( "Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays" )]
        [System.Diagnostics.CodeAnalysis.SuppressMessage( "Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode" )]
        [System.Diagnostics.DebuggerBrowsable( System.Diagnostics.DebuggerBrowsableState.RootHidden )]
        public object[] DebugView
        {
            get
            {
                return Untwiners.ToArray();
            }
        }
#endif
    }
}
