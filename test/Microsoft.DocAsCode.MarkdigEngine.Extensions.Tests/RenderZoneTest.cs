// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Tests
{
    using Xunit;

    public class RenderZoneTest
    {
        static public string LoggerPhase = "RenderZone";

        [Fact]
        public void KitchenSink()
        {
            //arange
            var content = @"# Article 2

Shared content.

## Section 1

Shared content.

::: zone target=""chromeless""
## Section for chromeless only

Some chromeless-specific content here...

::: nested moniker zone is not allowed. So this line is in plain text.
Inline ::: should not end moniker zone.

::: zone-end

## Section 2

Shared content.

:::  zone  pivot=""foo""
a pivot
:::    zone-end

:::  zone  pivot="" foo,bar ""    target=""docs""
a pivot with target 
:::    zone-end

::: zone target=""docs"" pivot=""csharp7-is-great""
hello
::: zone-end
";

            // assert
            var expected = @"<h1 id=""article-2"" sourceFile=""fake.md"" sourceStartLineNumber=""1"">Article 2</h1>
<p sourceFile=""fake.md"" sourceStartLineNumber=""3"">Shared content.</p>
<h2 id=""section-1"" sourceFile=""fake.md"" sourceStartLineNumber=""5"">Section 1</h2>
<p sourceFile=""fake.md"" sourceStartLineNumber=""7"">Shared content.</p>
<div class=""zone has-target"" data-target=""chromeless"" sourceFile=""fake.md"" sourceStartLineNumber=""9"">
<h2 id=""section-for-chromeless-only"" sourceFile=""fake.md"" sourceStartLineNumber=""10"">Section for chromeless only</h2>
<p sourceFile=""fake.md"" sourceStartLineNumber=""12"">Some chromeless-specific content here...</p>
<p sourceFile=""fake.md"" sourceStartLineNumber=""14"">::: nested moniker zone is not allowed. So this line is in plain text.
Inline ::: should not end moniker zone.</p>
</div>
<h2 id=""section-2"" sourceFile=""fake.md"" sourceStartLineNumber=""19"">Section 2</h2>
<p sourceFile=""fake.md"" sourceStartLineNumber=""21"">Shared content.</p>
<div class=""zone has-pivot"" data-pivot=""foo"" sourceFile=""fake.md"" sourceStartLineNumber=""23"">
<p sourceFile=""fake.md"" sourceStartLineNumber=""24"">a pivot</p>
</div>
<div class=""zone has-target has-pivot"" data-target=""docs"" data-pivot=""foo bar"" sourceFile=""fake.md"" sourceStartLineNumber=""27"">
<p sourceFile=""fake.md"" sourceStartLineNumber=""28"">a pivot with target</p>
</div>
<div class=""zone has-target has-pivot"" data-target=""docs"" data-pivot=""csharp7-is-great"" sourceFile=""fake.md"" sourceStartLineNumber=""31"">
<p sourceFile=""fake.md"" sourceStartLineNumber=""32"">hello</p>
</div>
";
            TestUtility.VerifyMarkup(content, expected, lineNumber: true, filePath: "fake.md");
        }

        [Fact]
        public void AttributeMissingClosingQuote()
        {
            //arange
            var source = @"::: zone target=""chromeless";

            // assert
            var expected = @"<p>::: zone target=&quot;chromeless</p>
";
            TestUtility.VerifyMarkup(source, expected, new[] { "invalid-zone" });
        }

        [Fact]
        public void MissingEndTag()
        {
            //arange
            var source1 = @"::: zone target=""chromeless""";
            var source2 = @"::: zone target=""chromeless""
::: zone-end";

            // assert
            var expected = @"<div class=""zone has-target"" data-target=""chromeless"">
</div>
";

            TestUtility.VerifyMarkup(source2, expected);
            TestUtility.VerifyMarkup(source1, expected, new[] { "invalid-zone" });
        }

        [Fact]
        public void NotNested()
        {
            //arange
            var content = @"::: zone target=""chromeless""
::: zone target=""pdf""
::: zone-end
::: zone-end
";

            TestUtility.VerifyMarkup(content, null, new[] { "invalid-zone" });
        }

        [Fact]
        public void PermitsNestedBlocks()
        {
            var source = @"::: zone target=""chromeless""
* foo
* bar
* baz
::: zone-end
";

            var expected = @"<div class=""zone has-target"" data-target=""chromeless"">
<ul>
<li>foo</li>
<li>bar</li>
<li>baz</li>
</ul>
</div>
";
            TestUtility.VerifyMarkup(source, expected);
        }

        [Fact]
        public void PdfPivotInvalid()
        {
            //arange
            var source = @"::: zone target = ""pdf""  pivot = ""foo""  ";

            // assert
            var expected = @"<p>::: zone target = &quot;pdf&quot;  pivot = &quot;foo&quot;</p>
";
            TestUtility.VerifyMarkup(source, expected, new[] { "invalid-zone" });
        }

        [Fact]
        public void PivotInvalid()
        {
            //arange
            var source = @"::: zone pivot = ""**""
::: zone-end";

            // assert
            var expected = "<p>::: zone pivot = &quot;**&quot;\n::: zone-end</p>\n";

            TestUtility.VerifyMarkup(source, expected, new[] { "invalid-zone" });
        }

        [Fact]
        public void PivotInvalid2()
        {
            //arange
            var source = @"::: zone pivot = ""a b""
::: zone-end";

            // assert
            var expected = "<p>::: zone pivot = &quot;a b&quot;\n::: zone-end</p>\n";

            TestUtility.VerifyMarkup(source, expected, new[] { "invalid-zone" });
        }

        [Fact]
        public void PivotCommaDelimited()
        {
            //arange
            var source = @"::: zone pivot = ""a,b""
::: zone-end";

            // assert
            var expected = "<div class=\"zone has-pivot\" data-pivot=\"a b\">\n</div>\n";

            TestUtility.VerifyMarkup(source, expected);
        }


        [Fact]
        public void UnexpectedAttribute()
        {
            //arange
            var source = @"::: zone target=""pdf"" something";

            // assert
            var expected = @"<p>::: zone target=&quot;pdf&quot; something</p>
";
            TestUtility.VerifyMarkup(source, expected, new[] { "invalid-zone" });
        }

        [Fact]
        public void DuplicateAttribute()
        {
            //arange
            var source = @"::: zone target=""pdf"" target=""docs""";

            // assert
            var expected = @"<p>::: zone target=&quot;pdf&quot; target=&quot;docs&quot;</p>
";
            TestUtility.VerifyMarkup(source, expected, new[] { "invalid-zone" });
        }

        [Fact]
        public void InvalidAttribute()
        {
            //arange
            var source = @"::: zone *=""pdf""";

            // assert
            var expected = @"<p>::: zone *=&quot;pdf&quot;</p>
";
            TestUtility.VerifyMarkup(source, expected, new[] { "invalid-zone" });
        }

        [Fact]
        public void TextAfterEndTag()
        {
            //arange
            var source = @":::zone
:::zone-end asdjklf";

            // assert
            var expected = "<p>:::zone\n:::zone-end asdjklf</p>\n";

            TestUtility.VerifyMarkup(source, expected, new[] { "invalid-zone" });
        }
    }
}
