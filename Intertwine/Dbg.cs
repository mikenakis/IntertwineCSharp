//Author MikeNakis (michael.gr)
namespace MikeNakis
{
	public static class Dbg
	{
		[System.Diagnostics.CodeAnalysis.SuppressMessage( "Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors" )]
		public class ExceptionForAssertionFailure: System.Exception
		{
			public ExceptionForAssertionFailure()
			{
			}
		}

		[System.Diagnostics.DebuggerStepThrough]
		[System.Diagnostics.DebuggerHidden]
		[System.Diagnostics.Conditional("DEBUG")]
		public static void Assert( bool expression )
		{
			if( expression )
				return;
			if( System.Diagnostics.Debugger.IsAttached )
				System.Diagnostics.Debugger.Break();
			else
				throw new ExceptionForAssertionFailure();
		}

		[System.Diagnostics.DebuggerStepThrough]
		[System.Diagnostics.DebuggerHidden]
		[System.Diagnostics.Conditional("DEBUG")]
		public static void Breakpoint()
		{
			if( System.Diagnostics.Debugger.IsAttached )
				System.Diagnostics.Debugger.Break();
		}

		public static bool True { get { return true; } }

		public static bool False { get { return false; } }

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
