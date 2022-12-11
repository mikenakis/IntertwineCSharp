//Author MikeNakis (michael.gr)
namespace MikeNakis.Intertwine
{
	using CastleDP = Castle.DynamicProxy;
	using Diagnostics = System.Diagnostics;
	using Generic = System.Collections.Generic;
	using LinFuDP = LinFu.DynamicProxy;
	using Reflection = System.Reflection;
	using Math = System.Math;

	public class Benchmarks
	{
		const double MICROSECONDS_PER_SECOND = 1000000.0;

		public static void Main( string[] args )
		{
			perform_benchmark( 1000, typeof( Benchmark_Blank ) );
			perform_benchmark( 1, typeof( Benchmark_Intertwine_Creation_Without_Caching ) );
			perform_benchmark( 1, typeof( Benchmark_LinFu_Creation_Without_Caching ) );
		  //perform_benchmark( 1, typeof( Benchmark_Castle_Creation_Without_Caching ) ); //does not work anymore, since they got rid of the IsCaching property.
			perform_benchmark( 10, typeof( Benchmark_Intertwine_Creation_With_Caching ) );
			perform_benchmark( 10, typeof( Benchmark_LinFu_Creation_With_Caching ) );
			perform_benchmark( 10, typeof( Benchmark_Castle_Creation_With_Caching ) );
			perform_benchmark( 10, typeof( Benchmark_Direct_Invocation ) );
			perform_benchmark( 10, typeof( Benchmark_Handwritten_Invocation ) );
			perform_benchmark( 10, typeof( Benchmark_Intertwine_Invocation ) );
			perform_benchmark( 10, typeof( Benchmark_LinFu_Invocation ) );
			perform_benchmark( 10, typeof( Benchmark_Castle_Invocation ) );
		}

		private static void perform_benchmark( int grouping, System.Type type_of_benchmark )
		{
			perform_benchmark( 0.0, 100, type_of_benchmark ); //warmup
			double result = perform_benchmark( 1.0, grouping, type_of_benchmark );
			string title = type_of_benchmark.Name.PadRight( 48, '.' );
			System.Diagnostics.Trace.TraceInformation( "> {0}: {1,9:.000} μs/op", title, result * MICROSECONDS_PER_SECOND );
		}

		private static double perform_benchmark( double duration_seconds, int grouping, System.Type type_of_benchmark )
		{
			Dbg.Assert( typeof( Benchmark ).IsAssignableFrom( type_of_benchmark ) );
			double minimum_ticks = long.MaxValue;
			using( var benchmark = (Benchmark)System.Activator.CreateInstance( type_of_benchmark ) )
			{
				long current_ticks = Diagnostics.Stopwatch.GetTimestamp();
				long ending_ticks = current_ticks + (long)(duration_seconds * Diagnostics.Stopwatch.Frequency);
				for( long last_ticks = current_ticks;  current_ticks <= ending_ticks;  last_ticks = current_ticks )
				{
					for( int i = 0;  i < grouping;  i++ )
						benchmark.RunOnce();
					current_ticks = Diagnostics.Stopwatch.GetTimestamp();
					double duration = (current_ticks - last_ticks) / (double)grouping;
					minimum_ticks = Math.Min( minimum_ticks, duration );

				}
			}
			return minimum_ticks / Diagnostics.Stopwatch.Frequency;
		}

		private abstract class Benchmark: System.IDisposable
		{
			public abstract void RunOnce();
			public virtual void Dispose()
			{
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
			T this[T t] { get; set; }
		}

		private class FooImplementation<T>: IFooable<T>
		{
			private T Member;

			void IFooable<T>.Aardvark() { Member = default( T ); }
			T IFooable<T>.Buffalo() { return Member; }
			void IFooable<T>.Crocodile( T t ) { Member = t; }
			void IFooable<T>.Dog( T t, out T ot ) { ot = t; }
			void IFooable<T>.Eagle( T t, ref T rt ) { Member = rt; rt = t; }
			T IFooable<T>.Flamingo { get { return Member; } set { Member = value; } }
			T IFooable<T>.this[T t] { get { return t; } set { if( !Equals( t, default( T ) ) ) Member = t; else Member = value; } }
		}

