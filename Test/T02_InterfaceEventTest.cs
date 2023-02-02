//Author MikeNakis (michael.gr)
// ReSharper disable InconsistentNaming

namespace MikeNakis.Intertwine.Test
{
	using MikeNakis.Intertwine.InterfaceEvents;
	using MikeNakis.Intertwine.InterfaceEvents.Extensions;
	using VsTesting = Microsoft.VisualStudio.TestTools.UnitTesting;
	using SysText = System.Text;

	public interface MyInterface
	{
		void A( int x );
		void B( int x, int y );
	}

	[VsTesting.TestClass]
	public class T02_InterfaceEventTest
	{
		private sealed class MyInterfaceEventObserver : MyInterface
		{
			private readonly string id;
			private readonly SysText.StringBuilder builder;

			public MyInterfaceEventObserver( string id, SysText.StringBuilder builder )
			{
				this.id = id;
				this.builder = builder;
			}

			public void A( int x )
			{
				builder.Append( id ).Append( $":{nameof(A)}(" ).Append( x ).Append( ")" );
			}

			public void B( int x, int y )
			{
				builder.Append( id ).Append( $":{nameof(B)}(" ).Append( x ).Append( "," ).Append( y ).Append( ")" );
			}
		}

		[VsTesting.TestMethod]
		public void InterfaceEventsTest()
		{
			IntertwineFactory intertwine_factory = new CachingIntertwineFactory( new CompilingIntertwineFactory() );
			var manager = new InterfaceEventManager<MyInterface>( intertwine_factory );
			var builder = new SysText.StringBuilder();
			manager.Source.RegisterObserver( new MyInterfaceEventObserver( "X", builder ) );
			manager.Source.RegisterObserver( new MyInterfaceEventObserver( "Y", builder ) );
			manager.Trigger.A( 1 );
			Dbg.Assert( builder.ToString() == $"X:{nameof(manager.Trigger.A)}(1)Y:{nameof(manager.Trigger.A)}(1)" );
			builder.Clear();
			manager.Trigger.B( 2, 3 );
			Dbg.Assert( builder.ToString() == $"X:{nameof(manager.Trigger.B)}(2,3)Y:{nameof(manager.Trigger.B)}(2,3)" );
		}
	}
}
