// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite.Tests
{
    using Microsoft.DocAsCode.Build.Engine;
    using Microsoft.DocAsCode.Plugins;

    using Xunit;

    public class JsonRendererTest
    {
        public string JsonRenderer(string content)
        {
            JsonServiceProvider jsonServiceProvider = new JsonServiceProvider();
            IMarkdownService jsonService = jsonServiceProvider.CreateMarkdownService(new MarkdownServiceParameters());
            return jsonService.Markup(content, null).Html;
        }

        [Fact]
        [Trait("Related", "Markdown")]
        public void TestTable_WithEmptyCell()
        {
            var source = @"# hello
|  Name |  Type |  Notes |  Read/Write |  Description |
|:-------|:-------|:-------|:-------|:-------|
| value | Edm.String |  |  |
| endDate | Edm.DateTime |  |  | The date and time at which the password expires. |
| value | Edm.String |  |  |  |
";
            var expected =
                "{\"name\":\"markdown\",\"children\":[{\"name\":\"Heading1\",\"children\":[{\"name\":\"InLineText(hello)\"}]},{\"name\":\"Table\",\"children\":[{\"name\":\"Header\",\"children\":[{\"name\":\"headerItem\",\"children\":[{\"name\":\"InLineText(Name)\"}]},{\"name\":\"headerItem\",\"children\":[{\"name\":\"InLineText(Type)\"}]},{\"name\":\"headerItem\",\"children\":[{\"name\":\"InLineText(Notes)\"}]},{\"name\":\"headerItem\",\"children\":[{\"name\":\"InLineText(Read/Write)\"}]},{\"name\":\"headerItem\",\"children\":[{\"name\":\"InLineText(Description)\"}]}]},{\"name\":\"Body\",\"children\":[{\"name\":\"Row\",\"children\":[{\"name\":\"RowItem\",\"children\":[{\"name\":\"InLineText(value)\"}]},{\"name\":\"RowItem\",\"children\":[{\"name\":\"InLineText(Edm.String)\"}]},{\"name\":\"RowItem\",\"children\":[]},{\"name\":\"RowItem\",\"children\":[]},{\"name\":\"RowItem\",\"children\":[]}]},{\"name\":\"Row\",\"children\":[{\"name\":\"RowItem\",\"children\":[{\"name\":\"InLineText(endDate)\"}]},{\"name\":\"RowItem\",\"children\":[{\"name\":\"InLineText(Edm.DateTime)\"}]},{\"name\":\"RowItem\",\"children\":[]},{\"name\":\"RowItem\",\"children\":[]},{\"name\":\"RowItem\",\"children\":[{\"name\":\"InLineText(The date and time at which the password expires.)\"}]}]},{\"name\":\"Row\",\"children\":[{\"name\":\"RowItem\",\"children\":[{\"name\":\"InLineText(value)\"}]},{\"name\":\"RowItem\",\"children\":[{\"name\":\"InLineText(Edm.String)\"}]},{\"name\":\"RowItem\",\"children\":[]},{\"name\":\"RowItem\",\"children\":[]},{\"name\":\"RowItem\",\"children\":[]}]}]}]}]}";

            Assert.Equal(expected, JsonRenderer(source));
        }

        [Fact]
        [Trait("Related", "Markdown")]
        public void TestGfmBuilder_CommentRuleShouldBeforeAutoLink()
        {
            var source = @"<!--
https://en.wikipedia.org/wiki/Draft:Microsoft_SQL_Server_Libraries/Drivers
-->";
            var expected =
                "{\"name\":\"markdown\",\"children\":[{\"name\":\"Html\",\"children\":[{\"name\":\"Raw(FromGfmHtmlComment)\"}]}]}";
            Assert.Equal(expected.Replace("\r\n", "\n"), JsonRenderer(source));
        }

        [Fact]
        [Trait("Related", "Markdown")]
        public void TestGfmBuilder_CodeTag()
        {
            var source = @"<pre><code>//*************************************************
        // Test!
        //*************************************************</code></pre>
";
            var expected =
                "{\"name\":\"markdown\",\"children\":[{\"name\":\"Html\",\"children\":[{\"name\":\"Raw(FromHtml)\"}]}]}";
            Assert.Equal(expected.Replace("\r\n", "\n"), JsonRenderer(source));
        }

        [Fact]
        [Trait("Related", "Markdown")]
        public void TestTable_WithRefLink()
        {
            var source = @"# Test table
| header-1 | header-2 | header-3 |
|:-------- |:--------:| --------:|
| *1-1* | [User] | test |

[User]: ./entity-and-complex-type-reference.md#UserEntity";

            var expected =
                "{\"name\":\"markdown\",\"children\":[{\"name\":\"Heading1\",\"children\":[{\"name\":\"InLineText(Test table)\"}]},{\"name\":\"Table\",\"children\":[{\"name\":\"Header\",\"children\":[{\"name\":\"headerItem\",\"children\":[{\"name\":\"InLineText(header-1)\"}]},{\"name\":\"headerItem\",\"children\":[{\"name\":\"InLineText(header-2)\"}]},{\"name\":\"headerItem\",\"children\":[{\"name\":\"InLineText(header-3)\"}]}]},{\"name\":\"Body\",\"children\":[{\"name\":\"Row\",\"children\":[{\"name\":\"RowItem\",\"children\":[{\"name\":\"Em\",\"children\":[{\"name\":\"InLineText(1-1)\"}]}]},{\"name\":\"RowItem\",\"children\":[{\"name\":\"Link\",\"children\":[{\"name\":\"InLineText(User)\"}]}]},{\"name\":\"RowItem\",\"children\":[{\"name\":\"InLineText(test)\"}]}]}]}]},{\"name\":\"Ignore\"}]}";
            Assert.Equal(expected.Replace("\r\n", "\n"), JsonRenderer(source));
        }

        [Fact]
        [Trait("Related", "Markdown")]
        public void TestGfmImageLink_WithSpecialCharactorsInAltText()
        {
            var source = @"![This is image alt text with quotation ' and double quotation ""hello"" world](girl.png)";

            var expected =
                "{\"name\":\"markdown\",\"children\":[{\"name\":\"Paragraph\",\"children\":[{\"name\":\"Image\"}]}]}";
            Assert.Equal(expected.Replace("\r\n", "\n"), JsonRenderer(source));
        }

        [Fact]
        [Trait("Related", "Markdown")]
        public void TestPref()
        {
            var source = @"Heading
=======
 
Sub-heading
-----------
  
### Another deeper heading
  
Paragraphs are separated
by a blank line.
 
Leave 2 spaces at the end of a line to do a  
line break
 
Text attributes *italic*, **bold**, 
`monospace`

~~strikethrough~~
 
A [link](http://example.com).

Shopping list
 
* apples
* oranges
* pears
 
Numbered list
 
1. apples
2. oranges
3. pears

<address@example.com>
";
            var expected =
                "{\"name\":\"markdown\",\"children\":[{\"name\":\"Heading1\",\"children\":[{\"name\":\"InLineText(Heading)\"}]},{\"name\":\"Heading2\",\"children\":[{\"name\":\"InLineText(Sub-heading)\"}]},{\"name\":\"Heading3\",\"children\":[{\"name\":\"InLineText(Another deeper heading)\"}]},{\"name\":\"Paragraph\",\"children\":[{\"name\":\"InLineText(Paragraphs are separated\\nby a blank line.)\"}]},{\"name\":\"Paragraph\",\"children\":[{\"name\":\"InLineText(Leave 2 spaces at the end of a line to do a)\"},{\"name\":\"Br\"},{\"name\":\"InLineText(line break)\"}]},{\"name\":\"Paragraph\",\"children\":[{\"name\":\"InLineText(Text attributes )\"},{\"name\":\"Em\",\"children\":[{\"name\":\"InLineText(italic)\"}]},{\"name\":\"InLineText(, )\"},{\"name\":\"Strong\",\"children\":[{\"name\":\"InLineText(bold)\"}]},{\"name\":\"InLineText(, \\n)\"},{\"name\":\"InLineCode\"}]},{\"name\":\"Paragraph\",\"children\":[{\"name\":\"Del\",\"children\":[{\"name\":\"InLineText(strikethrough)\"}]}]},{\"name\":\"Paragraph\",\"children\":[{\"name\":\"InLineText(A )\"},{\"name\":\"Link\",\"children\":[{\"name\":\"InLineText(link)\"}]},{\"name\":\"InLineText(.)\"}]},{\"name\":\"Paragraph\",\"children\":[{\"name\":\"InLineText(Shopping list)\"}]},{\"name\":\"ul\",\"children\":[{\"name\":\"li\",\"children\":[{\"name\":\"NonParagraph\",\"children\":[{\"name\":\"InLineText(apples)\"}]}]},{\"name\":\"li\",\"children\":[{\"name\":\"NonParagraph\",\"children\":[{\"name\":\"InLineText(oranges)\"}]}]},{\"name\":\"li\",\"children\":[{\"name\":\"NonParagraph\",\"children\":[{\"name\":\"InLineText(pears)\"}]}]}]},{\"name\":\"Paragraph\",\"children\":[{\"name\":\"InLineText(Numbered list)\"}]},{\"name\":\"ol\",\"children\":[{\"name\":\"li\",\"children\":[{\"name\":\"NonParagraph\",\"children\":[{\"name\":\"InLineText(apples)\"}]}]},{\"name\":\"li\",\"children\":[{\"name\":\"NonParagraph\",\"children\":[{\"name\":\"InLineText(oranges)\"}]}]},{\"name\":\"li\",\"children\":[{\"name\":\"NonParagraph\",\"children\":[{\"name\":\"InLineText(pears)\"}]}]}]},{\"name\":\"Paragraph\",\"children\":[{\"name\":\"Link\",\"children\":[{\"name\":\"Raw(FromInLineAutoLink)\"}]}]}]}";
            Assert.Equal(expected.Replace("\r\n", "\n"), JsonRenderer(source));
        }
    }
}