		private static void InvokeFoo<T>( IFooable<T> fooable, T t1, T t2 )
		{
			Dbg.Assert( !Equals( t1, t2 ) );
			Dbg.Assert( !Equals( t1, default( T ) ) );
			Dbg.Assert( typeof( T ) == typeof( bool ) || !Equals( t2, default( T ) ) );
			fooable.Aardvark();
			T a = fooable.Buffalo();
			Dbg.Assert( Equals( a, default( T ) ) );
			fooable.Crocodile( t1 );
			a = fooable.Buffalo();
			Dbg.Assert( Equals( a, t1 ) );
			a = default( T );
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
			fooable[t1] = default( T );
			a = fooable.Buffalo();
			Dbg.Assert( Equals( a, t1 ) );
			fooable[default( T )] = t2;
			a = fooable.Buffalo();
			Dbg.Assert( Equals( a, t2 ) );
		}

		private class Benchmark_Blank: Benchmark
		{
			public override void RunOnce()
			{
			}
		}

		private class Benchmark_Direct_Invocation: Benchmark
		{
			IFooable<string> Fooable;

			public Benchmark_Direct_Invocation()
			{
				Fooable = new FooImplementation<string>();
			}

			public override void RunOnce()
			{
				InvokeFoo( Fooable, "a", "b" );
			}
		}

		private class Benchmark_Intertwine_Creation_With_Caching: Benchmark
		{
			IFooable<string> Fooable;

			public Benchmark_Intertwine_Creation_With_Caching()
			{
				Fooable = new FooImplementation<string>();
				Factory.IsSaving = false;
			}

			public override void Dispose()
			{
				base.Dispose();
				Factory.IsSaving = true;
			}

			public override void RunOnce()
			{
				AnyCall untwiner = Factory.NewUntwiner<IFooable<string>>( Fooable );
				IFooable<string> entwiner = Factory.NewEntwiner<IFooable<string>>( untwiner );
			}
		}

		private class Benchmark_Intertwine_Creation_Without_Caching: Benchmark
		{
			IFooable<string> Fooable;

			public Benchmark_Intertwine_Creation_Without_Caching()
			{
				Fooable = new FooImplementation<string>();
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
				AnyCall untwiner = Factory.NewUntwiner<IFooable<string>>( Fooable );
				IFooable<string> entwiner = Factory.NewEntwiner<IFooable<string>>( untwiner );
			}
		}

		private class Benchmark_Intertwine_Invocation: Benchmark
		{
			IFooable<string> Fooable;
			AnyCall Untwiner;
			IFooable<string> Entwiner;

			public Benchmark_Intertwine_Invocation()
			{
				Fooable = new FooImplementation<string>();
				Untwiner = Factory.NewUntwiner<IFooable<string>>( Fooable );
				Entwiner = Factory.NewEntwiner<IFooable<string>>( Untwiner );
			}

			public override void RunOnce()
			{
				InvokeFoo( Entwiner, "a", "b" );
			}
		}

		#region Handwrittern //////////////////////////////////////////////////////////////////////

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

		private class Benchmark_Handwritten_Invocation: Benchmark
		{
			private readonly IFooable<string> Entwiner;

			public Benchmark_Handwritten_Invocation()
			{
				IFooable<string> fooable = new FooImplementation<string>();
				Untwiner untwiner = new UntwinerForFooableHandwritten<string>( fooable );
				Entwiner = new EntwinerForFooableHandwritten<string>( untwiner.AnyCall );
			}

			public override void RunOnce()
			{
				InvokeFoo( Entwiner, "a", "b" );
			}
		}

		#endregion

