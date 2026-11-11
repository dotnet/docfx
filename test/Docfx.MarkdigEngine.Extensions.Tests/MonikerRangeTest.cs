// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Docfx.MarkdigEngine.Tests;

public class MonikerRangeTest
{
    [Fact]
    public void MonikerRangeTestGeneral()
    {
        //arrange
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
<div class=""monikerRange"" range=""&gt;= myproduct-4.1"" data-moniker=""&gt;= myproduct-4.1"" sourceFile=""fake.md"" sourceStartLineNumber=""9"">
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
    public void MonikerRangeWithMultipleMonikers()
    {
        //arrange
        var source = @"::: moniker range=""moniker-1.0 || moniker-2.0  ||   moniker-3.0""
Content applies to moniker 1.0, 2.0 and 3.0.
::: moniker-end

::: moniker range=""moniker-4.0""
Content applies to moniker 4.0 only.
::: moniker-end
";

        // assert
        var expected = @"<div class=""monikerRange"" range=""moniker-1.0 || moniker-2.0  ||   moniker-3.0"" data-moniker=""moniker-1.0 moniker-2.0 moniker-3.0"">
<p>Content applies to moniker 1.0, 2.0 and 3.0.</p>
</div>
<div class=""monikerRange"" range=""moniker-4.0"" data-moniker=""moniker-4.0"">
<p>Content applies to moniker 4.0 only.</p>
</div>
";
        TestUtility.VerifyMarkup(source, expected);
    }

    [Fact]
    public void MonikerRangeTestInvalid()
    {
        //arrange
        var source = @"::: moniker range=""azure-rest-1.0";

        // assert
        var expected = @"<p>::: moniker range=&quot;azure-rest-1.0</p>
";
        TestUtility.VerifyMarkup(source, expected, ["invalid-moniker-range"]);
    }

    [Fact]
    public void MonikerRangeTestNotClosed()
    {
        //arrange
        var source1 = @"::: moniker range=""start""";
        var source2 = @"::: moniker range=""start""
::: moniker-end";

        // assert
        var expected = @"<div class=""monikerRange"" range=""start"" data-moniker=""start"">
</div>
";
        TestUtility.VerifyMarkup(source2, expected);
        TestUtility.VerifyMarkup(source1, expected, ["invalid-moniker-range"]);
    }

    [Fact]
    public void MonikerRangeWithCodeIndent()
    {
        var source = @"::: moniker range=""start""
    console.log(""hehe"")
::: moniker-end";
        var expected = @"<div class=""monikerRange"" range=""start"" data-moniker=""start"">
<pre><code>console.log(&quot;hehe&quot;)
</code></pre>
</div>
";
        TestUtility.VerifyMarkup(source, expected);
    }
}
