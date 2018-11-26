// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownRewriters.Tests
{
    using Microsoft.DocAsCode.Dfm;
    using Microsoft.DocAsCode.MarkdigMarkdownRewriters;

    using Xunit;

    public class MarkdigMarkdownRewritersTests
    {
        private DfmEngine _engine;

        public MarkdigMarkdownRewritersTests()
        {
            var option = DocfxFlavoredMarked.CreateDefaultOptions();
            option.LegacyMode = true;
            var builder = new DfmEngineBuilder(option);
            _engine = builder.CreateDfmEngine(new MarkdigMarkdownRenderer());
        }

        [Fact]
        [Trait("Related", "MarkdigMarkdownRewriters")]
        public void TestMarkdigMarkdownRewriters_Resloved_ShortcutXref()
        {
            var source = "@System.String";
            var expected = "@\"System.String\"\n\n";

            var result = Rewrite(source, "topic.md");
            Assert.Equal(expected, result);
        }

        [Fact]
        [Trait("Related", "MarkdigMarkdownRewriters")]
        public void TestMarkdigMarkdownRewriters_Unresloved_ShortcutXref()
        {
            var source = "@outlook.com";
            var expected = "@outlook.com\n\n";

            var result = Rewrite(source, "topic.md");
            Assert.Equal(expected, result);
        }

        [Fact]
        [Trait("Related", "MarkdigMarkdownRewriters")]
        public void TestMarkdigMarkdownRewriters_AutoLinkXref()
        {
            var source = "<xref:system.string>";
            var expected = "<xref:system.string>\n\n";

            var result = Rewrite(source, "topic.md");
            Assert.Equal(expected, result);
        }

        [Fact]
        [Trait("Related", "MarkdigMarkdownRewriters")]
        public void TestMarkdigMarkdownRewriters_NormalizeVideo()
        {
            var source = @"> [!VIDEO https://channel9.msdn.com]
>
>
";
            var expected = @"> [!VIDEO https://channel9.msdn.com]

";

            var result = Rewrite(source, "topic.md");
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "MarkdigMarkdownRewriters")]
        public void TestMarkdigMarkdownRewriters_NormalizeMultipleVideo()
        {
            var source = @"> Video Sample
> [!VIDEO https://channel9.msdn.com]
>
>[!VIDEO https://channel9.msdn.com]
>
> *abc*
";
            var expected = @"> Video Sample
> 
> [!VIDEO https://channel9.msdn.com]
> 
> [!VIDEO https://channel9.msdn.com]
> 
> *abc*
> 
> 

";

            var result = Rewrite(source, "topic.md");
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "MarkdigMarkdownRewriters")]
        public void TestMarkdigMarkdownRewriters_MailTo()
        {
            var source = "<Mailto:docs@microsoft.com>";
            var expected = "<docs@microsoft.com>\n\n";

            var result = Rewrite(source, "topic.md");
            Assert.Equal(expected, result);

            result = Rewrite(result, "topic.md");
            Assert.Equal(expected, result);
        }

        [Fact]
        [Trait("Related", "MarkdigMarkdownRewriters")]
        public void TestMarkdigMarkdownRewriters_InlineLink()
        {
            var source = "[cool **text**](this%20is%20a%20link)";
            var expected = "[cool **text**](this%20is%20a%20link)\n\n";

            var result = Rewrite(source, "topic.md");
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "MarkdigMarkdownRewriters")]
        public void TestMarkdigMarkdownRewriters_InlineExternalLink()
        {
            var source = "[cool](https://dotnet.github.io/docfx)";
            var expected = "[cool](https://dotnet.github.io/docfx)\n\n";

            var result = Rewrite(source, "topic.md");
            Assert.Equal(expected, result);
        }

        [Fact]
        [Trait("Related", "MarkdigMarkdownRewriters")]
        public void TestMarkdigMarkdownRewriters_InlineRelativeLink()
        {
            var source = "[cool](~/a.txt)";
            var expected = "[cool](~/a.txt)\n\n";

            var result = Rewrite(source, "topic.md");
            Assert.Equal(expected, result);
        }

        [Fact]
        [Trait("Related", "MarkdigMarkdownRewriters")]
        public void TestMarkdigMarkdownRewriters_InlineEscapedLink()
        {
            var source = "[cool **text**](this%20is%20a%20link)";
            var expected = "[cool **text**](this%20is%20a%20link)\n\n";

            var result = Rewrite(source, "topic.md");
            Assert.Equal(expected, result);
        }

        [Fact]
        [Trait("Related", "MarkdigMarkdownRewriters")]
        public void TestMarkdigMarkdownRewriters_Html()
        {
            var source = @"
**markdown**

<div>
This is **markdown** content.
</div>

# header";
            var expected = @"**markdown**

<div>
This is <strong>markdown</strong> content.
</div>

# header";

            var result = Rewrite(source, "topic.md");
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }
        [Fact]
        [Trait("Related", "MarkdigMarkdownRewriters")]
        public void TestMarkdigMarkdownRewriters_HeadingWithWhitespace()
        {
            var source = @" #    Heading with whitespace    #

content...";
            var expected = @" #    Heading with whitespace    #

content...

";

            var result = Rewrite(source, "topic.md");
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "MarkdigMarkdownRewriters")]
        public void TestMarkdigMarkdownRewriters_HeadingWithoutWhitespace()
        {
            var source = @" #Heading with whitespace

content...";
            var expected = @" # Heading with whitespace

content...

";

            var result = Rewrite(source, "topic.md");
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "MarkdigMarkdownRewriters")]
        public void TestMarkdigMarkdownRewriters_LHeading()
        {
            var source = @"Heading with whitespace
===

content...";
            var expected = @"Heading with whitespace
===

content...

";

            var result = Rewrite(source, "topic.md");
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "MarkdigMarkdownRewriters")]
        public void TestMarkdigMarkdownRewriters_StrongAndEm()
        {
            var source = "__a__ and _b_ and **a** and **b** and *__ab__*";
            var expected = "__a__ and _b_ and **a** and **b** and *__ab__*\n\n";

            var result = Rewrite(source, "topic.md");
            Assert.Equal(expected, result);
        }

        [Fact]
        [Trait("Related", "MarkdigMarkdownRewriters")]
        public void TestMarkdigMarkdownRewriters_Table()
        {
            var source = @"|Tables|Are|Cool|
|---|:--:|--:|
|col 1 is|  left-aligned | $1600 |
| col 2 is |      centered |   $12 |
| col 3 is | right-aligned |$1|";
            var expected = @"|  Tables  |      Are      |  Cool |
|----------|:-------------:|------:|
| col 1 is | left-aligned  | $1600 |
| col 2 is |   centered    |   $12 |
| col 3 is | right-aligned |    $1 |

";

            var result = Rewrite(source, "topic.md");
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "MarkdigMarkdownRewriters")]
        public void TestMarkdigMarkdownRewriters_UnalignedTable00()
        {
            var source = @"|Tables|Are|Cool|
|---|:--:|--:|
|col 1 is|  left-aligned | $1600 |
| col 2 is |      centered 
| col 3 is";
            var expected = @"|  Tables  |     Are      |  Cool |
|----------|:------------:|------:|
| col 1 is | left-aligned | $1600 |
| col 2 is |   centered   |       |
| col 3 is |              |       |

";

            var result = Rewrite(source, "topic.md");
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "MarkdigMarkdownRewriters")]
        public void TestMarkdigMarkdownRewriters_UnalignedTable01()
        {
            var source = @"| | a |
|---|---|
| |a| ";
            var expected = @"|   | a |
|---|---|
|   | a |

";

            var result = Rewrite(source, "topic.md");
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "MarkdigMarkdownRewriters")]
        public void TestMarkdigMarkdownRewriters_InclusionPath()
        {
            var source = @"[!INCLUDE [title](.\..\..\..\includes\a.md)]";
            var expected = @"[!INCLUDE [title](./../../../includes/a.md)]

";
            var result = Rewrite(source, "topic.md");

            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        private string Rewrite(string source, string filePath)
        {
            return _engine.Markup(source, filePath);
        }
    }
}
