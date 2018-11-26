// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Tests
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Xml;

    using MarkdigEngine.Extensions;

    using Microsoft.DocAsCode.Plugins;
    using Xunit;

    public class QuoteSectionNoteTest
    {
        private void TestMarkup(string source, string expected)
        {
            var marked = TestUtility.MarkupWithoutSourceInfo(source, "Topic.md");
            Assert.Equal(expected.Replace("\r\n", "\n"), marked.Html);
        }

        [Fact]
        [Trait("Related", "QuoteSectionNote")]
        public void QuoteSectionNoteTest_CornerCases()
        {
            var source = @"> [!Video https://test]
> [!Video]
> [!NOTE] no text here
> [!TIP
> [!di-no-v class=""whatever""]
> [!WARNING]";
            var expected = @"<div class=""embeddedvideo""><iframe src=""https://test/"" frameborder=""0"" allowfullscreen=""true""></iframe></div>
<blockquote>
<p>[!Video]
[!NOTE] no text here
[!TIP
[!di-no-v class=&quot;whatever&quot;]</p>
</blockquote>
<div class=""WARNING"">
<h5>WARNING</h5>
</div>
";
            TestMarkup(source, expected);
        }

        [Fact]
        [Trait("Related", "QuoteSectionNote")]
        public void QuoteSectionNoteTest_TabInSection()
        {
            var source = "> [!div\t\tclass=\"tab\"] \n> section";
            var expected = @"<div class=""tab"">
<p>section</p>
</div>
";
            TestMarkup(source, expected);
        }

        [Fact]
        [Trait("Related", "DfmMarkdown")]
        public void TestDfmNote_NoteWithLocalization()
        {
            var source = @"# Note not in one line
> [!NOTE]
> hello
> world
> [!WARNING]
> Hello world
this is also warning";
            var expected = @"<h1 id=""note-not-in-one-line"">Note not in one line</h1>
<div class=""NOTE"">
<h5>注意</h5>
<p>hello
world</p>
</div>
<div class=""WARNING"">
<h5>警告</h5>
<p>Hello world
this is also warning</p>
</div>
";
            var parameter = new MarkdownServiceParameters
            {
                BasePath = ".",
                Tokens = new Dictionary<string, string>
                {
                    {"note", "<h5>注意</h5>"},
                    {"warning", "<h5>警告</h5>" }
                }.ToImmutableDictionary(),
                Extensions = new Dictionary<string, object>
                {
                    { "EnableSourceInfo", false }
                }
            };
            var service = new MarkdigMarkdownService(parameter);
            var marked = service.Markup(source, "Topic.md");
            Assert.Equal(expected.Replace("\r\n", "\n"), marked.Html);
        }

        [Fact]
        [Trait("Related", "DfmMarkdown")]
        public void TestDfmNote_NoteCaseSensitive()
        {
            var source = @"> [!noTe]
> hello
> [!WARNING]";
            var expected = @"<div class=""NOTE"">
<h5>NOTE</h5>
<p>hello</p>
</div>
<div class=""WARNING"">
<h5>WARNING</h5>
</div>
";
            TestMarkup(source, expected);
        }

        [Fact]
        [Trait("Related", "DfmMarkdown")]
        public void TestDfmNote_NoteWithMix()
        {
            var source = @"# Note not in one line
> [!NOTE]
> hello
> world
> [!WARNING]
> Hello world
> [!WARNING]
>  Hello world this is also warning
> [!WARNING]
> Hello world this is also warning
> [!IMPORTANT]
> Hello world this IMPORTANT";
            var expected = @"<h1 id=""note-not-in-one-line"">Note not in one line</h1>
<div class=""NOTE"">
<h5>NOTE</h5>
<p>hello
world</p>
</div>
<div class=""WARNING"">
<h5>WARNING</h5>
<p>Hello world</p>
</div>
<div class=""WARNING"">
<h5>WARNING</h5>
<p>Hello world this is also warning</p>
</div>
<div class=""WARNING"">
<h5>WARNING</h5>
<p>Hello world this is also warning</p>
</div>
<div class=""IMPORTANT"">
<h5>IMPORTANT</h5>
<p>Hello world this IMPORTANT</p>
</div>
";
            TestMarkup(source, expected);
        }

        [Fact]
        [Trait("Related", "DfmMarkdown")]
        public void TestDfmNote_NoteWithTextFollow()
        {
            var source = @"# Note not in one line
> [!NOTE]
> hello
> world
> [!WARNING]
>   Hello world
this is also warning";
            var expected = @"<h1 id=""note-not-in-one-line"">Note not in one line</h1>
<div class=""NOTE"">
<h5>NOTE</h5>
<p>hello
world</p>
</div>
<div class=""WARNING"">
<h5>WARNING</h5>
<p>Hello world
this is also warning</p>
</div>
";
            TestMarkup(source, expected);
        }

        [Fact]
        [Trait("Related", "DfmMarkdown")]
        public void TestDfmVideo_ConsecutiveVideos()
        {
            // 1. Prepare data
            var root = @"The following is two videos.
> [!Video https://sec.ch9.ms/ch9/4393/7d7c7df7-3f15-4a65-a2f7-3e4d0bea4393/Episode208_mid.mp4]
> [!Video https://sec.ch9.ms/ch9/4393/7d7c7df7-3f15-4a65-a2f7-3e4d0bea4393/Episode208_mid.mp4]";

            var expected = @"<p>The following is two videos.</p>
<div class=""embeddedvideo""><iframe src=""https://sec.ch9.ms/ch9/4393/7d7c7df7-3f15-4a65-a2f7-3e4d0bea4393/Episode208_mid.mp4"" frameborder=""0"" allowfullscreen=""true""></iframe></div>
<div class=""embeddedvideo""><iframe src=""https://sec.ch9.ms/ch9/4393/7d7c7df7-3f15-4a65-a2f7-3e4d0bea4393/Episode208_mid.mp4"" frameborder=""0"" allowfullscreen=""true""></iframe></div>
";

            TestMarkup(root, expected);
        }

        [Fact]
        [Trait("Related", "DfmMarkdown")]
        public void TestDfmVideo_Video()
        {
            // 1. Prepare data
            var root = @"The following is video.
> [!Video https://sec.ch9.ms/ch9/4393/7d7c7df7-3f15-4a65-a2f7-3e4d0bea4393/Episode208_mid.mp4]
";

            var expected = @"<p>The following is video.</p>
<div class=""embeddedvideo""><iframe src=""https://sec.ch9.ms/ch9/4393/7d7c7df7-3f15-4a65-a2f7-3e4d0bea4393/Episode208_mid.mp4"" frameborder=""0"" allowfullscreen=""true""></iframe></div>
";

            TestMarkup(root, expected);
        }

        [Fact]
        [Trait("Related", "DfmMarkdown")]
        public void TestDfmVideo_MixWithNote()
        {
            // 1. Prepare data
            var source = @"The following is video mixed with note.
> [!Video https://sec.ch9.ms/ch9/4393/7d7c7df7-3f15-4a65-a2f7-3e4d0bea4393/Episode208_mid.mp4]
> [!NOTE]
> this is note text
> [!Video https://sec.ch9.ms/ch9/4393/7d7c7df7-3f15-4a65-a2f7-3e4d0bea4393/Episode208_mid.mp4]";

            var expected = @"<p>The following is video mixed with note.</p>
<div class=""embeddedvideo""><iframe src=""https://sec.ch9.ms/ch9/4393/7d7c7df7-3f15-4a65-a2f7-3e4d0bea4393/Episode208_mid.mp4"" frameborder=""0"" allowfullscreen=""true""></iframe></div>
<div class=""NOTE"">
<h5>NOTE</h5>
<p>this is note text</p>
</div>
<div class=""embeddedvideo""><iframe src=""https://sec.ch9.ms/ch9/4393/7d7c7df7-3f15-4a65-a2f7-3e4d0bea4393/Episode208_mid.mp4"" frameborder=""0"" allowfullscreen=""true""></iframe></div>
";

            TestMarkup(source, expected);
        }

        [Theory]
        [Trait("Related", "DfmMarkdown")]
        #region Inline Data
        [InlineData(@"the following is note type
  > [!NOTE]
  > note text 1-1
  > note text 1-2  
  > note text 2-1
This is also note  
This is also note with br

Skip the note
", @"<p>the following is note type</p>
<div class=""NOTE"">
<h5>NOTE</h5>
<p>note text 1-1
note text 1-2<br />
note text 2-1
This is also note<br />
This is also note with br</p>
</div>
<p>Skip the note</p>
")]
        [InlineData(@"the following is not note type
  > no-note text 1-1
  > [!NOTE]
  > no-note text 1-2  
  > no-note text 2-1
", @"<p>the following is not note type</p>
<blockquote>
<p>no-note text 1-1</p>
</blockquote>
<div class=""NOTE"">
<h5>NOTE</h5>
<p>no-note text 1-2<br />
no-note text 2-1</p>
</div>
")]
        [InlineData(@"the following is not note type
  > no-note text 1-1
  >
  > [!NOTE]
  > no-note text 2-1  
  > no-note text 2-2
", @"<p>the following is not note type</p>
<blockquote>
<p>no-note text 1-1</p>
</blockquote>
<div class=""NOTE"">
<h5>NOTE</h5>
<p>no-note text 2-1<br />
no-note text 2-2</p>
</div>
")]
        [InlineData(@"the following is code

    > code text 1-1
    > [!NOTE]
    > code text 1-2  
    > code text 2-1
", @"<p>the following is code</p>
<pre><code>&gt; code text 1-1
&gt; [!NOTE]
&gt; code text 1-2  
&gt; code text 2-1
</code></pre>
")]
        #endregion
        public void TestSectionNoteInBlockQuote(string source, string expected)
        {
            TestMarkup(source, expected);
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
> This is imoprtant text follow div")]
        public void TestSectionNoteMixture(string source)
        {
            var expected = "<blockquote>\n<p>this is blockquote</p>\n<p>this line is also in the before blockquote</p>\n</blockquote>\n<div class=\"NOTE\">\n<h5>NOTE</h5>\n<p>This is note text</p>\n</div>\n<div class=\"WARNING\">\n<h5>WARNING</h5>\n<p>This is warning text</p>\n</div>\n<div class=\"a\" id=\"diva\">\n<p>this is div with class a and id diva\ntext also in div</p>\n</div>\n<div class=\"b\" cause=\"divb\">\n<p>this is div with class b and cause divb</p>\n</div>\n<div class=\"IMPORTANT\">\n<h5>IMPORTANT</h5>\n<p>This is imoprtant text follow div</p>\n</div>\n";
            TestMarkup(source, expected);
        }

        [Theory]
        [Trait("Related", "DfmMarkdown")]
        [InlineData(@"> [!div class=""tabbedCodeSnippets"" data-resources=""OutlookServices.Calendar""]
>
>```cs-i
>    var outlookClient = await CreateOutlookClientAsync(""Calendar"");
>    var events = await outlookClient.Me.Events.Take(10).ExecuteAsync();
>            foreach (var calendarEvent in events.CurrentPage)
>            {
>                System.Diagnostics.Debug.WriteLine(""Event '{0}'."", calendarEvent.Subject);
>            }
>```
> 
>```javascript-i
>outlookClient.me.events.getEvents().fetch().then(function(result) {
>        result.currentPage.forEach(function(event) {
>        console.log('Event ""' + event.subject + '""')
>    });
>}, function(error)
>    {
>        console.log(error);
>    });
>```")]
        public void TestSectionBlockLevel(string source)
        {
            var parameter = new MarkdownServiceParameters
            {
                BasePath = "."
            };
            var service = new MarkdigMarkdownService(parameter);
            var content = service.Markup(source, "Topic.md");

            // assert
            XmlDocument xdoc = new XmlDocument();
            xdoc.LoadXml(content.Html);
            var tabbedCodeNode = xdoc.SelectSingleNode("//div[@class='tabbedCodeSnippets' and @data-resources='OutlookServices.Calendar']");
            Assert.True(tabbedCodeNode != null);
            var csNode = tabbedCodeNode.SelectSingleNode("./pre/code[@class='lang-cs-i']");
            Assert.True(csNode != null);
            var jsNode = tabbedCodeNode.SelectSingleNode("./pre/code[@class='lang-javascript-i']");
            Assert.True(jsNode != null);
        }

        [Theory]
        [Trait("Related", "DfmMarkdown")]
        [InlineData(@"> [!div class=""All"" id=""All""]
> this is out all
> > [!div class=""A"" id=""A""]
> > this is A
> > [!div class=""B"" id=""B""]
> > this is B")]
        public void TestSectionBlockLevelRecursive(string source)
        {
            var expected = @"<div class=""All"" id=""All"">
<p>this is out all</p>
<div class=""A"" id=""A"">
<p>this is A</p>
</div>
<div class=""B"" id=""B"">
<p>this is B</p>
</div>
</div>
";
            TestMarkup(source, expected);
        }

        [Theory]
        [Trait("Related", "DfmMarkdown")]
        [InlineData(@"> [!div]", "<div>\n</div>\n")]
        [InlineData(@"> [!div `id=""error""]", "<div>\n</div>\n")]
        [InlineData(@"> [!div `id=""right""`]", "<div id=\"right\">\n</div>\n")]
        public void TestSectionCornerCase(string source, string expected)
        {
            TestMarkup(source, expected);
        }

        [Theory]
        [Trait("Related", "DfmMarkdown")]
        [InlineData(@"> [!div class=""All"" id=""All""] Followed text
> We should support that.")]
        public void TestSectionWithTextFollowed(string source)
        {
            // not supported, render this to block quote
            string expected = @"<blockquote>
<p>[!div class=&quot;All&quot; id=&quot;All&quot;] Followed text
We should support that.</p>
</blockquote>
";
            TestMarkup(source, expected);
        }

        [Fact]
        [Trait("Related", "DfmMarkdown")]
        public void TestVideoBlock_Normal()
        {
            var source = @"# Article 2
> [!VIDEO https://microsoft.com:8080?query=value+A#bookmark]
";
            var expected = $@"<h1 id=""article-2"">Article 2</h1>
<div class=""embeddedvideo""><iframe src=""https://microsoft.com:8080/?query=value+A#bookmark"" frameborder=""0"" allowfullscreen=""true""></iframe></div>
";
            TestMarkup(source, expected);
        }

        [Fact]
        [Trait("Related", "DfmMarkdown")]
        public void TestVideoBlock_Channel9()
        {
            var source = @"# Article 2
> [!VIDEO https://channel9.msdn.com]
";
            var expected = $@"<h1 id=""article-2"">Article 2</h1>
<div class=""embeddedvideo""><iframe src=""https://channel9.msdn.com/?nocookie=true"" frameborder=""0"" allowfullscreen=""true""></iframe></div>
";
            TestMarkup(source, expected);
        }

        [Fact]
        [Trait("Related", "DfmMarkdown")]
        public void TestVideoBlock_Channel9WithQueryString()
        {
            var source = @"# Article 2
> [!VIDEO https://channel9.msdn.com?query=value+A]
";
            var expected = $@"<h1 id=""article-2"">Article 2</h1>
<div class=""embeddedvideo""><iframe src=""https://channel9.msdn.com/?query=value+A&nocookie=true"" frameborder=""0"" allowfullscreen=""true""></iframe></div>
";
            TestMarkup(source, expected);
        }

        [Fact]
        [Trait("Related", "DfmMarkdown")]
        public void TestVideoBlock_YouTube()
        {
            var source = @"# Article 2
> [!VIDEO https://youtube.com/foo]
";
            var expected = $@"<h1 id=""article-2"">Article 2</h1>
<div class=""embeddedvideo""><iframe src=""https://www.youtube-nocookie.com/foo"" frameborder=""0"" allowfullscreen=""true""></iframe></div>
";
            TestMarkup(source, expected);
        }
    }
}
