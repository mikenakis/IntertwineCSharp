//Author MikeNakis (michael.gr)
namespace MikeNakis.InterfaceEvents.Test
{
	using UnitTesting = Microsoft.VisualStudio.TestTools.UnitTesting;
	using Text = System.Text;
	using MikeNakis.InterfaceEvents.Extensions;


	public interface MyInterface
	{
		void a( int x );
		void b( int x, int y );
	}

	[UnitTesting.TestClass]
	public class InterfaceEventTest
	{
		public InterfaceEventTest()
		{
		}

		class MyInterfaceEventObserver: MyInterface
		{
			private readonly string id;
			private readonly Text.StringBuilder builder;

			public MyInterfaceEventObserver( string id, Text.StringBuilder builder )
			{
				this.id = id;
				this.builder = builder;
			}

			public void a( int x )
			{
				builder.Append( id ).Append( ":a(" ).Append( x ).Append( ")" );
			}

			public void b( int x, int y )
			{
				builder.Append( id ).Append( ":b(" ).Append( x ).Append( "," ).Append( y ).Append( ")" );
			}
		}

		[UnitTesting.TestMethod]
		public void InterfaceEventsTest()
		{
			var manager = new InterfaceEventManager<MyInterface>();
			var builder = new Text.StringBuilder();
			manager.Source.RegisterObserver( new MyInterfaceEventObserver( "X", builder ) );
			manager.Source.RegisterObserver( new MyInterfaceEventObserver( "Y", builder ) );
			manager.Trigger.a( 1 );
			Dbg.Assert( builder.ToString() == "X:a(1)Y:a(1)" );
			builder.Clear();
			manager.Trigger.b( 2, 3 );
			Dbg.Assert( builder.ToString() == "X:b(2,3)Y:b(2,3)" );
		}
	}
}
