// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Tests
{
    using Xunit;

    public class MonikerRangeTest
    {
        static public string LoggerPhase = "MonikerRange";

        [Fact]
        public void MonikerRangeTestGeneral()
        {
            //arange
            var source = @"# Article 2

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

            // assert
            var expected = @"<h1 id=""article-2"" sourceFile=""fake.md"" sourceStartLineNumber=""1"">Article 2</h1>
<p sourceFile=""fake.md"" sourceStartLineNumber=""3"">Shared content.</p>
<h2 id=""section-1"" sourceFile=""fake.md"" sourceStartLineNumber=""5"">Section 1</h2>
<p sourceFile=""fake.md"" sourceStartLineNumber=""7"">Shared content.</p>
<div range=""&gt;= myproduct-4.1"" sourceFile=""fake.md"" sourceStartLineNumber=""9"">
<h2 id=""section-for-myproduct-41-and-later"" sourceFile=""fake.md"" sourceStartLineNumber=""10"">Section for myproduct-4.1 and Later</h2>
<p sourceFile=""fake.md"" sourceStartLineNumber=""12"">Some version-specific content here...</p>
<p sourceFile=""fake.md"" sourceStartLineNumber=""14"">::: nested moniker zone is not allowed. So this line is in plain text.
Inline ::: should not end moniker zone.</p>
</div>
<h2 id=""section-2"" sourceFile=""fake.md"" sourceStartLineNumber=""19"">Section 2</h2>
<p sourceFile=""fake.md"" sourceStartLineNumber=""21"">Shared content.</p>
";
            TestUtility.VerifyMarkup(source, expected, lineNumber: true, filePath: "fake.md");
        }


        [Fact]
        public void MonikerRangeTestInvalid()
        {
            //arange
            var source = @"::: moniker range=""azure-rest-1.0";

            // assert
            var expected = @"<p>::: moniker range=&quot;azure-rest-1.0</p>
";
            TestUtility.VerifyMarkup(source, expected, new[] { "invalid-moniker-range" });
        }

        [Fact]
        public void MonikerRangeTestNotClosed()
        {
            //arange
            var source1 = @"::: moniker range=""start""";
            var source2 = @"::: moniker range=""start""
::: moniker-end";

            // assert
            var expected = @"<div range=""start"">
</div>
";
            TestUtility.VerifyMarkup(source2, expected);
            TestUtility.VerifyMarkup(source1, expected, new[] { "invalid-moniker-range" });
        }

        [Fact]
        public void MonikerRangeWithCodeIndent()
        {
            var source = @"::: moniker range=""start""
    console.log(""hehe"")
::: moniker-end";
            var expected = @"<div range=""start"">
<pre><code>console.log(&quot;hehe&quot;)
</code></pre>
</div>
";
            TestUtility.VerifyMarkup(source, expected);
        }
    }
}
