// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Tests
{
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;
    using Xunit;

    public class RenderZoneTest
    {
        static public string LoggerPhase = "MonikerRange";

        [Fact]
        public void RenderZoneTestGeneral()
        {
            //arange
            var content = @"# Article 2

Shared content.

## Section 1

Shared content.

::: zone render=""chromeless""
## Section for chromeless only

Some chromeless-specific content here...

::: nested moniker zone is not allowed. So this line is in plain text.
Inline ::: should not end moniker zone.

::: zone-end

## Section 2

Shared content.
";

            var marked = TestUtility.Markup(content, "fake.md");

            // assert
            var expected = @"<h1 id=""article-2"" sourceFile=""fake.md"" sourceStartLineNumber=""1"">Article 2</h1>
<p sourceFile=""fake.md"" sourceStartLineNumber=""3"">Shared content.</p>
<h2 id=""section-1"" sourceFile=""fake.md"" sourceStartLineNumber=""5"">Section 1</h2>
<p sourceFile=""fake.md"" sourceStartLineNumber=""7"">Shared content.</p>
<div sourceFile=""fake.md"" sourceStartLineNumber=""9"" zone=""chromeless"">
<h2 id=""section-for-chromeless-only"" sourceFile=""fake.md"" sourceStartLineNumber=""10"">Section for chromeless only</h2>
<p sourceFile=""fake.md"" sourceStartLineNumber=""12"">Some chromeless-specific content here...</p>
<p sourceFile=""fake.md"" sourceStartLineNumber=""14"">::: nested moniker zone is not allowed. So this line is in plain text.
Inline ::: should not end moniker zone.</p>
</div>
<h2 id=""section-2"" sourceFile=""fake.md"" sourceStartLineNumber=""19"">Section 2</h2>
<p sourceFile=""fake.md"" sourceStartLineNumber=""21"">Shared content.</p>
".Replace("\r\n", "\n");
            Assert.Equal(expected.Replace("\r\n", "\n"), marked.Html);
        }
    }
}
