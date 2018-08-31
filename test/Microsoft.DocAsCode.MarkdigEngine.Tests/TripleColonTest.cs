// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Tests
{
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;
    using Xunit;

    public class TripleColonTest
    {

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
    }
}
