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
                "{\"name\":\"markdown_0\",\"children\":[{\"name\":\"Heading1_1\",\"children\":[{\"name\":\"InLineText_1_hello\"}]},{\"name\":\"Table_2\",\"children\":[{\"name\":\"Header_2\",\"children\":[{\"name\":\"headerItem_2\",\"children\":[{\"name\":\"InLineText_2_Name\"}]},{\"name\":\"headerItem_2\",\"children\":[{\"name\":\"InLineText_2_Type\"}]},{\"name\":\"headerItem_2\",\"children\":[{\"name\":\"InLineText_2_Notes\"}]},{\"name\":\"headerItem_2\",\"children\":[{\"name\":\"InLineText_2_Read/Write\"}]},{\"name\":\"headerItem_2\",\"children\":[{\"name\":\"InLineText_2_Description\"}]}]},{\"name\":\"Body_3\",\"children\":[{\"name\":\"Row_2\",\"children\":[{\"name\":\"RowItem_2\",\"children\":[{\"name\":\"InLineText_2_value\"}]},{\"name\":\"RowItem_2\",\"children\":[{\"name\":\"InLineText_2_Edm.String\"}]},{\"name\":\"RowItem_2\",\"children\":[]},{\"name\":\"RowItem_2\",\"children\":[]},{\"name\":\"RowItem_2\",\"children\":[]}]},{\"name\":\"Row_2\",\"children\":[{\"name\":\"RowItem_2\",\"children\":[{\"name\":\"InLineText_2_endDate\"}]},{\"name\":\"RowItem_2\",\"children\":[{\"name\":\"InLineText_2_Edm.DateTime\"}]},{\"name\":\"RowItem_2\",\"children\":[]},{\"name\":\"RowItem_2\",\"children\":[]},{\"name\":\"RowItem_2\",\"children\":[{\"name\":\"InLineText_2_The date and time at which the password expires.\"}]}]},{\"name\":\"Row_2\",\"children\":[{\"name\":\"RowItem_2\",\"children\":[{\"name\":\"InLineText_2_value\"}]},{\"name\":\"RowItem_2\",\"children\":[{\"name\":\"InLineText_2_Edm.String\"}]},{\"name\":\"RowItem_2\",\"children\":[]},{\"name\":\"RowItem_2\",\"children\":[]},{\"name\":\"RowItem_2\",\"children\":[]}]}]}]}]}";

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
                "{\"name\":\"markdown_0\",\"children\":[{\"name\":\"Html_1\",\"children\":[{\"name\":\"Raw(FromGfmHtmlComment)_1_&lt;!--\\nhttps://en.wikipedia.org/wiki/Draft:Microsoft_SQL_Server_Libraries/Drivers\\n--&gt;\\n\"}]}]}";
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
                "{\"name\":\"markdown_0\",\"children\":[{\"name\":\"Html_1\",\"children\":[{\"name\":\"Raw(FromHtml)_1_&lt;pre&gt;&lt;code&gt;//*************************************************\\n        // Test!\\n        //*************************************************&lt;/code&gt;&lt;/pre&gt;\\n\"}]}]}";
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
                "{\"name\":\"markdown_0\",\"children\":[{\"name\":\"Heading1_1\",\"children\":[{\"name\":\"InLineText_1_Test table\"}]},{\"name\":\"Table_2\",\"children\":[{\"name\":\"Header_2\",\"children\":[{\"name\":\"headerItem_2\",\"children\":[{\"name\":\"InLineText_2_header-1\"}]},{\"name\":\"headerItem_2\",\"children\":[{\"name\":\"InLineText_2_header-2\"}]},{\"name\":\"headerItem_2\",\"children\":[{\"name\":\"InLineText_2_header-3\"}]}]},{\"name\":\"Body_3\",\"children\":[{\"name\":\"Row_2\",\"children\":[{\"name\":\"RowItem_2\",\"children\":[{\"name\":\"Em_2\",\"children\":[{\"name\":\"InLineText_2_1-1\"}]}]},{\"name\":\"RowItem_2\",\"children\":[{\"name\":\"Link_2\",\"children\":[{\"name\":\"InLineText_2_User\"}]}]},{\"name\":\"RowItem_2\",\"children\":[{\"name\":\"InLineText_2_test\"}]}]}]}]},{\"name\":\"Ignore_6_[User]: ./entity-and-complex-type-reference.md#UserEntity\\n\"}]}";
            Assert.Equal(expected, JsonRenderer(source));
        }

        [Fact]
        [Trait("Related", "Markdown")]
        public void TestGfmImageLink_WithSpecialCharactorsInAltText()
        {
            var source = @"![This is image alt text with quotation ' and double quotation ""hello"" world](girl.png)";

            var expected =
                "{\"name\":\"markdown_0\",\"children\":[{\"name\":\"Paragraph_1\",\"children\":[{\"name\":\"Image_1_girl.png\"}]}]}";
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
                "{\"name\":\"markdown_0\",\"children\":[{\"name\":\"Heading1_1\",\"children\":[{\"name\":\"InLineText_1_Heading\"}]},{\"name\":\"Heading2_4\",\"children\":[{\"name\":\"InLineText_4_Sub-heading\"}]},{\"name\":\"Heading3_7\",\"children\":[{\"name\":\"InLineText_7_Another deeper heading\"}]},{\"name\":\"Paragraph_9\",\"children\":[{\"name\":\"InLineText_9_Paragraphs are separated\\nby a blank line.\"}]},{\"name\":\"Paragraph_12\",\"children\":[{\"name\":\"InLineText_12_Leave 2 spaces at the end of a line to do a\"},{\"name\":\"Br_12\"},{\"name\":\"InLineText_13_line break\"}]},{\"name\":\"Paragraph_15\",\"children\":[{\"name\":\"InLineText_15_Text attributes \"},{\"name\":\"Em_15\",\"children\":[{\"name\":\"InLineText_15_italic\"}]},{\"name\":\"InLineText_15_, \"},{\"name\":\"Strong_15\",\"children\":[{\"name\":\"InLineText_15_bold\"}]},{\"name\":\"InLineText_15_, \\n\"},{\"name\":\"InLineCode_16_monospace\"}]},{\"name\":\"Paragraph_18\",\"children\":[{\"name\":\"Del_18\",\"children\":[{\"name\":\"InLineText_18_strikethrough\"}]}]},{\"name\":\"Paragraph_20\",\"children\":[{\"name\":\"InLineText_20_A \"},{\"name\":\"Link_20\",\"children\":[{\"name\":\"InLineText_20_link\"}]},{\"name\":\"InLineText_20_.\"}]},{\"name\":\"Paragraph_22\",\"children\":[{\"name\":\"InLineText_22_Shopping list\"}]},{\"name\":\"ul_24\",\"children\":[{\"name\":\"li_24\",\"children\":[{\"name\":\"NonParagraph_24\",\"children\":[{\"name\":\"InLineText_24_apples\"}]}]},{\"name\":\"li_25\",\"children\":[{\"name\":\"NonParagraph_25\",\"children\":[{\"name\":\"InLineText_25_oranges\"}]}]},{\"name\":\"li_26\",\"children\":[{\"name\":\"NonParagraph_26\",\"children\":[{\"name\":\"InLineText_26_pears\"}]}]}]},{\"name\":\"Paragraph_28\",\"children\":[{\"name\":\"InLineText_28_Numbered list\"}]},{\"name\":\"ol_30\",\"children\":[{\"name\":\"li_30\",\"children\":[{\"name\":\"NonParagraph_30\",\"children\":[{\"name\":\"InLineText_30_apples\"}]}]},{\"name\":\"li_31\",\"children\":[{\"name\":\"NonParagraph_31\",\"children\":[{\"name\":\"InLineText_31_oranges\"}]}]},{\"name\":\"li_32\",\"children\":[{\"name\":\"NonParagraph_32\",\"children\":[{\"name\":\"InLineText_32_pears\"}]}]}]},{\"name\":\"Paragraph_34\",\"children\":[{\"name\":\"Link_34\",\"children\":[{\"name\":\"Raw(FromInline.AutoLink)_34_address@example.com\"}]}]}]}";
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
