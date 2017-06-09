// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite.Tests
{
    using Microsoft.DocAsCode.Build.Engine;
    using Microsoft.DocAsCode.Plugins;

    using Xunit;

    public class JsonTokenTreeRendererTest
    {
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
                "{\"name\":\"0>0>markdown\",\"children\":[{\"name\":\"1>1>Heading1\",\"children\":[{\"name\":\"1>1>Text>hello\"}]},{\"name\":\"2>6>Table\",\"children\":[{\"name\":\"2>2>Header\",\"children\":[{\"name\":\"2>2>headerItem\",\"children\":[{\"name\":\"2>2>Text>Name\"}]},{\"name\":\"2>2>headerItem\",\"children\":[{\"name\":\"2>2>Text>Type\"}]},{\"name\":\"2>2>headerItem\",\"children\":[{\"name\":\"2>2>Text>Notes\"}]},{\"name\":\"2>2>headerItem\",\"children\":[{\"name\":\"2>2>Text>Read/Write\"}]},{\"name\":\"2>2>headerItem\",\"children\":[{\"name\":\"2>2>Text>Description\"}]}]},{\"name\":\"4>6>Body\",\"children\":[{\"name\":\"4>4>Row\",\"children\":[{\"name\":\"4>4>RowItem\",\"children\":[{\"name\":\"4>4>Text>value\"}]},{\"name\":\"4>4>RowItem\",\"children\":[{\"name\":\"4>4>Text>Edm.String\"}]},{\"name\":\"4>4>RowItem\",\"children\":[]},{\"name\":\"4>4>RowItem\",\"children\":[]},{\"name\":\"4>4>RowItem\",\"children\":[]}]},{\"name\":\"5>5>Row\",\"children\":[{\"name\":\"5>5>RowItem\",\"children\":[{\"name\":\"5>5>Text>endDate\"}]},{\"name\":\"5>5>RowItem\",\"children\":[{\"name\":\"5>5>Text>Edm.DateTime\"}]},{\"name\":\"5>5>RowItem\",\"children\":[]},{\"name\":\"5>5>RowItem\",\"children\":[]},{\"name\":\"5>5>RowItem\",\"children\":[{\"name\":\"5>5>Text>The date and time at which the password expires.\"}]}]},{\"name\":\"6>6>Row\",\"children\":[{\"name\":\"6>6>RowItem\",\"children\":[{\"name\":\"6>6>Text>value\"}]},{\"name\":\"6>6>RowItem\",\"children\":[{\"name\":\"6>6>Text>Edm.String\"}]},{\"name\":\"6>6>RowItem\",\"children\":[]},{\"name\":\"6>6>RowItem\",\"children\":[]},{\"name\":\"6>6>RowItem\",\"children\":[]}]}]}]}]}";

            Assert.Equal(expected, JsonRenderer(source));
        }

        [Fact]
        [Trait("Related", "Markdown")]
        public void TestGfmBuilder_CommentRuleShouldBeforeAutoLink()
        {
            var source = @"<!--
https://en.wikipedia.org/wiki/Draft:Microsoft_SQL_Server_Libraries/Drivers
-->
";
            var expected =
                "{\"name\":\"0>0>markdown\",\"children\":[{\"name\":\"1>3>Html\",\"children\":[{\"name\":\"1>3>Raw(FromGfmHtmlComment)>&lt;!--\\nhttps://en.wikipedia.org/wiki/Draft:Microsoft_SQL_Server_Libraries/Drivers\\n--&gt;\\n\"}]}]}";
            Assert.Equal(expected, JsonRenderer(source));
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
                "{\"name\":\"0>0>markdown\",\"children\":[{\"name\":\"1>3>Raw(FromBlock.Html.PreElement)>&lt;pre&gt;&lt;code&gt;//*************************************************\\n        // Test!\\n        //*************************************************&lt;/code&gt;&lt;/pre&gt;\\n\"}]}";
            Assert.Equal(expected, JsonRenderer(source));
        }

        [Fact]
        [Trait("Related", "Markdown")]
        public void TestTable_WithRefLink()
        {
            var source = @"# Test table
| header-1 | header-2 | header-3 |
|:-------- |:--------:| --------:|
| *1-1* | [User] | test |

[User]: ./entity-and-complex-type-reference.md#UserEntity
";

            var expected =
                "{\"name\":\"0>0>markdown\",\"children\":[{\"name\":\"1>1>Heading1\",\"children\":[{\"name\":\"1>1>Text>Test table\"}]},{\"name\":\"2>4>Table\",\"children\":[{\"name\":\"2>2>Header\",\"children\":[{\"name\":\"2>2>headerItem\",\"children\":[{\"name\":\"2>2>Text>header-1\"}]},{\"name\":\"2>2>headerItem\",\"children\":[{\"name\":\"2>2>Text>header-2\"}]},{\"name\":\"2>2>headerItem\",\"children\":[{\"name\":\"2>2>Text>header-3\"}]}]},{\"name\":\"4>4>Body\",\"children\":[{\"name\":\"4>4>Row\",\"children\":[{\"name\":\"4>4>RowItem\",\"children\":[{\"name\":\"4>4>Em\",\"children\":[{\"name\":\"4>4>Text>1-1\"}]}]},{\"name\":\"4>4>RowItem\",\"children\":[{\"name\":\"4>4>Link\",\"children\":[{\"name\":\"4>4>Text>User\"}]}]},{\"name\":\"4>4>RowItem\",\"children\":[{\"name\":\"4>4>Text>test\"}]}]}]}]},{\"name\":\"6>6>Ignore>[User]: ./entity-and-complex-type-reference.md#UserEntity\\n\"}]}";
            Assert.Equal(expected, JsonRenderer(source));
        }

        [Fact]
        [Trait("Related", "Markdown")]
        public void TestGfmImageLink_WithSpecialCharactorsInAltText()
        {
            var source = @"![This is image alt text with quotation ' and double quotation ""hello"" world](girl.png)";

            var expected =
                "{\"name\":\"0>0>markdown\",\"children\":[{\"name\":\"1>1>Paragraph\",\"children\":[{\"name\":\"1>1>Image>girl.png\"}]}]}";
            Assert.Equal(expected, JsonRenderer(source));
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
                "{\"name\":\"0>0>markdown\",\"children\":[{\"name\":\"1>2>Heading1\",\"children\":[{\"name\":\"1>1>Text>Heading\"}]},{\"name\":\"4>5>Heading2\",\"children\":[{\"name\":\"4>4>Text>Sub-heading\"}]},{\"name\":\"7>7>Heading3\",\"children\":[{\"name\":\"7>7>Text>Another deeper heading\"}]},{\"name\":\"9>10>Paragraph\",\"children\":[{\"name\":\"9>10>Text>Paragraphs are separated\\nby a blank line.\"}]},{\"name\":\"12>13>Paragraph\",\"children\":[{\"name\":\"12>12>Text>Leave 2 spaces at the end of a line to do a\"},{\"name\":\"12>12>Br\"},{\"name\":\"13>13>Text>line break\"}]},{\"name\":\"15>16>Paragraph\",\"children\":[{\"name\":\"15>15>Text>Text attributes \"},{\"name\":\"15>15>Em\",\"children\":[{\"name\":\"15>15>Text>italic\"}]},{\"name\":\"15>15>Text>, \"},{\"name\":\"15>15>Strong\",\"children\":[{\"name\":\"15>15>Text>bold\"}]},{\"name\":\"15>15>Text>, \\n\"},{\"name\":\"16>16>Code>monospace\"}]},{\"name\":\"18>18>Paragraph\",\"children\":[{\"name\":\"18>18>Del\",\"children\":[{\"name\":\"18>18>Text>strikethrough\"}]}]},{\"name\":\"20>20>Paragraph\",\"children\":[{\"name\":\"20>20>Text>A \"},{\"name\":\"20>20>Link\",\"children\":[{\"name\":\"20>20>Text>link\"}]},{\"name\":\"20>20>Text>.\"}]},{\"name\":\"22>22>Paragraph\",\"children\":[{\"name\":\"22>22>Text>Shopping list\"}]},{\"name\":\"24>26>ul\",\"children\":[{\"name\":\"24>24>ListItem\",\"children\":[{\"name\":\"24>24>NonParagraph\",\"children\":[{\"name\":\"24>24>Text>apples\"}]}]},{\"name\":\"25>25>ListItem\",\"children\":[{\"name\":\"25>25>NonParagraph\",\"children\":[{\"name\":\"25>25>Text>oranges\"}]}]},{\"name\":\"26>26>ListItem\",\"children\":[{\"name\":\"26>26>NonParagraph\",\"children\":[{\"name\":\"26>26>Text>pears\"}]}]}]},{\"name\":\"28>28>Paragraph\",\"children\":[{\"name\":\"28>28>Text>Numbered list\"}]},{\"name\":\"30>32>ol\",\"children\":[{\"name\":\"30>30>ListItem\",\"children\":[{\"name\":\"30>30>NonParagraph\",\"children\":[{\"name\":\"30>30>Text>apples\"}]}]},{\"name\":\"31>31>ListItem\",\"children\":[{\"name\":\"31>31>NonParagraph\",\"children\":[{\"name\":\"31>31>Text>oranges\"}]}]},{\"name\":\"32>32>ListItem\",\"children\":[{\"name\":\"32>32>NonParagraph\",\"children\":[{\"name\":\"32>32>Text>pears\"}]}]}]},{\"name\":\"34>34>Paragraph\",\"children\":[{\"name\":\"34>34>Link\",\"children\":[{\"name\":\"34>34>Raw(FromInline.AutoLink)>address@example.com\"}]}]}]}";
            Assert.Equal(expected, JsonRenderer(source));
        }

        private string JsonRenderer(string content)
        {
            JsonTokenTreeServiceProvider jsonServiceProvider = new JsonTokenTreeServiceProvider();
            IMarkdownService jsonService = jsonServiceProvider.CreateMarkdownService(new MarkdownServiceParameters());
            return jsonService.Markup(content, null).Html;
        }
    }
}
