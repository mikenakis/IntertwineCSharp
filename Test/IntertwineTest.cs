//Author MikeNakis (michael.gr)
namespace MikeNakis.Intertwine.Test
{
	using UnitTesting = Microsoft.VisualStudio.TestTools.UnitTesting;

	[UnitTesting.TestClass]
	public class IntertwineTest
	{
		public IntertwineTest()
		{ 
		}

		private class FooImplementation<T>: IFooable<T>
		{
			private T Member;

			void IFooable<T>.Aardvark() { }
			T    IFooable<T>.Buffalo() { return Member; }
			void IFooable<T>.Crocodile( T t ) { Member = t; }
			void IFooable<T>.Dog( T t, out T ot ) { ot = t; }
			void IFooable<T>.Eagle( T t, ref T rt ) { Member = rt;  rt = t; }
			T    IFooable<T>.Flamingo { get { return Member; } set { Member = value; } }
			T    IFooable<T>.this[T t] { get { return t; } set { if( !Equals( t, default(T) ) ) Member = t; else Member = value; } }
		}

		private static void InvokeFoo<T>( IFooable<T> fooable, T t1, T t2 )
		{
			Dbg.Assert( !Equals( t1, t2 ) );
			Dbg.Assert( !Equals( t1, default(T) ) );
			Dbg.Assert( typeof(T) == typeof(bool) || !Equals( t2, default(T) ) );
			fooable.Aardvark();
			T a = fooable.Buffalo(); 
			Dbg.Assert( Equals( a, default(T) ) );
			fooable.Crocodile( t1 ); 
			a = fooable.Buffalo();
			Dbg.Assert( Equals( a, t1 ) );
			a = default(T);
			fooable.Dog( t1, out a ); 
			Dbg.Assert( Equals( a, t1 ) );
			a = t1;
			fooable.Eagle( t2, ref a ); 
			T b = fooable.Buffalo();
			Dbg.Assert( Equals( b, t1 ) );
			Dbg.Assert( Equals( a, t2 ) );
			fooable.Flamingo = t2;
			a = fooable.Flamingo;
			Dbg.Assert( Equals( a, t2 ) );
			a = fooable[t1];
			Dbg.Assert( Equals( a, t1 ) );
			a = fooable[t2];
			Dbg.Assert( Equals( a, t2 ) );
			fooable[t1] = default(T);
			a = fooable.Buffalo();
			Dbg.Assert( Equals( a, t1 ) );
			fooable[default(T)] = t2;
			a = fooable.Buffalo();
			Dbg.Assert( Equals( a, t2 ) );
		}

		private enum Method
		{
			Direct,
			Handwritten,
			Intertwine
		}

		private static void Run<T>( Method method, T t1, T t2 )
		{
			System.Diagnostics.Trace.WriteLine( method.ToString() + ": " + typeof(T).Name );
			IFooable<T> fooable = new FooImplementation<T>();
			switch( method )
			{
				case Method.Direct:
					break;
				case Method.Handwritten:
				{
					AnyCall untwiner = new UntwinerForFooableHandwritten<T>( fooable ).AnyCall;
					Intermediary intermediary = new Intermediary( untwiner );
					var entwiner = new EntwinerForFooableHandwritten<T>( intermediary.AnyCall );
					fooable = entwiner;
					break;
				}
				case Method.Intertwine:
				{
					AnyCall untwiner = Factory.NewUntwiner<IFooable<T>>( fooable );
					Intermediary intermediary = new Intermediary( untwiner );
					var entwiner = Factory.NewEntwiner<IFooable<T>>( intermediary.AnyCall );
					fooable = entwiner;
					break;
				}
				default:
					Dbg.Breakpoint();
					break;
			}
			InvokeFoo( fooable, t1, t2 );
		}

		private static void Run( Method method )
		{
			System.Diagnostics.Trace.WriteLine( method.ToString() + " ----------------------------------------------------------------" );
			Run<bool>           ( method, true, false );
			Run<sbyte>          ( method, -120, 65 );
			Run<byte>           ( method, 1, 240 );
			Run<short>          ( method, -32000, 8000 );
			Run<ushort>         ( method, 48000, 65000 );
			Run<int>            ( method, -1000000, 1000001 );
			Run<uint>           ( method, 1000000, 2000000 );
			Run<long>           ( method, -6000000000L, 6000000000L );
			Run<ulong>          ( method, 6000000000L, 7000000000L );
			Run<float>          ( method, 1.618034f, 3.14159f );
			Run<double>         ( method, 1.618034, 3.14159 );
			Run<System.DateTime>( method, new System.DateTime( 1969, 4, 8, 17, 34, 29 ), new System.DateTime( 2011, 06, 01, 23, 25, 12 ) );
			Run<System.TimeSpan>( method, new System.TimeSpan( 1, 38, 42 ), new System.TimeSpan( 12, 11, 2 ) );
			Run<System.Guid>    ( method, new System.Guid( "473df440-c936-4c27-9e9a-02219ab38b13" ), new System.Guid( "43806818-1DD7-44FE-9BD7-736E44C9030D" ) );
			Run<string>         ( method, "fubar!", "spoon!" );
			Run<SomeBigStruct>  ( method, new SomeBigStruct( 100 ), new SomeBigStruct( 225 ) );
		}

