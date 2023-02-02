//Author MikeNakis (michael.gr)

namespace MikeNakis.Intertwine.Benchmark
{
	using Sys = System;
	using System.Collections.Generic;
	using CastleDP = Castle.DynamicProxy;
	using SysDiag = System.Diagnostics;
	using LinFuDP = LinFu.DynamicProxy;
	using SysReflect = System.Reflection;
	using Math = System.Math;

	public class Benchmarks
	{
		const double microseconds_per_second = 1000000.0;

		public static void Main( string[] args )
		{
			perform_benchmark( 1000, typeof(BenchmarkBlank) );
			perform_benchmark( 1, typeof(BenchmarkIntertwineCreationWithoutCaching) );
			perform_benchmark( 1, typeof(BenchmarkLinFuCreationWithoutCaching) );
			perform_benchmark( 10, typeof(BenchmarkIntertwineCreationWithCaching) );
			perform_benchmark( 10, typeof(BenchmarkLinFuCreationWithCaching) );
			perform_benchmark( 10, typeof(BenchmarkCastleCreation) );
			perform_benchmark( 10, typeof(BenchmarkDirectInvocation) );
			perform_benchmark( 10, typeof(BenchmarkHandwrittenInvocation) );
			perform_benchmark( 10, typeof(BenchmarkIntertwineInvocation) );
			perform_benchmark( 10, typeof(BenchmarkLinFuInvocation) );
			perform_benchmark( 10, typeof(BenchmarkCastleInvocation) );
		}

		private static void perform_benchmark( int grouping, System.Type type_of_benchmark )
		{
			perform_benchmark( 0.0, 100, type_of_benchmark ); //warmup
			double result = perform_benchmark( 1.0, grouping, type_of_benchmark );
			string title = type_of_benchmark.Name.PadRight( 48, '.' );
			System.Diagnostics.Trace.TraceInformation( "> {0}: {1,9:.000} μs/op", title, result * microseconds_per_second );
		}

		private static double perform_benchmark( double duration_seconds, int grouping, System.Type type_of_benchmark )
		{
			Dbg.Assert( typeof(Benchmark).IsAssignableFrom( type_of_benchmark ) );
			double minimum_ticks = long.MaxValue;
			using( var benchmark = (Benchmark)System.Activator.CreateInstance( type_of_benchmark ) )
			{
				long current_ticks = SysDiag.Stopwatch.GetTimestamp();
				long ending_ticks = current_ticks + (long)(duration_seconds * SysDiag.Stopwatch.Frequency);
				for( long last_ticks = current_ticks; current_ticks <= ending_ticks; last_ticks = current_ticks )
				{
					for( int i = 0; i < grouping; i++ )
						benchmark.RunOnce();
					current_ticks = SysDiag.Stopwatch.GetTimestamp();
					double duration = (current_ticks - last_ticks) / (double)grouping;
					minimum_ticks = Math.Min( minimum_ticks, duration );
				}
			}
			return minimum_ticks / SysDiag.Stopwatch.Frequency;
		}

		private abstract class Benchmark : Sys.IDisposable
		{
			public abstract void RunOnce();

			public virtual void Dispose()
			{ }
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
		}

		private sealed class FooImplementation<T> : IFooable<T>
		{
			private T member;

			void IFooable<T>.Aardvark() => member = default;
			T IFooable<T>.Buffalo() => member;
			void IFooable<T>.Crocodile( T t ) => member = t;
			void IFooable<T>.Dog( T t, out T ot ) => ot = t;

			void IFooable<T>.Eagle( T t, ref T rt )
			{
				member = rt;
				rt = t;
			}

			T IFooable<T>.Flamingo { get => member; set => member = value; }
			T IFooable<T>.this[ T t ] { get => t; set => member = !Equals( t, default(T) ) ? t : value; }
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

		private sealed class BenchmarkBlank : Benchmark
		{
			public override void RunOnce()
			{ }
		}

		private sealed class BenchmarkDirectInvocation : Benchmark
		{
			private readonly IFooable<string> fooable;

			public BenchmarkDirectInvocation()
			{
				fooable = new FooImplementation<string>();
			}

			public override void RunOnce()
			{
				invoke_foo( fooable, "a", "b" );
			}
		}

		private sealed class BenchmarkIntertwineCreationWithCaching : Benchmark
		{
			private readonly IFooable<string> fooable;

			public BenchmarkIntertwineCreationWithCaching()
			{
				fooable = new FooImplementation<string>();
				Factory.IsSaving = false;
			}

			public override void Dispose()
			{
				base.Dispose();
				Factory.IsSaving = true;
			}

			public override void RunOnce()
			{
				AnyCall untwiner = Factory.NewUntwiner( fooable );
				Factory.NewEntwiner<IFooable<string>>( untwiner );
			}
		}

