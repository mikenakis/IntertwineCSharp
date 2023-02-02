//Author MikeNakis (michael.gr)

#nullable enable
namespace MikeNakis.Intertwine
{
	///<author>michael.gr</author>
	public static class Dbg
	{
		[System.Diagnostics.CodeAnalysis.SuppressMessage( "Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors" )]
		public class AssertionFailureException : System.Exception
		{
			public AssertionFailureException()
			{ }
		}

		[System.Diagnostics.DebuggerStepThrough, System.Diagnostics.DebuggerHidden, System.Diagnostics.Conditional( "DEBUG" )]
		public static void Assert( bool expression )
		{
			if( expression )
				return;
			if( System.Diagnostics.Debugger.IsAttached )
				System.Diagnostics.Debugger.Break();
			else
				throw new AssertionFailureException();
		}

		[System.Diagnostics.DebuggerStepThrough, System.Diagnostics.DebuggerHidden]
		public static T NotNull<T>( T? nullable ) where T : class
		{
			Assert( nullable != null );
			return nullable!;
		}

		[System.Diagnostics.DebuggerStepThrough, System.Diagnostics.DebuggerHidden, System.Diagnostics.Conditional( "DEBUG" )]
		public static void Breakpoint()
		{
			if( System.Diagnostics.Debugger.IsAttached )
				System.Diagnostics.Debugger.Break();
		}

		public static bool True => true;

		public static bool False => false;

		public static bool Debug
		{
			get
			{
#if DEBUG
				return true;
#else
				return false;
#endif
			}
		}
	}
}