		#region Castle Benchmarking Routines //////////////////////////////////////////////////////

		private class Benchmark_Castle_Creation_With_Caching: Benchmark
		{
			IFooable<string> Fooable;

			public Benchmark_Castle_Creation_With_Caching()
			{
				Fooable = new FooImplementation<string>();
				//CastleProxyGenerator.ProxyBuilder.ModuleScope.IsCaching = true;  This used to compile, but not anymore. It appears that they discontinued the "IsCaching" property.
			}

			public override void RunOnce()
			{
				Untwiner untwiner = CastleGetUntwiner<IFooable<string>>( Fooable );
				IFooable<string> entwiner = CastleGetEntwiner<IFooable<string>>( untwiner.AnyCall );
			}
		}

		private class Benchmark_Castle_Creation_Without_Caching: Benchmark
		{
			IFooable<string> Fooable;

			public Benchmark_Castle_Creation_Without_Caching()
			{
				Fooable = new FooImplementation<string>();
				//CastleProxyGenerator.ProxyBuilder.ModuleScope.IsCaching = false;  This used to compile, but not anymore. It appears that they discontinued the "IsCaching" property.
			}

			public override void Dispose()
			{
				base.Dispose();
				//CastleProxyGenerator.ProxyBuilder.ModuleScope.IsCaching = true;  This used to compile, but not anymore. It appears that they discontinued the "IsCaching" property.
			}

			public override void RunOnce()
			{
				Untwiner untwiner = CastleGetUntwiner<IFooable<string>>( Fooable );
				IFooable<string> entwiner = CastleGetEntwiner<IFooable<string>>( untwiner.AnyCall );
			}
		}

		private class Benchmark_Castle_Invocation: Benchmark
		{
			IFooable<string> Entwiner;

			public Benchmark_Castle_Invocation()
			{
				IFooable<string> fooable = new FooImplementation<string>();
				Untwiner untwiner = CastleGetUntwiner<IFooable<string>>( fooable );
				Entwiner = CastleGetEntwiner<IFooable<string>>( untwiner.AnyCall );
			}

			public override void RunOnce()
			{
				InvokeFoo( Entwiner, "a", "b" );
			}
		}

		internal static CastleDP.ProxyGenerator CastleProxyGenerator = new CastleDP.ProxyGenerator();

		public static T CastleGetEntwiner<T>( AnyCall anycall )
			where T: class //actually, interface
		{
			CastleDP.IInterceptor interceptor = new EntwinerForCastle( typeof( T ), anycall );
			return (T)(object)CastleProxyGenerator.CreateInterfaceProxyWithoutTarget<T>( interceptor );
		}

		public static Untwiner CastleGetUntwiner<T>( T target )
			where T: class //actually, interface
		{
			// The Castle DynamicProxy does not offer any untwining functionality, so we use a reflecting untwiner.
			return new ReflectingUntwiner( typeof( T ), target );
		}

		private class EntwinerForCastle: Entwiner, CastleDP.IInterceptor
		{
			private readonly Generic.Dictionary<Reflection.MethodInfo, int> SelectorMap = new Generic.Dictionary<Reflection.MethodInfo, int>();

			public EntwinerForCastle( System.Type twinee, AnyCall anycall )
				: base( twinee, anycall )
			{
				var methodinfos = Twinee.GetMethods( System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance );
				for( int i = 0; i < methodinfos.Length; i++ )
					SelectorMap.Add( methodinfos[i], i );
			}

			void CastleDP.IInterceptor.Intercept( CastleDP.IInvocation invocation )
			{
				int selector = SelectorMap[invocation.Method];
				invocation.ReturnValue = AnyCall( selector, invocation.Arguments );
			}
		}

		#endregion

		#region LinFu Benchmarking Routines ///////////////////////////////////////////////////////

		private class Benchmark_LinFu_Creation_With_Caching: Benchmark
		{
			IFooable<string> Fooable;