		private sealed class BenchmarkIntertwineCreationWithoutCaching : Benchmark
		{
			private readonly IFooable<string> fooable;

			public BenchmarkIntertwineCreationWithoutCaching()
			{
				fooable = new FooImplementation<string>();
				Factory.IsSaving = false;
				Factory.IsCaching = false;
			}

			public override void Dispose()
			{
				base.Dispose();
				Factory.IsCaching = true;
				Factory.IsSaving = true;
			}

			public override void RunOnce()
			{
				AnyCall untwiner = Factory.NewUntwiner( fooable );
				Factory.NewEntwiner<IFooable<string>>( untwiner );
			}
		}

		private sealed class BenchmarkIntertwineInvocation : Benchmark
		{
			private readonly IFooable<string> entwiner;

			public BenchmarkIntertwineInvocation()
			{
				IFooable<string> fooable = new FooImplementation<string>();
				AnyCall untwiner = Factory.NewUntwiner( fooable );
				entwiner = Factory.NewEntwiner<IFooable<string>>( untwiner );
			}

			public override void RunOnce()
			{
				invoke_foo( entwiner, "a", "b" );
			}
		}

#region Handwrittern //////////////////////////////////////////////////////////////////////
		public sealed class HandwrittenFooableEntwiner<T> : Entwiner, IFooable<T>
		{
			public HandwrittenFooableEntwiner( AnyCall any_call )
					: base( typeof(IFooable<T>), any_call )
			{ }

			void IFooable<T>.Aardvark() => AnyCall( 0, Sys.Array.Empty<object>() );
			T IFooable<T>.Buffalo() => (T)AnyCall( 1, Sys.Array.Empty<object>() );
			void IFooable<T>.Crocodile( T t ) => AnyCall( 2, new object[] { t } );

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

			T IFooable<T>.Flamingo { get => (T)AnyCall( 5, Sys.Array.Empty<object>() ); set => AnyCall( 6, new object[] { value } ); }
			T IFooable<T>.this[ T t ] { get => (T)AnyCall( 7, new object[] { t } ); set => AnyCall( 8, new object[] { t, value } ); }
		}

		private sealed class HandwrittenFooableUntwiner<T> : Untwiner
		{
			private readonly IFooable<T> target;

			public HandwrittenFooableUntwiner( IFooable<T> target )
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

		private sealed class BenchmarkHandwrittenInvocation : Benchmark
		{
			private readonly IFooable<string> entwiner;

			public BenchmarkHandwrittenInvocation()
			{
				IFooable<string> fooable = new FooImplementation<string>();
				Untwiner untwiner = new HandwrittenFooableUntwiner<string>( fooable );
				entwiner = new HandwrittenFooableEntwiner<string>( untwiner.AnyCall );
			}

			public override void RunOnce()
			{
				invoke_foo( entwiner, "a", "b" );
			}
		}
#endregion

#region Castle Benchmarking Routines //////////////////////////////////////////////////////
		private sealed class BenchmarkCastleCreation/*WithCaching*/ : Benchmark
		{
			private readonly IFooable<string> fooable;

			public BenchmarkCastleCreation/*WithCaching*/()
			{
				fooable = new FooImplementation<string>();
				//CastleProxyGenerator.ProxyBuilder.ModuleScope.IsCaching = true;  This used to compile, but not anymore. It appears that they discontinued the "IsCaching" property.
			}

			public override void RunOnce()
			{
				Untwiner untwiner = CastleGetUntwiner( fooable );
				CastleGetEntwiner<IFooable<string>>( untwiner.AnyCall );
			}
		}

		private sealed class BenchmarkCastleInvocation : Benchmark
		{
			private readonly IFooable<string> entwiner;

			public BenchmarkCastleInvocation()
			{
				IFooable<string> fooable = new FooImplementation<string>();
				Untwiner untwiner = CastleGetUntwiner( fooable );
				entwiner = CastleGetEntwiner<IFooable<string>>( untwiner.AnyCall );
			}

			public override void RunOnce()
			{
				invoke_foo( entwiner, "a", "b" );
			}
		}

		internal static CastleDP.ProxyGenerator CastleProxyGenerator = new CastleDP.ProxyGenerator();

		public static T CastleGetEntwiner<T>( AnyCall any_call ) where T : class //actually, interface
		{
			CastleDP.IInterceptor interceptor = new EntwinerForCastle( typeof(T), any_call );
			return CastleProxyGenerator.CreateInterfaceProxyWithoutTarget<T>( interceptor );
		}

		public static Untwiner CastleGetUntwiner<T>( T target ) where T : class //actually, interface
		{
			// The Castle DynamicProxy does not offer any untwining functionality, so we use a reflecting untwiner.
			return new ReflectingUntwiner( typeof(T), target );
		}

