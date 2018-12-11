// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Tests
{
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;
	using System.Linq;
	using Xunit;

    public class TripleColonTest
    {
		static public string LoggerPhase = "TripleColon";

		[Fact]
        public void TripleColonTestGeneral()
        {
            var source = @"::: zone pivot=""windows""
    hello
::: zone-end
";
            var expected = @"<div class=""zone has-pivot"" data-pivot=""windows"">
<pre><code>hello
</code></pre>
</div>
".Replace("\r\n", "\n");

            TestUtility.AssertEqual(expected, source, TestUtility.MarkupWithoutSourceInfo);
        }

		[Fact]
		public void TripleColonTestNested()
		{
			var source = @"::: moniker range="".NET""
	::: zone pivot=""windows""
		hello

	::: zone-end
	::: form action=""create-resource"" submitText=""Create"" :::
::: end-moniker
";

			/*
			var expected = @"<div class=""zone has-pivot"" data-pivot=""windows"">
<pre><code>hello
</code></pre>
</div>
".Replace("\r\n", "\n");
*/

			TestUtility.MarkupWithoutSourceInfo(source);
		}

		[Fact]
		public void TripleColonTestSelfClosing()
		{
			var source = @"::: zone target=""chromeless""
::: form action=""create-resource"" submitText=""Create"" :::
::: zone-end
";

			var expected = @"<div class=""zone has-target"" data-target=""chromeless"">
<form class=""chromeless-form"" data-action=""create-resource"">
<div></div>
<button class=""button is-primary"" disabled=""disabled"" type=""submit"">Create</button>
</form>
</div>
".Replace("\r\n", "\n");

			var listener = TestLoggerListener.CreateLoggerListenerWithPhaseEqualFilter(LoggerPhase);

			Logger.RegisterListener(listener);
			using (new LoggerPhaseScope(LoggerPhase))
			{
				TestUtility.AssertEqual(expected, source, TestUtility.MarkupWithoutSourceInfo);
			}
			Logger.UnregisterListener(listener);

			// Listener should have no error or warning message.
			Assert.Empty(listener.Items);
		}
	}
}
