//Author MikeNakis (michael.gr)

namespace MikeNakis.Intertwine.Test
{
	using System.Collections.Generic;
	using Sys = System;
	using VsTesting = Microsoft.VisualStudio.TestTools.UnitTesting;

	[VsTesting.TestClass]
	public class IntertwineTest
	{
		public IntertwineTest()
		{ }

		private sealed class FooImplementation<T> : IFooable<T>
		{
			private T member;

			void IFooable<T>.Aardvark()
			{ }

			T IFooable<T>.Buffalo() => member;
			void IFooable<T>.Crocodile( T t ) => member = t;
			void IFooable<T>.Dog( T t, out T ot ) => ot = t;
			T IFooable<T>.Flamingo { get => member; set => member = value; }
			T IFooable<T>.this[ T t ] { get => t; set => member = !Equals( t, default(T) ) ? t : value; }

			void IFooable<T>.Eagle( T t, ref T rt )
			{
				member = rt;
				rt = t;
			}
		}

		private static void invoke_foo<T>( IFooable<T> fooable, T t1, T t2 )
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
			// ReSharper disable once RedundantAssignment
			a = default;
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
			fooable[t1] = default;
			a = fooable.Buffalo();
			Dbg.Assert( Equals( a, t1 ) );
			fooable[default] = t2;
			a = fooable.Buffalo();
			Dbg.Assert( Equals( a, t2 ) );
		}

		private enum Method
		{
			Direct,
			Handwritten,
			Intertwine
		}

