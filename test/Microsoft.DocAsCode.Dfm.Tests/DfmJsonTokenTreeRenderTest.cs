// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm.Tests
{
    using Microsoft.DocAsCode.Build.Engine;
    using Microsoft.DocAsCode.Plugins;

    using Xunit;

    public class DfmJsonTokenTreeRenderTest
    {
        [Theory]
        [Trait("Related", "DfmMarkdown")]
        [InlineData("", "{\"name\":\"0>0>markdown\",\"children\":[]}")]
        [InlineData("<address@example.com>",
             "{\"name\":\"0>0>markdown\",\"children\":[{\"name\":\"1>1>Paragraph\",\"children\":[{\"name\":\"1>1>Link\",\"children\":[{\"name\":\"1>1>Raw(FromInline.AutoLink)>address@example.com\"}]}]}]}"
         )]
        [InlineData(" https://github.com/dotnet/docfx/releases ",
             "{\"name\":\"0>0>markdown\",\"children\":[{\"name\":\"1>1>Paragraph\",\"children\":[{\"name\":\"1>1>Text> \"},{\"name\":\"1>1>Link\",\"children\":[{\"name\":\"1>1>Raw(FromInline.Gfm.Url)>https://github.com/dotnet/docfx/releases\"}]},{\"name\":\"1>1>Text> \"}]}]}"
         )]
        [InlineData("<Insert OneGet Details - meeting on 10/30 for details.>",
             "{\"name\":\"0>0>markdown\",\"children\":[{\"name\":\"1>1>Html\",\"children\":[{\"name\":\"1>1>Text>&lt;Insert OneGet Details - meeting on 10/30 for details.&gt;\"}]}]}")
        ]
        [InlineData("<http://example.com/>",
             "{\"name\":\"0>0>markdown\",\"children\":[{\"name\":\"1>1>Paragraph\",\"children\":[{\"name\":\"1>1>Link\",\"children\":[{\"name\":\"1>1>Raw(FromInline.AutoLink)>http://example.com/\"}]}]}]}"
         )]
        [InlineData("# Hello World",
             "{\"name\":\"0>0>markdown\",\"children\":[{\"name\":\"1>1>Heading1\",\"children\":[{\"name\":\"1>1>Text>Hello World\"}]}]}"
         )]
        [InlineData("Hot keys: <kbd>Ctrl+[</kbd> and <kbd>Ctrl+]</kbd>",
             "{\"name\":\"0>0>markdown\",\"children\":[{\"name\":\"1>1>Paragraph\",\"children\":[{\"name\":\"1>1>Text>Hot keys\"},{\"name\":\"1>1>Text>: \"},{\"name\":\"1>1>Tag\"},{\"name\":\"1>1>Text>Ctrl+\"},{\"name\":\"1>1>Text>[\"},{\"name\":\"1>1>Tag\"},{\"name\":\"1>1>Text> and \"},{\"name\":\"1>1>Tag\"},{\"name\":\"1>1>Text>Ctrl+]\"},{\"name\":\"1>1>Tag\"}]}]}"
         )]
        [InlineData("<div>Some text here</div>",
             "{\"name\":\"0>0>markdown\",\"children\":[{\"name\":\"1>1>Html\",\"children\":[{\"name\":\"1>1>Tag\"},{\"name\":\"1>1>Text>Some text here\"},{\"name\":\"1>1>Tag\"}]}]}"
         )]
        [InlineData(@"---
a: b
b:
  c: e
---", "{\"name\":\"0>0>markdown\",\"children\":[{\"name\":\"1>5>YamlHeader>a: b\\nb:\\n  c: e\"}]}")]
        [InlineData(@"# Hello @CrossLink1 @'CrossLink2'dummy 
@World",
             "{\"name\":\"0>0>markdown\",\"children\":[{\"name\":\"1>1>Heading1\",\"children\":[{\"name\":\"1>1>Text>Hello \"},{\"name\":\"1>1>Xref>CrossLink1\",\"children\":[]},{\"name\":\"1>1>Text> \"},{\"name\":\"1>1>Xref>CrossLink2\",\"children\":[]},{\"name\":\"1>1>Text>dummy\"}]},{\"name\":\"2>2>Paragraph\",\"children\":[{\"name\":\"2>2>Xref>World\",\"children\":[]}]}]}"
         )]
        [InlineData("a\n```\nc\n```",
             "{\"name\":\"0>0>markdown\",\"children\":[{\"name\":\"1>1>Paragraph\",\"children\":[{\"name\":\"1>1>Text>a\"}]},{\"name\":\"2>4>Code>c\"}]}"
         )]
        [InlineData(@" *hello* abc @api__1",
             "{\"name\":\"0>0>markdown\",\"children\":[{\"name\":\"1>1>Paragraph\",\"children\":[{\"name\":\"1>1>Text> \"},{\"name\":\"1>1>Em\",\"children\":[{\"name\":\"1>1>Text>hello\"}]},{\"name\":\"1>1>Text> abc \"},{\"name\":\"1>1>Xref>api__1\",\"children\":[]}]}]}"
         )]
        [InlineData("@1abc",
             "{\"name\":\"0>0>markdown\",\"children\":[{\"name\":\"1>1>Paragraph\",\"children\":[{\"name\":\"1>1>Text>@1abc\"}]}]}"
         )]
        [InlineData(@"@api1 @api__1 @api!1 @api@a abc@api.com a.b.c@api.com @'a p ';@""a!pi"",@api...@api",
             "{\"name\":\"0>0>markdown\",\"children\":[{\"name\":\"1>1>Paragraph\",\"children\":[{\"name\":\"1>1>Xref>api1\",\"children\":[]},{\"name\":\"1>1>Text> \"},{\"name\":\"1>1>Xref>api__1\",\"children\":[]},{\"name\":\"1>1>Text> \"},{\"name\":\"1>1>Xref>api!1\",\"children\":[]},{\"name\":\"1>1>Text> \"},{\"name\":\"1>1>Xref>api@a\",\"children\":[]},{\"name\":\"1>1>Text> abc@api.com\"},{\"name\":\"1>1>Text> a.b.c@api.com\"},{\"name\":\"1>1>Text> \"},{\"name\":\"1>1>Xref>a p \",\"children\":[]},{\"name\":\"1>1>Text>;\"},{\"name\":\"1>1>Xref>a!pi\",\"children\":[]},{\"name\":\"1>1>Text>,\"},{\"name\":\"1>1>Xref>api\",\"children\":[]},{\"name\":\"1>1>Text>.\"},{\"name\":\"1>1>Text>.\"},{\"name\":\"1>1>Text>.\"},{\"name\":\"1>1>Xref>api\",\"children\":[]}]}]}"
         )]
        [InlineData("[name](xref:uid \"title\")",
             "{\"name\":\"0>0>markdown\",\"children\":[{\"name\":\"1>1>Paragraph\",\"children\":[{\"name\":\"1>1>Link\",\"children\":[{\"name\":\"1>1>Text>name\"}]}]}]}"
         )]
        [InlineData("<xref:uid>text",
             "{\"name\":\"0>0>markdown\",\"children\":[{\"name\":\"1>1>Paragraph\",\"children\":[{\"name\":\"1>1>Xref>uid\",\"children\":[]},{\"name\":\"1>1>Text>text\"}]}]}"
         )]
        [InlineData("<xref:'uid with space'>text",
             "{\"name\":\"0>0>markdown\",\"children\":[{\"name\":\"1>1>Paragraph\",\"children\":[{\"name\":\"1>1>Xref>uid with space\",\"children\":[]},{\"name\":\"1>1>Text>text\"}]}]}"
         )]
        [InlineData(
             "[*a*](xref:uid)",
             "{\"name\":\"0>0>markdown\",\"children\":[{\"name\":\"1>1>Paragraph\",\"children\":[{\"name\":\"1>1>Link\",\"children\":[{\"name\":\"1>1>Em\",\"children\":[{\"name\":\"1>1>Text>a\"}]}]}]}]}"
         )]
        public void TestDfmInGeneral(string source, string expected)
        {
            TestDfmJsonTokenTreeJsonRender(source, expected);
        }

        [Theory]
        [Trait("Related", "DfmMarkdown")]
        [InlineData(@"The following is video.
> [!Video https://sec.ch9.ms/ch9/4393/7d7c7df7-3f15-4a65-a2f7-3e4d0bea4393/Episode208_mid.mp4]",
             "{\"name\":\"0>0>markdown\",\"children\":[{\"name\":\"1>1>Paragraph\",\"children\":[{\"name\":\"1>1>Text>The following is video.\"}]},{\"name\":\"2>2>Blockquote\",\"children\":[{\"name\":\"2>2>Video>https://sec.ch9.ms/ch9/4393/7d7c7df7-3f15-4a65-a2f7-3e4d0bea4393/Episode208_mid.mp4\"}]}]}"
         )]
        public void TestDfmVideo_Video(string source, string expected)
        {
            TestDfmJsonTokenTreeJsonRender(source, expected);
        }

        [Theory]
        [Trait("Related", "DfmMarkdown")]
        [InlineData(@"The following is two videos.
> [!Video https://sec.ch9.ms/ch9/4393/7d7c7df7-3f15-4a65-a2f7-3e4d0bea4393/Episode208_mid.mp4]
> [!Video https://sec.ch9.ms/ch9/4393/7d7c7df7-3f15-4a65-a2f7-3e4d0bea4393/Episode208_mid.mp4]",
             "{\"name\":\"0>0>markdown\",\"children\":[{\"name\":\"1>1>Paragraph\",\"children\":[{\"name\":\"1>1>Text>The following is two videos.\"}]},{\"name\":\"2>3>Blockquote\",\"children\":[{\"name\":\"2>2>Video>https://sec.ch9.ms/ch9/4393/7d7c7df7-3f15-4a65-a2f7-3e4d0bea4393/Episode208_mid.mp4\"},{\"name\":\"3>3>Video>https://sec.ch9.ms/ch9/4393/7d7c7df7-3f15-4a65-a2f7-3e4d0bea4393/Episode208_mid.mp4\"}]}]}"
         )]
        public void TestDfmVideo_ConsecutiveVideos(string source, string expected)
        {
            TestDfmJsonTokenTreeJsonRender(source, expected);
        }

        [Theory]
        [Trait("Related", "DfmMarkdown")]
        [InlineData(@"The following is video mixed with note.
> [!Video https://sec.ch9.ms/ch9/4393/7d7c7df7-3f15-4a65-a2f7-3e4d0bea4393/Episode208_mid.mp4]
> [!NOTE]
> this is note text
> [!Video https://sec.ch9.ms/ch9/4393/7d7c7df7-3f15-4a65-a2f7-3e4d0bea4393/Episode208_mid.mp4]",
             "{\"name\":\"0>0>markdown\",\"children\":[{\"name\":\"1>1>Paragraph\",\"children\":[{\"name\":\"1>1>Text>The following is video mixed with note.\"}]},{\"name\":\"2>5>Blockquote\",\"children\":[{\"name\":\"2>2>Video>https://sec.ch9.ms/ch9/4393/7d7c7df7-3f15-4a65-a2f7-3e4d0bea4393/Episode208_mid.mp4\"},{\"name\":\"3>3>NOTE\"},{\"name\":\"4>4>Paragraph\",\"children\":[{\"name\":\"4>4>Text>this is note text\"}]},{\"name\":\"5>5>Video>https://sec.ch9.ms/ch9/4393/7d7c7df7-3f15-4a65-a2f7-3e4d0bea4393/Episode208_mid.mp4\"}]}]}"
         )]
        public void TestDfmVideo_MixWithNote(string source, string expected)
        {
            TestDfmJsonTokenTreeJsonRender(source, expected);
        }

        [Theory]
        [Trait("Related", "DfmMarkdown")]
        [InlineData(@"# Title
---
Not yaml syntax
---
hello world",
             "{\"name\":\"0>0>markdown\",\"children\":[{\"name\":\"1>1>Heading1\",\"children\":[{\"name\":\"1>1>Text>Title\"}]},{\"name\":\"2>2>Hr\"},{\"name\":\"3>4>Heading2\",\"children\":[{\"name\":\"3>3>Text>Not yaml syntax\"}]},{\"name\":\"5>5>Paragraph\",\"children\":[{\"name\":\"5>5>Text>hello world\"}]}]}"
         )]
        public void TestYaml_InvalidYamlInsideContent(string source, string expected)
        {
            TestDfmJsonTokenTreeJsonRender(source, expected);
        }

        [Theory]
        [Trait("Related", "DfmMarkdown")]
        [InlineData(@"# Note not in one line
> [!NOTE]hello
> world
> [!WARNING]     Hello world
this is also warning",
             "{\"name\":\"0>0>markdown\",\"children\":[{\"name\":\"1>1>Heading1\",\"children\":[{\"name\":\"1>1>Text>Note not in one line\"}]},{\"name\":\"2>5>Blockquote\",\"children\":[{\"name\":\"2>2>NOTE\"},{\"name\":\"2>3>Paragraph\",\"children\":[{\"name\":\"2>3>Text>hello\\nworld\"}]},{\"name\":\"4>4>WARNING\"},{\"name\":\"4>5>Paragraph\",\"children\":[{\"name\":\"4>5>Text>Hello world\\nthis is also warning\"}]}]}]}"
         )]
        public void TestDfmNote_NoteWithTextFollow(string source, string expected)
        {
            TestDfmJsonTokenTreeJsonRender(source, expected);
        }

        [Theory]
        [Trait("Related", "DfmMarkdown")]
        [InlineData(@"# Note not in one line
> [!NOTE]
> hello
> world
> [!WARNING] Hello world
> [!WARNING]  Hello world this is also warning
> [!WARNING]
> Hello world this is also warning
> [!IMPORTANT]
> Hello world this IMPORTANT",
             "{\"name\":\"0>0>markdown\",\"children\":[{\"name\":\"1>1>Heading1\",\"children\":[{\"name\":\"1>1>Text>Note not in one line\"}]},{\"name\":\"2>10>Blockquote\",\"children\":[{\"name\":\"2>2>NOTE\"},{\"name\":\"3>4>Paragraph\",\"children\":[{\"name\":\"3>4>Text>hello\\nworld\"}]},{\"name\":\"5>5>WARNING\"},{\"name\":\"5>5>Paragraph\",\"children\":[{\"name\":\"5>5>Text>Hello world\"}]},{\"name\":\"6>6>WARNING\"},{\"name\":\"6>6>Paragraph\",\"children\":[{\"name\":\"6>6>Text>Hello world this is also warning\"}]},{\"name\":\"7>7>WARNING\"},{\"name\":\"8>8>Paragraph\",\"children\":[{\"name\":\"8>8>Text>Hello world this is also warning\"}]},{\"name\":\"9>9>IMPORTANT\"},{\"name\":\"10>10>Paragraph\",\"children\":[{\"name\":\"10>10>Text>Hello world this IMPORTANT\"}]}]}]}"
         )]
        public void TestDfmNote_NoteWithMix(string source, string expected)
        {
            TestDfmJsonTokenTreeJsonRender(source, expected);
        }

        [Theory]
        [Trait("Related", "DfmMarkdown")]
        [InlineData(@"---

### /Unconfigure

---",
             "{\"name\":\"0>0>markdown\",\"children\":[{\"name\":\"1>1>Hr\"},{\"name\":\"3>3>Heading3\",\"children\":[{\"name\":\"3>3>Text>/Unconfigure\"}]},{\"name\":\"5>5>Hr\"}]}"
         )]
        public void TestDfmYamlHeader_YamlUtilityReturnNull(string source, string expected)
        {
            TestDfmJsonTokenTreeJsonRender(source, expected);
        }

        [Theory]
        [Trait("Related", "DfmMarkdown")]
        [InlineData(@"---
- Jon Schlinkert
- Brian Woodward

---",
             "{\"name\":\"0>0>markdown\",\"children\":[{\"name\":\"1>1>Hr\"},{\"name\":\"2>3>ul\",\"children\":[{\"name\":\"2>2>ListItem\",\"children\":[{\"name\":\"2>2>NonParagraph\",\"children\":[{\"name\":\"2>2>Text>Jon Schlinkert\"}]}]},{\"name\":\"3>3>ListItem\",\"children\":[{\"name\":\"3>3>NonParagraph\",\"children\":[{\"name\":\"3>3>Text>Brian Woodward\"}]}]}]},{\"name\":\"5>5>Hr\"}]}"
         )]
        public void TestDfm_InvalidYamlHeader_YamlUtilityThrowException(string source, string expected)
        {
            TestDfmJsonTokenTreeJsonRender(source, expected);
        }

        [Theory]
        [Trait("Related", "DfmMarkdown")]
        [InlineData(@"the following is note type
  > [!NOTE]
  > note text 1-1
  > note text 1-2  
  > note text 2-1
This is also note  
This is also note with br

Skip the note",
             "{\"name\":\"0>0>markdown\",\"children\":[{\"name\":\"1>1>Paragraph\",\"children\":[{\"name\":\"1>1>Text>the following is note type\"}]},{\"name\":\"2>7>Blockquote\",\"children\":[{\"name\":\"2>2>NOTE\"},{\"name\":\"3>7>Paragraph\",\"children\":[{\"name\":\"3>4>Text>note text 1-1\\nnote text 1-2\"},{\"name\":\"4>4>Br\"},{\"name\":\"5>6>Text>note text 2-1\\nThis is also note\"},{\"name\":\"6>6>Br\"},{\"name\":\"7>7>Text>This is also note with br\"}]}]},{\"name\":\"9>9>Paragraph\",\"children\":[{\"name\":\"9>9>Text>Skip the note\"}]}]}"
         )]
        [InlineData(@"the following is not note type
  > no-note text 1-1
  > [!NOTE]
  > no-note text 1-2  
  > no-note text 2-1",
             "{\"name\":\"0>0>markdown\",\"children\":[{\"name\":\"1>1>Paragraph\",\"children\":[{\"name\":\"1>1>Text>the following is not note type\"}]},{\"name\":\"2>5>Blockquote\",\"children\":[{\"name\":\"2>2>Paragraph\",\"children\":[{\"name\":\"2>2>Text>no-note text 1-1\"}]},{\"name\":\"3>3>NOTE\"},{\"name\":\"4>5>Paragraph\",\"children\":[{\"name\":\"4>4>Text>no-note text 1-2\"},{\"name\":\"4>4>Br\"},{\"name\":\"5>5>Text>no-note text 2-1\"}]}]}]}"
         )]
        [InlineData(@"the following is not note type
  > no-note text 1-1
  >
  > [!NOTE]
  > no-note text 2-1  
  > no-note text 2-2",
             "{\"name\":\"0>0>markdown\",\"children\":[{\"name\":\"1>1>Paragraph\",\"children\":[{\"name\":\"1>1>Text>the following is not note type\"}]},{\"name\":\"2>6>Blockquote\",\"children\":[{\"name\":\"2>2>Paragraph\",\"children\":[{\"name\":\"2>2>Text>no-note text 1-1\"}]},{\"name\":\"4>4>NOTE\"},{\"name\":\"5>6>Paragraph\",\"children\":[{\"name\":\"5>5>Text>no-note text 2-1\"},{\"name\":\"5>5>Br\"},{\"name\":\"6>6>Text>no-note text 2-2\"}]}]}]}"
         )]
        [InlineData(@"the following is code

    > code text 1-1
    > [!NOTE]
    > code text 1-2  
    > code text 2-1",
             "{\"name\":\"0>0>markdown\",\"children\":[{\"name\":\"1>1>Paragraph\",\"children\":[{\"name\":\"1>1>Text>the following is code\"}]},{\"name\":\"3>6>Code>&gt; code text 1-1\\n&gt; [!NOTE]\\n&gt; code text 1-2  \\n&gt; code text 2-1\"}]}"
         )]
        public void TestSectionNoteInBlockQuote(string source, string expected)
        {
            TestDfmJsonTokenTreeJsonRender(source, expected);
        }

        [Theory]
        [Trait("Related", "DfmMarkdown")]
        [InlineData(@"> [!div class=""All"" id=""All""] Followed text
> We should support that.",
             "{\"name\":\"0>0>markdown\",\"children\":[{\"name\":\"1>2>Blockquote\",\"children\":[{\"name\":\"1>1>Section\"},{\"name\":\"1>2>Paragraph\",\"children\":[{\"name\":\"1>2>Text>Followed text\\nWe should support that.\"}]}]}]}"
         )]
        public void TestSectionWithTextFollowed(string source, string expected)
        {
            TestDfmJsonTokenTreeJsonRender(source, expected);
        }

        [Theory]
        [Trait("Related", "DfmMarkdown")]
        [InlineData(@"> [!div class=""All"" id=""All""]
> this is out all
> > [!div class=""A"" id=""A""]
> > this is A
> > [!div class=""B"" id=""B""]
> > this is B",
             "{\"name\":\"0>0>markdown\",\"children\":[{\"name\":\"1>6>Blockquote\",\"children\":[{\"name\":\"1>1>Section\"},{\"name\":\"2>2>Paragraph\",\"children\":[{\"name\":\"2>2>Text>this is out all\"}]},{\"name\":\"3>6>Blockquote\",\"children\":[{\"name\":\"3>3>Section\"},{\"name\":\"4>4>Paragraph\",\"children\":[{\"name\":\"4>4>Text>this is A\"}]},{\"name\":\"5>5>Section\"},{\"name\":\"6>6>Paragraph\",\"children\":[{\"name\":\"6>6>Text>this is B\"}]}]}]}]}"
         )]
        public void TestSectionBlockLevelRecursive(string source, string expected)
        {
            TestDfmJsonTokenTreeJsonRender(source, expected);
        }

        [Theory]
        [Trait("Related", "DfmMarkdown")]
        [InlineData(@"> this is blockquote
>
> this line is also in the before blockquote
> [!NOTE]
> This is note text
> [!WARNING]
> This is warning text
> [!div class=""a"" id=""diva""]
> this is div with class a and id diva
> text also in div
> [!div class=""b"" cause=""divb""]
> this is div with class b and cause divb
> [!IMPORTANT]
> This is imoprtant text follow div",
             "{\"name\":\"0>0>markdown\",\"children\":[{\"name\":\"1>14>Blockquote\",\"children\":[{\"name\":\"1>1>Paragraph\",\"children\":[{\"name\":\"1>1>Text>this is blockquote\"}]},{\"name\":\"3>3>Paragraph\",\"children\":[{\"name\":\"3>3>Text>this line is also in the before blockquote\"}]},{\"name\":\"4>4>NOTE\"},{\"name\":\"5>5>Paragraph\",\"children\":[{\"name\":\"5>5>Text>This is note text\"}]},{\"name\":\"6>6>WARNING\"},{\"name\":\"7>7>Paragraph\",\"children\":[{\"name\":\"7>7>Text>This is warning text\"}]},{\"name\":\"8>8>Section\"},{\"name\":\"9>10>Paragraph\",\"children\":[{\"name\":\"9>10>Text>this is div with class a and id diva\\ntext also in div\"}]},{\"name\":\"11>11>Section\"},{\"name\":\"12>12>Paragraph\",\"children\":[{\"name\":\"12>12>Text>this is div with class b and cause divb\"}]},{\"name\":\"13>13>IMPORTANT\"},{\"name\":\"14>14>Paragraph\",\"children\":[{\"name\":\"14>14>Text>This is imoprtant text follow div\"}]}]}]}"
         )]
        public void TestSectionNoteMixture(string source, string expected)
        {
            TestDfmJsonTokenTreeJsonRender(source, expected);
        }

        [Theory]
        [Trait("Related", "DfmMarkdown")]
        [InlineData(@"> [!div]",
             "{\"name\":\"0>0>markdown\",\"children\":[{\"name\":\"1>1>Blockquote\",\"children\":[{\"name\":\"1>1>Section\"}]}]}"
         )]
        [InlineData(@"> [!div `id=""error""]",
             "{\"name\":\"0>0>markdown\",\"children\":[{\"name\":\"1>1>Blockquote\",\"children\":[{\"name\":\"1>1>Section\"}]}]}"
         )]
        [InlineData(@"> [!div `id=""right""`]",
             "{\"name\":\"0>0>markdown\",\"children\":[{\"name\":\"1>1>Blockquote\",\"children\":[{\"name\":\"1>1>Section\"}]}]}"
         )]
        public void TestSectionCornerCase(string source, string expected)
        {
            TestDfmJsonTokenTreeJsonRender(source, expected);
        }

        [Theory]
        [Trait("Related", "DfmMarkdown")]
        [InlineData(@"> [!div class=""op_single_selector""]
> * [Universal Windows](../articles/notification-hubs-windows-store-dotnet-get-started/.md)
> * [Windows Phone](../articles/notification-hubs-windows-phone-get-started/.md)
> * [iOS](../articles/notification-hubs-ios-get-started/.md)
> * [Android](../articles/notification-hubs-android-get-started/.md)
> * [Kindle](../articles/notification-hubs-kindle-get-started/.md)
> * [Baidu](../articles/notification-hubs-baidu-get-started/.md)
> * [Xamarin.iOS](../articles/partner-xamarin-notification-hubs-ios-get-started/.md)
> * [Xamarin.Android](../articles/partner-xamarin-notification-hubs-android-get-started/.md)
> 
> ",
             "{\"name\":\"0>0>markdown\",\"children\":[{\"name\":\"1>11>Blockquote\",\"children\":[{\"name\":\"1>1>Section\"},{\"name\":\"2>9>ul\",\"children\":[{\"name\":\"2>2>ListItem\",\"children\":[{\"name\":\"2>2>NonParagraph\",\"children\":[{\"name\":\"2>2>Link\",\"children\":[{\"name\":\"2>2>Text>Universal Windows\"}]}]}]},{\"name\":\"3>3>ListItem\",\"children\":[{\"name\":\"3>3>NonParagraph\",\"children\":[{\"name\":\"3>3>Link\",\"children\":[{\"name\":\"3>3>Text>Windows Phone\"}]}]}]},{\"name\":\"4>4>ListItem\",\"children\":[{\"name\":\"4>4>NonParagraph\",\"children\":[{\"name\":\"4>4>Link\",\"children\":[{\"name\":\"4>4>Text>iOS\"}]}]}]},{\"name\":\"5>5>ListItem\",\"children\":[{\"name\":\"5>5>NonParagraph\",\"children\":[{\"name\":\"5>5>Link\",\"children\":[{\"name\":\"5>5>Text>Android\"}]}]}]},{\"name\":\"6>6>ListItem\",\"children\":[{\"name\":\"6>6>NonParagraph\",\"children\":[{\"name\":\"6>6>Link\",\"children\":[{\"name\":\"6>6>Text>Kindle\"}]}]}]},{\"name\":\"7>7>ListItem\",\"children\":[{\"name\":\"7>7>NonParagraph\",\"children\":[{\"name\":\"7>7>Link\",\"children\":[{\"name\":\"7>7>Text>Baidu\"}]}]}]},{\"name\":\"8>8>ListItem\",\"children\":[{\"name\":\"8>8>NonParagraph\",\"children\":[{\"name\":\"8>8>Link\",\"children\":[{\"name\":\"8>8>Text>Xamarin.iOS\"}]}]}]},{\"name\":\"9>9>ListItem\",\"children\":[{\"name\":\"9>9>NonParagraph\",\"children\":[{\"name\":\"9>9>Link\",\"children\":[{\"name\":\"9>9>Text>Xamarin.Android\"}]}]}]}]}]}]}"
         )]
        public void TestSection_AzureSingleSelector(string source, string expected)
        {
            TestDfmJsonTokenTreeJsonRender(source, expected);
        }

        [Theory]
        [Trait("Related", "DfmMarkdown")]
        [InlineData(@"> [!div class=""op_multi_selector"" title1=""Platform"" title2=""Backend""]
> * [(iOS | .NET)](./mobile-services-dotnet-backend-ios-get-started-push.md)
> * [(iOS | JavaScript)](./mobile-services-javascript-backend-ios-get-started-push.md)
> * [(Windows universal C# | .NET)](./mobile-services-dotnet-backend-windows-universal-dotnet-get-started-push.md)
> * [(Windows universal C# | Javascript)](./mobile-services-javascript-backend-windows-universal-dotnet-get-started-push.md)
> * [(Windows Phone | .NET)](./mobile-services-dotnet-backend-windows-phone-get-started-push.md)
> * [(Windows Phone | Javascript)](./mobile-services-javascript-backend-windows-phone-get-started-push.md)
> * [(Android | .NET)](./mobile-services-dotnet-backend-android-get-started-push.md)
> * [(Android | Javascript)](./mobile-services-javascript-backend-android-get-started-push.md)
> * [(Xamarin iOS | Javascript)](./partner-xamarin-mobile-services-ios-get-started-push.md)
> * [(Xamarin Android | Javascript)](./partner-xamarin-mobile-services-android-get-started-push.md)
> 
> ",
             "{\"name\":\"0>0>markdown\",\"children\":[{\"name\":\"1>13>Blockquote\",\"children\":[{\"name\":\"1>1>Section\"},{\"name\":\"2>11>ul\",\"children\":[{\"name\":\"2>2>ListItem\",\"children\":[{\"name\":\"2>2>NonParagraph\",\"children\":[{\"name\":\"2>2>Link\",\"children\":[{\"name\":\"2>2>Text>(iOS | .NET)\"}]}]}]},{\"name\":\"3>3>ListItem\",\"children\":[{\"name\":\"3>3>NonParagraph\",\"children\":[{\"name\":\"3>3>Link\",\"children\":[{\"name\":\"3>3>Text>(iOS | JavaScript)\"}]}]}]},{\"name\":\"4>4>ListItem\",\"children\":[{\"name\":\"4>4>NonParagraph\",\"children\":[{\"name\":\"4>4>Link\",\"children\":[{\"name\":\"4>4>Text>(Windows universal C# | .NET)\"}]}]}]},{\"name\":\"5>5>ListItem\",\"children\":[{\"name\":\"5>5>NonParagraph\",\"children\":[{\"name\":\"5>5>Link\",\"children\":[{\"name\":\"5>5>Text>(Windows universal C# | Javascript)\"}]}]}]},{\"name\":\"6>6>ListItem\",\"children\":[{\"name\":\"6>6>NonParagraph\",\"children\":[{\"name\":\"6>6>Link\",\"children\":[{\"name\":\"6>6>Text>(Windows Phone | .NET)\"}]}]}]},{\"name\":\"7>7>ListItem\",\"children\":[{\"name\":\"7>7>NonParagraph\",\"children\":[{\"name\":\"7>7>Link\",\"children\":[{\"name\":\"7>7>Text>(Windows Phone | Javascript)\"}]}]}]},{\"name\":\"8>8>ListItem\",\"children\":[{\"name\":\"8>8>NonParagraph\",\"children\":[{\"name\":\"8>8>Link\",\"children\":[{\"name\":\"8>8>Text>(Android | .NET)\"}]}]}]},{\"name\":\"9>9>ListItem\",\"children\":[{\"name\":\"9>9>NonParagraph\",\"children\":[{\"name\":\"9>9>Link\",\"children\":[{\"name\":\"9>9>Text>(Android | Javascript)\"}]}]}]},{\"name\":\"10>10>ListItem\",\"children\":[{\"name\":\"10>10>NonParagraph\",\"children\":[{\"name\":\"10>10>Link\",\"children\":[{\"name\":\"10>10>Text>(Xamarin iOS | Javascript)\"}]}]}]},{\"name\":\"11>11>ListItem\",\"children\":[{\"name\":\"11>11>NonParagraph\",\"children\":[{\"name\":\"11>11>Link\",\"children\":[{\"name\":\"11>11>Text>(Xamarin Android | Javascript)\"}]}]}]}]}]}]}"
         )]
        public void TestSection_AzureMultiSelector(string source, string expected)
        {
            TestDfmJsonTokenTreeJsonRender(source, expected);
        }

        [Theory]
        [Trait("Related", "DfmMarkdown")]
        [InlineData(@"![This is image alt text with quotation ' and double quotation ""hello"" world](girl.png)",
             "{\"name\":\"0>0>markdown\",\"children\":[{\"name\":\"1>1>Paragraph\",\"children\":[{\"name\":\"1>1>Image>girl.png\"}]}]}"
         )]
        public void TestDfmImageLink_WithSpecialCharactorsInAltText(string source, string expected)
        {
            TestDfmJsonTokenTreeJsonRender(source, expected);
        }

        [Theory]
        [Trait("Related", "DfmMarkdown")]
        [InlineData(@"[text's string](https://www.google.com.sg/?gfe_rd=cr&ei=Xk ""Google's homepage"")",
             "{\"name\":\"0>0>markdown\",\"children\":[{\"name\":\"1>1>Paragraph\",\"children\":[{\"name\":\"1>1>Link\",\"children\":[{\"name\":\"1>1>Text>text&#39;s string\"}]}]}]}"
         )]
        public void TestDfmLink_LinkWithSpecialCharactorsInTitle(string source, string expected)
        {
            TestDfmJsonTokenTreeJsonRender(source, expected);
        }

        [Theory]
        [Trait("Related", "DfmMarkdown")]
        [InlineData(
             @"[This is link text with quotation ' and double quotation ""hello"" world](girl.png ""title is ""hello"" world."")",
             "{\"name\":\"0>0>markdown\",\"children\":[{\"name\":\"1>1>Paragraph\",\"children\":[{\"name\":\"1>1>Link\",\"children\":[{\"name\":\"1>1>Text>This is link text with quotation &#39; and double quotation &quot;hello&quot; world\"}]}]}]}"
         )]
        public void TestDfmLink_WithSpecialCharactorsInTitle(string source, string expected)
        {
            TestDfmJsonTokenTreeJsonRender(source, expected);
        }

        [Theory]
        [Trait("Related", "DfmMarkdown")]
        [InlineData(
             @"[User-Defined Date/Time Formats (Format Function)](http://msdn2.microsoft.com/library/73ctwf33\(VS.90\).aspx)",
             "{\"name\":\"0>0>markdown\",\"children\":[{\"name\":\"1>1>Paragraph\",\"children\":[{\"name\":\"1>1>Link\",\"children\":[{\"name\":\"1>1>Text>User-Defined Date/Time Formats (Format Function)\"}]}]}]}"
         )]
        public void TestPathUtility_AbsoluteLinkWithBracketAndBrackt(string source, string expected)
        {
            TestDfmJsonTokenTreeJsonRender(source, expected);
        }

        [Theory]
        [Trait("Related", "DfmMarkdown")]
        [InlineData(@"[!code-FakeREST[REST](api.json)]
[!Code-FakeREST-i[REST-i](api.json ""This is root"")]
[!CODE[No Language](api.json)]
[!code-js[empty](api.json)]",
             "{\"name\":\"0>0>markdown\",\"children\":[{\"name\":\"1>1>Fences\"},{\"name\":\"2>2>Fences\"},{\"name\":\"3>3>Fences\"},{\"name\":\"4>4>Fences\"}]}"
         )]
        public void TestDfmFencesLevel(string source, string expected)
        {
            TestDfmJsonTokenTreeJsonRender(source, expected);
        }

        private void TestDfmJsonTokenTreeJsonRender(string source, string expected)
        {
            DfmJsonTokenTreeServiceProvider dfmJsonTokenTreeServiceProvider = new DfmJsonTokenTreeServiceProvider();
            IMarkdownService dfmMarkdownService =
                dfmJsonTokenTreeServiceProvider.CreateMarkdownService(new MarkdownServiceParameters());
            var result = dfmMarkdownService.Markup(source, null).Html;
            Assert.Equal(expected, result);
        }
    }
}