			public Benchmark_LinFu_Creation_With_Caching()
			{
				Fooable = new FooImplementation<string>();
				LinFuIsCaching = true;
			}

			public override void RunOnce()
			{
				AnyCall untwiner = LinFuGetUntwiner<IFooable<string>>( Fooable );
				IFooable<string> entwiner = LinFuGetEntwiner<IFooable<string>>( untwiner );
			}
		}

		private class Benchmark_LinFu_Creation_Without_Caching: Benchmark
		{
			IFooable<string> Fooable;

			public Benchmark_LinFu_Creation_Without_Caching()
			{
				Fooable = new FooImplementation<string>();
				LinFuIsCaching = false;
			}

			public override void Dispose()
			{
				base.Dispose();
				LinFuIsCaching = true;
			}

			public override void RunOnce()
			{
				AnyCall untwiner = LinFuGetUntwiner<IFooable<string>>( Fooable );
				IFooable<string> entwiner = LinFuGetEntwiner<IFooable<string>>( untwiner );
			}
		}

		private class Benchmark_LinFu_Invocation: Benchmark
		{
			IFooable<string> Fooable;
			AnyCall Untwiner;
			IFooable<string> Entwiner;

			public Benchmark_LinFu_Invocation()
			{
				Fooable = new FooImplementation<string>();
				Untwiner = LinFuGetUntwiner<IFooable<string>>( Fooable );
				Entwiner = LinFuGetEntwiner<IFooable<string>>( Untwiner );
			}

			public override void RunOnce()
			{
				InvokeFoo( Entwiner, "a", "b" );
			}
		}

		internal static LinFuDP.ProxyFactory LinFuProxyFactory = new LinFuDP.ProxyFactory();

		internal class LinFuDummyProxyCache: LinFuDP.IProxyCache
		{
			bool LinFuDP.IProxyCache.Contains( System.Type baseType, params System.Type[] baseInterfaces ) { return false; }
			System.Type LinFuDP.IProxyCache.GetProxyType( System.Type baseType, params System.Type[] baseInterfaces ) { return null; }
			void LinFuDP.IProxyCache.StoreProxyType( System.Type result, System.Type baseType, params System.Type[] baseInterfaces ) { }
		}

		public static bool LinFuIsCaching
		{
			get
			{
				return LinFuProxyFactory.Cache.GetType() == typeof( LinFuDP.ProxyCache );
			}
			set
			{
				if( value )
					LinFuProxyFactory.Cache = new LinFuDP.ProxyCache();
				else
					LinFuProxyFactory.Cache = new LinFuDummyProxyCache();
			}
		}

		public static T LinFuGetEntwiner<T>( AnyCall anycall )
		{
			LinFuDP.IInterceptor interceptor = new EntwinerForLinFu( typeof( T ), anycall );
			return (T)(object)LinFuProxyFactory.CreateProxy<T>( interceptor );
		}

		public static AnyCall LinFuGetUntwiner<T>( T target )
		{
			// The LinFu DynamicProxy does not offer any untwining functionality, so we use a reflecting untwiner.
			return new ReflectingUntwiner( typeof( T ), target ).AnyCall;
		}

		private class EntwinerForLinFu: Entwiner, LinFuDP.IInterceptor
		{
			private readonly Generic.Dictionary<Reflection.MethodInfo, int> SelectorMap = new Generic.Dictionary<Reflection.MethodInfo, int>();

			public EntwinerForLinFu( System.Type twinee, AnyCall anycall )
				: base( twinee, anycall )
			{
				var methodinfos = Twinee.GetMethods( System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance );
				for( int i = 0; i < methodinfos.Length; i++ )
					SelectorMap.Add( methodinfos[i], i );
			}

			object LinFuDP.IInterceptor.Intercept( LinFuDP.InvocationInfo info )
			{
				int selector = SelectorMap[info.TargetMethod];
				return AnyCall( selector, info.Arguments );
			}
		}

		#endregion

	}
}