		[UnitTesting.TestMethod]
		public void IntertwineTestDirect()
		{
			Run( Method.Direct );
		}

		[UnitTesting.TestMethod]
		public void IntertwineTestHandwritten()
		{
			Run( Method.Handwritten );
		}

		[UnitTesting.TestMethod]
		public void IntertwineTestIntertwine()
		{
			Run( Method.Intertwine );
		}
	}

	sealed class EntwinerForFooableHandwritten<T>: MikeNakis.Intertwine.Entwiner, IFooable<T>
	{
		public EntwinerForFooableHandwritten( AnyCall anycall ) 
			:base( typeof(IFooable<T>), anycall )
		{ 
		}

		private static readonly object[] NoObjects = new object[0];

		void IFooable<T>.Aardvark ()                { AnyCall( 0, NoObjects ); } //TODO: use this optimization in Intertwine!
		T    IFooable<T>.Buffalo  ()                { return (T)AnyCall( 1, NoObjects ); }
		void IFooable<T>.Crocodile( T t )           { AnyCall( 2, new object[]{ t } ); }
		void IFooable<T>.Dog      ( T t, out T ot ) { var args = new object[]{ t, default(T) }; AnyCall( 3, args ); ot = (T)args[1]; }
		void IFooable<T>.Eagle    ( T t, ref T rt ) { var args = new object[]{ t, rt         }; AnyCall( 4, args ); rt = (T)args[1]; }
		T    IFooable<T>.Flamingo { get { return (T)AnyCall( 5, NoObjects ); } set { AnyCall( 6, new object[]{ value } ); } }
		T    IFooable<T>.this[T t] { get { return (T)AnyCall( 7, new object[]{ t } ); } set { AnyCall( 8, new object[]{ t, value } ); } }
	}

	sealed class UntwinerForFooableHandwritten<T>: Untwiner
	{
		private readonly IFooable<T> _Target;

		public UntwinerForFooableHandwritten( IFooable<T> target ) 
			:base( typeof(IFooable<T>) )
		{ 
			_Target = target;
		}

		public sealed override object Target 
		{ 
			get
			{
				return _Target;
			}
		}

		public sealed override object AnyCall( int selector, object[] args )
		{
			switch( selector )
			{
				default: throw new System.InvalidOperationException();
				case 0: _Target.Aardvark(); return null;
				case 1: return _Target.Buffalo();
				case 2: _Target.Crocodile( (T)args[0] ); return null;
				case 3: { T t = default(T); _Target.Dog( (T)args[0], out t ); args[1] = t; return null; }
				case 4: { T t = (T)args[1]; _Target.Eagle( (T)args[0], ref t ); args[1] = t; return null; }
				case 5: return _Target.Flamingo;
				case 6: _Target.Flamingo = (T)args[0]; return null;
				case 7: return _Target[(T)args[0]];
				case 8: _Target[(T)args[0]] = (T)args[1]; return null;
			}
		}
	}

	class Intermediary
	{
		private readonly AnyCall Target;

		public Intermediary( AnyCall target )
		{
			Target = target;
		}

		public object AnyCall( int selector, object[] arguments )
		{
			System.Console.Write( selector + " In:" + dump( selector, arguments ) );
			System.Console.Out.Flush();
			object result = Target( selector, arguments );
			System.Console.WriteLine( "  Out: " + dump( selector, arguments ) + ", result=" + result );
			return result;
		}

		private string dump( int selector, object[] arguments )
		{
			var builder = new System.Text.StringBuilder();
			for( int i = 0;  i < arguments.Length;  i++ )
			{
				if( i > 0 )
					builder.Append( ", " );
				builder.Append( "arg[" );
				builder.Append( i );
				builder.Append( "]=" );
				builder.Append( arguments[i] );
			}
			return builder.ToString();
		}
	}

	public struct SomeBigStruct
	{
		public readonly long A;
		public readonly long B;
		public readonly long C;
		public readonly long D;

		public SomeBigStruct( int i )
		{
			A = B = C = D = i;
		}

		public override int GetHashCode()
		{
			return A.GetHashCode() + B.GetHashCode() + C.GetHashCode() + D.GetHashCode();
		}

		public override bool Equals( object obj )
		{
			if( obj is SomeBigStruct )
				return Equals( (SomeBigStruct)obj );
			Dbg.Breakpoint();
			return false;
		}

		public bool Equals( SomeBigStruct other )
		{
			return A == other.A && B == other.B && C == other.C && D == other.D;
		}

		public override string ToString()
		{
			return A.ToString() + B.ToString() + C.ToString() + D.ToString();
		}
	}

	public interface IFooable<T>
	{
		void Aardvark();
		T    Buffalo();
		void Crocodile( T t );
		void Dog( T t, out T ot );
		void Eagle( T t, ref T rt );
		T    Flamingo{ get; set; }
		T    this[T t]{ get; set; }
//        void Gorilla<P>( P p, ref P rp ); //NOT SUPPORTED!
	}
}