		private static void run<T>( Method method, T t1, T t2 )
		{
			Sys.Diagnostics.Trace.WriteLine( $"{method}: {typeof(T).Name}" );
			IFooable<T> fooable = new FooImplementation<T>();
			switch( method )
			{
				case Method.Direct: break;
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
					AnyCall untwiner = Factory.NewUntwiner( fooable );
					Intermediary intermediary = new Intermediary( untwiner );
					var entwiner = Factory.NewEntwiner<IFooable<T>>( intermediary.AnyCall );
					fooable = entwiner;
					break;
				}
				default:
					Dbg.Breakpoint();
					break;
			}
			invoke_foo( fooable, t1, t2 );
		}

		private static void run( Method method )
		{
			Sys.Diagnostics.Trace.WriteLine( $"{method} ----------------------------------------------------------------" );
			run( method, true, false );
			run<sbyte>( method, -120, 65 );
			run<byte>( method, 1, 240 );
			run<short>( method, -32000, 8000 );
			run<ushort>( method, 48000, 65000 );
			run( method, -1000000, 1000001 );
			run<uint>( method, 1000000, 2000000 );
			run( method, -6000000000L, 6000000000L );
			run<ulong>( method, 6000000000L, 7000000000L );
			run( method, 1.618034f, 3.14159f );
			run( method, 1.618034, 3.14159 );
			run( method, new Sys.DateTime( 1969, 4, 8, 17, 34, 29 ), new Sys.DateTime( 2011, 06, 01, 23, 25, 12 ) );
			run( method, new Sys.TimeSpan( 1, 38, 42 ), new Sys.TimeSpan( 12, 11, 2 ) );
			run( method, new Sys.Guid( "473df440-c936-4c27-9e9a-02219ab38b13" ), new Sys.Guid( "43806818-1DD7-44FE-9BD7-736E44C9030D" ) );
			run( method, "foo!", "spoon!" );
			run( method, new SomeBigStruct( 100 ), new SomeBigStruct( 225 ) );
		}

		[VsTesting.TestMethod]
		public void IntertwineTestDirect()
		{
			run( Method.Direct );
		}

		[VsTesting.TestMethod]
		public void IntertwineTestHandwritten()
		{
			run( Method.Handwritten );
		}

		[VsTesting.TestMethod]
		public void IntertwineTestIntertwine()
		{
			run( Method.Intertwine );
		}
	}

	internal sealed class EntwinerForFooableHandwritten<T> : Entwiner, IFooable<T>
	{
		public EntwinerForFooableHandwritten( AnyCall any_call )
				: base( typeof(IFooable<T>), any_call )
		{ }

		void IFooable<T>.Aardvark() => AnyCall( 0, Sys.Array.Empty<object>() ); //TODO: use this optimization in Intertwine!
		T IFooable<T>.Buffalo() => (T)AnyCall( 1, Sys.Array.Empty<object>() );
		void IFooable<T>.Crocodile( T t ) => AnyCall( 2, new object[] { t } );
		T IFooable<T>.Flamingo { get => (T)AnyCall( 5, Sys.Array.Empty<object>() ); set => AnyCall( 6, new object[] { value } ); }
		T IFooable<T>.this[ T t ] { get => (T)AnyCall( 7, new object[] { t } ); set => AnyCall( 8, new object[] { t, value } ); }

		void IFooable<T>.Dog( T t, out T ot )
		{
			var args = new object[] { t, default(T) };
			AnyCall( 3, args );
			ot = (T)args[1];
		}

		void IFooable<T>.Eagle( T t, ref T rt )
		{
			var args = new object[] { t, rt };
			AnyCall( 4, args );
			rt = (T)args[1];
		}
	}

	internal sealed class UntwinerForFooableHandwritten<T> : Untwiner
	{
		private readonly IFooable<T> target;

		public UntwinerForFooableHandwritten( IFooable<T> target )
				: base( typeof(IFooable<T>) )
		{
			this.target = target;
		}

		public override object Target => target;

		public override object AnyCall( int selector, object[] args )
		{
			switch( selector )
			{
				case 0:
					target.Aardvark();
					return null;
				case 1: //
					return target.Buffalo();
				case 2:
					target.Crocodile( (T)args[0] );
					return null;
				case 3:
				{
					target.Dog( (T)args[0], out T t );
					args[1] = t;
					return null;
				}
				case 4:
				{
					T t = (T)args[1];
					target.Eagle( (T)args[0], ref t );
					args[1] = t;
					return null;
				}
				case 5: //
					return target.Flamingo;
				case 6:
					target.Flamingo = (T)args[0];
					return null;
				case 7: //
					return target[(T)args[0]];
				case 8:
					target[(T)args[0]] = (T)args[1];
					return null;
				default: //
					throw new Sys.InvalidOperationException();
			}
		}
	}

	internal class Intermediary
	{
		private readonly AnyCall target;

		public Intermediary( AnyCall target )
		{
			this.target = target;
		}

		public object AnyCall( int selector, object[] arguments )
		{
			Sys.Console.Write( selector + " In:" + dump( arguments ) );
			Sys.Console.Out.Flush();
			object result = target( selector, arguments );
			Sys.Console.WriteLine( "  Out: " + dump( arguments ) + ", result=" + result );
			return result;
		}

		private static string dump( IReadOnlyList<object> arguments )
		{
			var builder = new Sys.Text.StringBuilder();
			for( int i = 0; i < arguments.Count; i++ )
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

	public readonly struct SomeBigStruct
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
			if( obj is SomeBigStruct some_big_struct )
				return Equals( some_big_struct );
			Dbg.Breakpoint();
			return false;
		}

		public bool Equals( SomeBigStruct other )
		{
			return A == other.A && B == other.B && C == other.C && D == other.D;
		}

		public override string ToString()
		{
			return $"{A}{B}{C}{D}";
		}
	}

	public interface IFooable<T>
	{
		void Aardvark();
		T Buffalo();
		void Crocodile( T t );
		void Dog( T t, out T ot );
		void Eagle( T t, ref T rt );
		T Flamingo { get; set; }

		T this[ T t ] { get; set; }
//        void Gorilla<P>( P p, ref P rp ); //NOT SUPPORTED!
	}
}
