// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Tests
{
    using Microsoft.DocAsCode.Plugins;
    using Xunit;

    public class MonikerRangeTest
    {
        [Fact]
        public void XrefTestGeneral()
        {
            //arange
            var content = @"# Article 2

Shared content.

## Section 1

Shared content.

::: moniker range="">= myproduct-4.1""
## Section for myproduct-4.1 and Later

Some version-specific content here...

::: nested moniker zone is not allowed. So this line is in plain text.
Inline ::: should not end moniker zone.

::: moniker-end

## Section 2

Shared content.
";

            var marked = TestUtility.Markup(content, "fake.md");

            // assert
            var expected = @"<h1 id=""article-2"" sourceFile=""fake.md"" sourceStartLineNumber=""1"" sourceEndLineNumber=""1"">Article 2</h1>
<p sourceFile=""fake.md"" sourceStartLineNumber=""3"" sourceEndLineNumber=""3"">Shared content.</p>
<h2 id=""section-1"" sourceFile=""fake.md"" sourceStartLineNumber=""5"" sourceEndLineNumber=""5"">Section 1</h2>
<p sourceFile=""fake.md"" sourceStartLineNumber=""7"" sourceEndLineNumber=""7"">Shared content.</p>
<div sourceFile=""fake.md"" sourceStartLineNumber=""9"" sourceEndLineNumber=""17"" range="">= myproduct-4.1"">
<h2 id=""section-for-myproduct-41-and-later"" sourceFile=""fake.md"" sourceStartLineNumber=""10"" sourceEndLineNumber=""10"">Section for myproduct-4.1 and Later</h2>
<p sourceFile=""fake.md"" sourceStartLineNumber=""12"" sourceEndLineNumber=""12"">Some version-specific content here...</p>
<p sourceFile=""fake.md"" sourceStartLineNumber=""14"" sourceEndLineNumber=""15"">::: nested moniker zone is not allowed. So this line is in plain text.
Inline ::: should not end moniker zone.</p>
</div>
<h2 id=""section-2"" sourceFile=""fake.md"" sourceStartLineNumber=""19"" sourceEndLineNumber=""19"">Section 2</h2>
<p sourceFile=""fake.md"" sourceStartLineNumber=""21"" sourceEndLineNumber=""21"">Shared content.</p>
".Replace("\r\n", "\n");
            Assert.Equal(expected.Replace("\r\n", "\n"), marked.Html);
        }
    }
}