		private sealed class EntwinerForCastle : Entwiner, CastleDP.IInterceptor
		{
			private readonly Dictionary<SysReflect.MethodInfo, int> selector_map = new Dictionary<SysReflect.MethodInfo, int>();

			public EntwinerForCastle( System.Type twinee, AnyCall any_call )
					: base( twinee, any_call )
			{
				var method_infos = Twinee.GetMethods( SysReflect.BindingFlags.Public | SysReflect.BindingFlags.NonPublic | SysReflect.BindingFlags.Instance );
				for( int i = 0; i < method_infos.Length; i++ )
					selector_map.Add( method_infos[i], i );
			}

			void CastleDP.IInterceptor.Intercept( CastleDP.IInvocation invocation )
			{
				int selector = selector_map[invocation.Method];
				invocation.ReturnValue = AnyCall( selector, invocation.Arguments );
			}
		}
#endregion

#region LinFu Benchmarking Routines ///////////////////////////////////////////////////////
		private sealed class BenchmarkLinFuCreationWithCaching : Benchmark
		{
			private readonly IFooable<string> fooable;

			public BenchmarkLinFuCreationWithCaching()
			{
				fooable = new FooImplementation<string>();
				LinFuIsCaching = true;
			}

			public override void RunOnce()
			{
				AnyCall untwiner = LinFuGetUntwiner( fooable );
				LinFuGetEntwiner<IFooable<string>>( untwiner );
			}
		}

		private sealed class BenchmarkLinFuCreationWithoutCaching : Benchmark
		{
			private readonly IFooable<string> fooable;

			public BenchmarkLinFuCreationWithoutCaching()
			{
				fooable = new FooImplementation<string>();
				LinFuIsCaching = false;
			}

			public override void Dispose()
			{
				base.Dispose();
				LinFuIsCaching = true;
			}

			public override void RunOnce()
			{
				AnyCall untwiner = LinFuGetUntwiner( fooable );
				LinFuGetEntwiner<IFooable<string>>( untwiner );
			}
		}

		private sealed class BenchmarkLinFuInvocation : Benchmark
		{
			private readonly IFooable<string> entwiner;

			public BenchmarkLinFuInvocation()
			{
				IFooable<string> fooable = new FooImplementation<string>();
				AnyCall untwiner = LinFuGetUntwiner( fooable );
				entwiner = LinFuGetEntwiner<IFooable<string>>( untwiner );
			}

			public override void RunOnce()
			{
				invoke_foo( entwiner, "a", "b" );
			}
		}

		internal static LinFuDP.ProxyFactory LinFuProxyFactory = new LinFuDP.ProxyFactory();

		internal class LinFuDummyProxyCache : LinFuDP.IProxyCache
		{
			bool LinFuDP.IProxyCache.Contains( System.Type base_type, params System.Type[] base_interfaces )
			{
				return false;
			}

			System.Type LinFuDP.IProxyCache.GetProxyType( System.Type base_type, params System.Type[] base_interfaces )
			{
				return null;
			}

			void LinFuDP.IProxyCache.StoreProxyType( System.Type result, System.Type base_type, params System.Type[] base_interfaces )
			{ }
		}

		public static bool LinFuIsCaching
		{
			get => LinFuProxyFactory.Cache.GetType() == typeof(LinFuDP.ProxyCache);
			set
			{
				if( value )
					LinFuProxyFactory.Cache = new LinFuDP.ProxyCache();
				else
					LinFuProxyFactory.Cache = new LinFuDummyProxyCache();
			}
		}

		public static T LinFuGetEntwiner<T>( AnyCall any_call )
		{
			LinFuDP.IInterceptor interceptor = new EntwinerForLinFu( typeof(T), any_call );
			return LinFuProxyFactory.CreateProxy<T>( interceptor );
		}

		public static AnyCall LinFuGetUntwiner<T>( T target )
		{
			// The LinFu DynamicProxy does not offer any untwining functionality, so we use a reflecting untwiner.
			return new ReflectingUntwiner( typeof(T), target ).AnyCall;
		}

		private sealed class EntwinerForLinFu : Entwiner, LinFuDP.IInterceptor
		{
			private readonly Dictionary<SysReflect.MethodInfo, int> selector_map = new Dictionary<SysReflect.MethodInfo, int>();

			public EntwinerForLinFu( System.Type twinee, AnyCall any_call )
					: base( twinee, any_call )
			{
				var method_infos = Twinee.GetMethods( SysReflect.BindingFlags.Public | SysReflect.BindingFlags.NonPublic | SysReflect.BindingFlags.Instance );
				for( int i = 0; i < method_infos.Length; i++ )
					selector_map.Add( method_infos[i], i );
			}

			object LinFuDP.IInterceptor.Intercept( LinFuDP.InvocationInfo info )
			{
				int selector = selector_map[info.TargetMethod];
				return AnyCall( selector, info.Arguments );
			}
		}
#endregion
	}
}
