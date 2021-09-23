using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;

namespace RG.MAUI.Redux.Internals {
	internal static class ExceptionHelpers {
		[ExcludeFromCodeCoverage]
		public static void Throw(this Exception exception) =>
			ExceptionDispatchInfo.Capture(exception).Throw();
	}
}
