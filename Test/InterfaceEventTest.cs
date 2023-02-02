//Author MikeNakis (michael.gr)

namespace MikeNakis.Intertwine.Test
{
	using MikeNakis.Intertwine;
	using MikeNakis.Intertwine.InterfaceEvents;
	using MikeNakis.Intertwine.InterfaceEvents.Extensions;
	using UnitTesting = Microsoft.VisualStudio.TestTools.UnitTesting;
	using Text = System.Text;

	public interface MyInterface
	{
		void A( int x );
		void B( int x, int y );
	}

	[UnitTesting.TestClass]
	public class InterfaceEventTest
	{
		public InterfaceEventTest()
		{ }

		private sealed class MyInterfaceEventObserver : MyInterface
		{
			private readonly string id;
			private readonly Text.StringBuilder builder;

			public MyInterfaceEventObserver( string id, Text.StringBuilder builder )
			{
				this.id = id;
				this.builder = builder;
			}

			public void A( int x )
			{
				builder.Append( id ).Append( $":{nameof(A)}(" ).Append( nameof(x) ).Append( ")" );
			}

			public void B( int x, int y )
			{
				builder.Append( id ).Append( $":{nameof(B)}(" ).Append( nameof(x) ).Append( "," ).Append( nameof(y) ).Append( ")" );
			}
		}

		[UnitTesting.TestMethod]
		public void InterfaceEventsTest()
		{
			var manager = new InterfaceEventManager<MyInterface>();
			var builder = new Text.StringBuilder();
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
