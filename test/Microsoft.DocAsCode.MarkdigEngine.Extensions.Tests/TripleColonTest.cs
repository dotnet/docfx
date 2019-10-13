// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Tests
{
    using Markdig.Syntax;
    using Microsoft.DocAsCode.MarkdigEngine.Extensions;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
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
";
            TestUtility.VerifyMarkup(source, expected);
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
";
            TestUtility.VerifyMarkup(source, expected);
        }

        [Fact]
        public void TripleColonTestBlockClosed()
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
";
            TestUtility.VerifyMarkup(source, expected);
        }

        [Fact]
        public void TripleColonWithInMonikerTestBlockClosed()
        {
            var source = new StringBuilder()
                .AppendLine("::: moniker range=\"chromeless\"")
                .AppendLine("::: zone target=\"docs\"")
                .AppendLine("## Alt text")
                .AppendLine("::: zone-end")
                .AppendLine("::: moniker-end")
                .ToString();

            var expected = @"<div range=""chromeless"">
<div class=""zone has-target"" data-target=""docs"">
<h2 id=""alt-text"">Alt text</h2>
</div>
</div>";

            TestUtility.VerifyMarkup(source, expected);
        }

        [Fact]
        public void TripleColonWithInMonikerTestBlockUnClosed()
        {
            var source = new StringBuilder()
                .AppendLine("::: moniker range=\"chromeless\"")
                .AppendLine("::: zone target=\"docs\"")
                .AppendLine("## Alt text")
                .AppendLine("::: moniker-end")
                .ToString();

            var expected = @"<div range=""chromeless"">
<div class=""zone has-target"" data-target=""docs"">
<h2 id=""alt-text"">Alt text</h2>
</div>
</div>
";

            TestUtility.VerifyMarkup(source, expected);
        }
    }
}
