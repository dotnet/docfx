// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm.Tests
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Xml;

    using Xunit;

    using Microsoft.DocAsCode.Dfm;

    public class DfmTest
    {
        [Theory]
        [Trait("Related", "DfmMarkdown")]
        #region Inline Data
        [InlineData("", "")]
        [InlineData("<address@example.com>", "<p><a href=\"mailto:address@example.com\" data-raw-source=\"&lt;address@example.com&gt;\">address@example.com</a></p>\n")]
        [InlineData(" https://github.com/dotnet/docfx/releases ", "<p> <a href=\"https://github.com/dotnet/docfx/releases\" data-raw-source=\"https://github.com/dotnet/docfx/releases\">https://github.com/dotnet/docfx/releases</a> </p>\n")]
        [InlineData(@"<Insert OneGet Details - meeting on 10/30 for details.>", @"&lt;Insert OneGet Details - meeting on 10/30 for details.&gt;")]
        [InlineData("<http://example.com/>", "<p><a href=\"http://example.com/\" data-raw-source=\"&lt;http://example.com/&gt;\">http://example.com/</a></p>\n")]
        [InlineData("# Hello World", "<h1 id=\"hello-world\">Hello World</h1>\n")]
        [InlineData("Hot keys: <kbd>Ctrl+[</kbd> and <kbd>Ctrl+]</kbd>", "<p>Hot keys: <kbd>Ctrl+[</kbd> and <kbd>Ctrl+]</kbd></p>\n")]
        [InlineData("<div>Some text here</div>", "<div>Some text here</div>")]
        [InlineData(@"---
a: b
b:
  c: e
---", "<yamlheader start=\"1\" end=\"5\">a: b\nb:\n  c: e</yamlheader>")]
        [InlineData(@"# Hello @CrossLink1 @'CrossLink2'dummy 
@World",
            "<h1 id=\"hello-crosslink1-crosslink2dummy\">Hello <xref href=\"CrossLink1\" data-throw-if-not-resolved=\"False\" data-raw-source=\"@CrossLink1\"></xref> <xref href=\"CrossLink2\" data-throw-if-not-resolved=\"False\" data-raw-source=\"@&#39;CrossLink2&#39;\"></xref>dummy</h1>\n<p><xref href=\"World\" data-throw-if-not-resolved=\"False\" data-raw-source=\"@World\"></xref></p>\n")]
        [InlineData("a\n```\nc\n```",
            "<p>a</p>\n<pre><code>c\n</code></pre>")]
        [InlineData(@" *hello* abc @api__1",
            "<p> <em>hello</em> abc <xref href=\"api__1\" data-throw-if-not-resolved=\"False\" data-raw-source=\"@api__1\"></xref></p>\n")]
        [InlineData("@1abc", "<p>@1abc</p>\n")]
        [InlineData(@"@api1 @api__1 @api!1 @api@a abc@api.com a.b.c@api.com @'a p ';@""a!pi"",@api...@api",
            "<p><xref href=\"api1\" data-throw-if-not-resolved=\"False\" data-raw-source=\"@api1\"></xref> <xref href=\"api__1\" data-throw-if-not-resolved=\"False\" data-raw-source=\"@api__1\"></xref> <xref href=\"api!1\" data-throw-if-not-resolved=\"False\" data-raw-source=\"@api!1\"></xref> <xref href=\"api@a\" data-throw-if-not-resolved=\"False\" data-raw-source=\"@api@a\"></xref> abc@api.com a.b.c@api.com <xref href=\"a p \" data-throw-if-not-resolved=\"False\" data-raw-source=\"@&#39;a p &#39;\"></xref>;<xref href=\"a!pi\" data-throw-if-not-resolved=\"False\" data-raw-source=\"@&quot;a!pi&quot;\"></xref>,<xref href=\"api\" data-throw-if-not-resolved=\"False\" data-raw-source=\"@api\"></xref>...<xref href=\"api\" data-throw-if-not-resolved=\"False\" data-raw-source=\"@api\"></xref></p>\n")]
        [InlineData("[name](xref:uid \"title\")", "<p><a href=\"xref:uid\" title=\"title\" data-raw-source=\"[name](xref:uid &quot;title&quot;)\">name</a></p>\n")]
        [InlineData("<xref:uid>text", "<p><xref href=\"uid\" data-throw-if-not-resolved=\"True\" data-raw-source=\"&lt;xref:uid&gt;\"></xref>text</p>\n")]
        [InlineData("<xref:'uid with space'>text", "<p><xref href=\"uid with space\" data-throw-if-not-resolved=\"True\" data-raw-source=\"&lt;xref:&#39;uid with space&#39;&gt;\"></xref>text</p>\n")]
        [InlineData(
            @"[*a*](xref:uid)",
            "<p><a href=\"xref:uid\" data-raw-source=\"[*a*](xref:uid)\"><em>a</em></a></p>\n")]
        [InlineData(
            @"# <a id=""x""></a>Y",
            @"<h1 id=""x"">Y</h1>
")]
        [InlineData(
            @"# <a name=""x""></a>Y",
            @"<h1 id=""x"">Y</h1>
")]
        #endregion
        public void TestDfmInGeneral(string source, string expected)
        {
            var result = DocfxFlavoredMarked.Markup(source);
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "DfmMarkdown")]
        public void TestTabGroup()
        {
            var options = DocfxFlavoredMarked.CreateDefaultOptions();
            options.ShouldExportSourceInfo = true;
            var actual = DocfxFlavoredMarked.Markup(null, null, options, @"# [title-a](#tab/a)
content-a
# <a id=""x""></a>[title-b](#tab/b/c)
content-b
- - -", "test.md");
            var groupId = "uBn0rykxXo";
            var expected = $@"<div class=""tabGroup"" id=""tabgroup_{groupId}"" sourceFile=""test.md"" sourceStartLineNumber=""1"" sourceEndLineNumber=""5"">
<ul role=""tablist"">
<li role=""presentation"">
<a href=""#tabpanel_{groupId}_a"" role=""tab"" aria-controls=""tabpanel_{groupId}_a"" data-tab=""a"" tabindex=""0"" aria-selected=""true"" sourceFile=""test.md"" sourceStartLineNumber=""1"" sourceEndLineNumber=""1"">title-a</a>
</li>
<li role=""presentation"" aria-hidden=""true"" hidden=""hidden"">
<a href=""#tabpanel_{groupId}_b_c"" role=""tab"" aria-controls=""tabpanel_{groupId}_b_c"" data-tab=""b"" data-condition=""c"" tabindex=""-1"" sourceFile=""test.md"" sourceStartLineNumber=""3"" sourceEndLineNumber=""3"">title-b</a>
</li>
</ul>
<section id=""tabpanel_{groupId}_a"" role=""tabpanel"" data-tab=""a"">
<p sourceFile=""test.md"" sourceStartLineNumber=""2"" sourceEndLineNumber=""2"">content-a</p>
</section>
<section id=""tabpanel_{groupId}_b_c"" role=""tabpanel"" data-tab=""b"" data-condition=""c"" aria-hidden=""true"" hidden=""hidden"">
<p sourceFile=""test.md"" sourceStartLineNumber=""4"" sourceEndLineNumber=""4"">content-b</p>
</section>
</div>
";
            Assert.Equal(expected.Replace("\r\n", "\n"), actual);
        }

        [Fact]
        [Trait("Related", "DfmMarkdown")]
        public void TestTabGroup_2()
        {
            var options = DocfxFlavoredMarked.CreateDefaultOptions();
            options.ShouldExportSourceInfo = true;
            var actual = DocfxFlavoredMarked.Markup(null, null, options, @"# [title-a](#tab/a)
content-a
# [title-b](#tab/b/c)
content-b
- - -
# [title-a](#tab/a)
content-a
# [title-b](#tab/b/a)
content-b
- - -", "test.md");
            var groupId = "uBn0rykxXo";
            var expected = $@"<div class=""tabGroup"" id=""tabgroup_{groupId}"" sourceFile=""test.md"" sourceStartLineNumber=""1"" sourceEndLineNumber=""5"">
<ul role=""tablist"">
<li role=""presentation"">
<a href=""#tabpanel_{groupId}_a"" role=""tab"" aria-controls=""tabpanel_{groupId}_a"" data-tab=""a"" tabindex=""0"" aria-selected=""true"" sourceFile=""test.md"" sourceStartLineNumber=""1"" sourceEndLineNumber=""1"">title-a</a>
</li>
<li role=""presentation"" aria-hidden=""true"" hidden=""hidden"">
<a href=""#tabpanel_{groupId}_b_c"" role=""tab"" aria-controls=""tabpanel_{groupId}_b_c"" data-tab=""b"" data-condition=""c"" tabindex=""-1"" sourceFile=""test.md"" sourceStartLineNumber=""3"" sourceEndLineNumber=""3"">title-b</a>
</li>
</ul>
<section id=""tabpanel_{groupId}_a"" role=""tabpanel"" data-tab=""a"">
<p sourceFile=""test.md"" sourceStartLineNumber=""2"" sourceEndLineNumber=""2"">content-a</p>
</section>
<section id=""tabpanel_{groupId}_b_c"" role=""tabpanel"" data-tab=""b"" data-condition=""c"" aria-hidden=""true"" hidden=""hidden"">
<p sourceFile=""test.md"" sourceStartLineNumber=""4"" sourceEndLineNumber=""4"">content-b</p>
</section>
</div>
<div class=""tabGroup"" id=""tabgroup_{groupId}-1"" sourceFile=""test.md"" sourceStartLineNumber=""6"" sourceEndLineNumber=""10"">
<ul role=""tablist"">
<li role=""presentation"">
<a href=""#tabpanel_{groupId}-1_a"" role=""tab"" aria-controls=""tabpanel_uBn0rykxXo-1_a"" data-tab=""a"" tabindex=""0"" aria-selected=""true"" sourceFile=""test.md"" sourceStartLineNumber=""6"" sourceEndLineNumber=""6"">title-a</a>
</li>
<li role=""presentation"">
<a href=""#tabpanel_{groupId}-1_b_a"" role=""tab"" aria-controls=""tabpanel_uBn0rykxXo-1_b_a"" data-tab=""b"" data-condition=""a"" tabindex=""-1"" sourceFile=""test.md"" sourceStartLineNumber=""8"" sourceEndLineNumber=""8"">title-b</a>
</li>
</ul>
<section id=""tabpanel_{groupId}-1_a"" role=""tabpanel"" data-tab=""a"">
<p sourceFile=""test.md"" sourceStartLineNumber=""7"" sourceEndLineNumber=""7"">content-a</p>
</section>
<section id=""tabpanel_{groupId}-1_b_a"" role=""tabpanel"" data-tab=""b"" data-condition=""a"" aria-hidden=""true"" hidden=""hidden"">
<p sourceFile=""test.md"" sourceStartLineNumber=""9"" sourceEndLineNumber=""9"">content-b</p>
</section>
</div>
";
            Assert.Equal(expected.Replace("\r\n", "\n"), actual);
        }

        [Fact]
        [Trait("Related", "DfmMarkdown")]
        public void TestTabGroup_Combining()
        {
            var options = DocfxFlavoredMarked.CreateDefaultOptions();
            options.ShouldExportSourceInfo = true;
            var actual = DocfxFlavoredMarked.Markup(null, null, options, @"# [title-a or b](#tab/a+b)
content-a or b
# [title-c](#tab/c)
content-c
- - -
# [title-a](#tab/a)
content-a
# [title-b or c](#tab/b+c)
content-b or c
- - -", "test.md");
            var groupId = "uBn0rykxXo";
            var expected = $@"<div class=""tabGroup"" id=""tabgroup_{groupId}"" sourceFile=""test.md"" sourceStartLineNumber=""1"" sourceEndLineNumber=""5"">
<ul role=""tablist"">
<li role=""presentation"">
<a href=""#tabpanel_{groupId}_a+b"" role=""tab"" aria-controls=""tabpanel_{groupId}_a+b"" data-tab=""a+b"" tabindex=""0"" aria-selected=""true"" sourceFile=""test.md"" sourceStartLineNumber=""1"" sourceEndLineNumber=""1"">title-a or b</a>
</li>
<li role=""presentation"">
<a href=""#tabpanel_{groupId}_c"" role=""tab"" aria-controls=""tabpanel_{groupId}_c"" data-tab=""c"" tabindex=""-1"" sourceFile=""test.md"" sourceStartLineNumber=""3"" sourceEndLineNumber=""3"">title-c</a>
</li>
</ul>
<section id=""tabpanel_{groupId}_a+b"" role=""tabpanel"" data-tab=""a+b"">
<p sourceFile=""test.md"" sourceStartLineNumber=""2"" sourceEndLineNumber=""2"">content-a or b</p>
</section>
<section id=""tabpanel_{groupId}_c"" role=""tabpanel"" data-tab=""c"" aria-hidden=""true"" hidden=""hidden"">
<p sourceFile=""test.md"" sourceStartLineNumber=""4"" sourceEndLineNumber=""4"">content-c</p>
</section>
</div>
<div class=""tabGroup"" id=""tabgroup_{groupId}-1"" sourceFile=""test.md"" sourceStartLineNumber=""6"" sourceEndLineNumber=""10"">
<ul role=""tablist"">
<li role=""presentation"">
<a href=""#tabpanel_{groupId}-1_a"" role=""tab"" aria-controls=""tabpanel_uBn0rykxXo-1_a"" data-tab=""a"" tabindex=""0"" aria-selected=""true"" sourceFile=""test.md"" sourceStartLineNumber=""6"" sourceEndLineNumber=""6"">title-a</a>
</li>
<li role=""presentation"">
<a href=""#tabpanel_{groupId}-1_b+c"" role=""tab"" aria-controls=""tabpanel_uBn0rykxXo-1_b+c"" data-tab=""b+c"" tabindex=""-1"" sourceFile=""test.md"" sourceStartLineNumber=""8"" sourceEndLineNumber=""8"">title-b or c</a>
</li>
</ul>
<section id=""tabpanel_{groupId}-1_a"" role=""tabpanel"" data-tab=""a"">
<p sourceFile=""test.md"" sourceStartLineNumber=""7"" sourceEndLineNumber=""7"">content-a</p>
</section>
<section id=""tabpanel_{groupId}-1_b+c"" role=""tabpanel"" data-tab=""b+c"" aria-hidden=""true"" hidden=""hidden"">
<p sourceFile=""test.md"" sourceStartLineNumber=""9"" sourceEndLineNumber=""9"">content-b or c</p>
</section>
</div>
";
            Assert.Equal(expected.Replace("\r\n", "\n"), actual);
        }

        [Fact]
        [Trait("Related", "DfmMarkdown")]
        public void TestCodeFenceWithSpaceInFileName()
        {
            var source = @"  [!code-csharp  [  Test Space  ] ( test space in\) link.cs#abc ""title test"" ) ]  ";
            var result = DocfxFlavoredMarked.Markup(source, "a.md");
            Assert.Equal("<!-- Can not find reference test space in) link.cs -->\n", result);
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

            var marked = DocfxFlavoredMarked.Markup(root);
            Assert.Equal(expected.Replace("\r\n", "\n"), marked);
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

            var marked = DocfxFlavoredMarked.Markup(root);
            Assert.Equal(expected.Replace("\r\n", "\n"), marked);
        }

        [Fact]
        [Trait("Related", "DfmMarkdown")]
        public void TestDfmVideo_MixWithNote()
        {
            // 1. Prepare data
            var root = @"The following is video mixed with note.
> [!Video https://sec.ch9.ms/ch9/4393/7d7c7df7-3f15-4a65-a2f7-3e4d0bea4393/Episode208_mid.mp4]
> [!NOTE]
> this is note text
> [!Video https://sec.ch9.ms/ch9/4393/7d7c7df7-3f15-4a65-a2f7-3e4d0bea4393/Episode208_mid.mp4]";

            var expected = @"<p>The following is video mixed with note.</p>
<div class=""embeddedvideo""><iframe src=""https://sec.ch9.ms/ch9/4393/7d7c7df7-3f15-4a65-a2f7-3e4d0bea4393/Episode208_mid.mp4"" frameborder=""0"" allowfullscreen=""true""></iframe></div>
<div class=""NOTE""><h5>NOTE</h5><p>this is note text</p>
</div>
<div class=""embeddedvideo""><iframe src=""https://sec.ch9.ms/ch9/4393/7d7c7df7-3f15-4a65-a2f7-3e4d0bea4393/Episode208_mid.mp4"" frameborder=""0"" allowfullscreen=""true""></iframe></div>
";

            var marked = DocfxFlavoredMarked.Markup(root);
            Assert.Equal(expected.Replace("\r\n", "\n"), marked);
        }

        [Fact]
        [Trait("Related", "DfmMarkdown")]
        public void TestYaml_InvalidYamlInsideContent()
        {
            var source = @"# Title
---
Not yaml syntax
---
hello world";
            var expected = @"<h1 id=""title"">Title</h1>
<hr/>
<h2 id=""not-yaml-syntax"">Not yaml syntax</h2>
<p>hello world</p>
";
            var marked = DocfxFlavoredMarked.Markup(source);
            Assert.Equal(expected.Replace("\r\n", "\n"), marked);
        }

        [Fact]
        [Trait("Related", "DfmMarkdown")]
        public void TestDfmNote_NoteWithTextFollow()
        {
            var source = @"# Note not in one line
> [!NOTE]hello
> world
> [!WARNING]     Hello world
this is also warning";
            var expected = @"<h1 id=""note-not-in-one-line"">Note not in one line</h1>
<div class=""NOTE""><h5>NOTE</h5><p>hello
world</p>
</div>
<div class=""WARNING""><h5>WARNING</h5><p>Hello world
this is also warning</p>
</div>
";
            var marked = DocfxFlavoredMarked.Markup(source);
            Assert.Equal(expected.Replace("\r\n", "\n"), marked);
        }

        [Fact]
        [Trait("Related", "DfmMarkdown")]
        public void TestDfmNote_NoteWithLocalization()
        {
            var source = @"# Note not in one line
> [!NOTE]hello
> world
> [!WARNING]     Hello world
this is also warning";
            var expected = @"<h1 id=""note-not-in-one-line"">Note not in one line</h1>
<div class=""NOTE""><h5>注意</h5><p>hello
world</p>
</div>
<div class=""WARNING""><h5>警告</h5><p>Hello world
this is also warning</p>
</div>
";
            var marked = DocfxFlavoredMarked.Markup(source,
                null,
                new Dictionary<string, string>
                {
                    {"note", "<h5>注意</h5>"},
                    {"warning", "<h5>警告</h5>" }
                }.ToImmutableDictionary());
            Assert.Equal(expected.Replace("\r\n", "\n"), marked);
        }

        [Fact]
        [Trait("Related", "DfmMarkdown")]
        public void TestCode_ParentFolderNotExist()
        {
            var source = @"[!code-cs[not exist](not_exist_folder/file.cs)]";
            var expected = "<!-- Can not find reference not_exist_folder/file.cs -->\n";
            var marked = DocfxFlavoredMarked.Markup(source, "parent");
            Assert.Equal(expected.Replace("\r\n", "\n"), marked);
        }

        [Fact]
        [Trait("Related", "DfmMarkdown")]
        public void TestDfmNote_NoteWithMix()
        {
            var source = @"# Note not in one line
> [!NOTE]
> hello
> world
> [!WARNING] Hello world
> [!WARNING]  Hello world this is also warning
> [!WARNING]
> Hello world this is also warning
> [!IMPORTANT]
> Hello world this IMPORTANT";
            var expected = @"<h1 id=""note-not-in-one-line"">Note not in one line</h1>
<div class=""NOTE""><h5>NOTE</h5><p>hello
world</p>
</div>
<div class=""WARNING""><h5>WARNING</h5><p>Hello world</p>
</div>
<div class=""WARNING""><h5>WARNING</h5><p>Hello world this is also warning</p>
</div>
<div class=""WARNING""><h5>WARNING</h5><p>Hello world this is also warning</p>
</div>
<div class=""IMPORTANT""><h5>IMPORTANT</h5><p>Hello world this IMPORTANT</p>
</div>
";
            var marked = DocfxFlavoredMarked.Markup(source);
            Assert.Equal(expected.Replace("\r\n", "\n"), marked);
        }

        [Fact]
        [Trait("Related", "DfmMarkdown")]
        public void TestDfmYamlHeader_YamlUtilityReturnNull()
        {
            var source = @"---

### /Unconfigure

---";
            var expected = @"<hr/>
<h3 id=""unconfigure"">/Unconfigure</h3>
<hr/>
";
            var marked = DocfxFlavoredMarked.Markup(source);
            Assert.Equal(expected.Replace("\r\n", "\n"), marked);
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
<div class=""NOTE""><h5>NOTE</h5><p>note text 1-1
note text 1-2<br/>note text 2-1
This is also note<br/>This is also note with br</p>
</div>
<p>Skip the note</p>
")]
        [InlineData(@"the following is not note type
  > no-note text 1-1
  > [!NOTE]
  > no-note text 1-2  
  > no-note text 2-1
", @"<p>the following is not note type</p>
<blockquote><p>no-note text 1-1</p>
</blockquote>
<div class=""NOTE""><h5>NOTE</h5><p>no-note text 1-2<br/>no-note text 2-1</p>
</div>
")]
        [InlineData(@"the following is not note type
  > no-note text 1-1
  >
  > [!NOTE]
  > no-note text 2-1  
  > no-note text 2-2
", @"<p>the following is not note type</p>
<blockquote><p>no-note text 1-1</p>
</blockquote>
<div class=""NOTE""><h5>NOTE</h5><p>no-note text 2-1<br/>no-note text 2-2</p>
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
</code></pre>")]
        #endregion
        public void TestSectionNoteInBlockQuote(string source, string expected)
        {
            var markedContent = DocfxFlavoredMarked.Markup(source);
            Assert.Equal(expected.Replace("\r\n", "\n"), markedContent);
        }

        [Theory]
        [Trait("Related", "DfmMarkdown")]
        [InlineData(@"> [!div class=""All"" id=""All""] Followed text
> We should support that.")]
        public void TestSectionWithTextFollowed(string source)
        {
            var markedContent = DocfxFlavoredMarked.Markup(source);
            Assert.Equal("<div class=\"All\" id=\"All\"><p>Followed text\nWe should support that.</p>\n</div>\n", markedContent);
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
            var markedContent = DocfxFlavoredMarked.Markup(source);
            Assert.Equal("<div class=\"All\" id=\"All\"><p>this is out all</p>\n<div class=\"A\" id=\"A\"><p>this is A</p>\n</div>\n<div class=\"B\" id=\"B\"><p>this is B</p>\n</div>\n</div>\n", markedContent);
        }

        [Theory]
        [Trait("Related", "DfmMarkdown")]
        [InlineData(@"> [!div class=""tabbedCodeSnippets"" data-resources=""OutlookServices.Calendar""]
>
>```cs-i
    var outlookClient = await CreateOutlookClientAsync(""Calendar"");
    var events = await outlookClient.Me.Events.Take(10).ExecuteAsync();
            foreach (var calendarEvent in events.CurrentPage)
            {
                System.Diagnostics.Debug.WriteLine(""Event '{0}'."", calendarEvent.Subject);
            }
```
> 
>```javascript-i
outlookClient.me.events.getEvents().fetch().then(function(result) {
        result.currentPage.forEach(function(event) {
        console.log('Event ""' + event.subject + '""')
    });
}, function(error)
    {
        console.log(error);
    });
```")]
        public void TestSectionBlockLevel(string source)
        {
            var content = DocfxFlavoredMarked.Markup(source);

            // assert
            XmlDocument xdoc = new XmlDocument();
            xdoc.LoadXml(content);
            var tabbedCodeNode = xdoc.SelectSingleNode("//div[@class='tabbedCodeSnippets' and @data-resources='OutlookServices.Calendar']");
            Assert.True(tabbedCodeNode != null);
            var csNode = tabbedCodeNode.SelectSingleNode("./pre/code[@class='lang-cs-i']");
            Assert.True(csNode != null);
            var jsNode = tabbedCodeNode.SelectSingleNode("./pre/code[@class='lang-javascript-i']");
            Assert.True(jsNode != null);
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
            var content = DocfxFlavoredMarked.Markup(source);
            Assert.Equal("<blockquote><p>this is blockquote</p>\n<p>this line is also in the before blockquote</p>\n</blockquote>\n<div class=\"NOTE\"><h5>NOTE</h5><p>This is note text</p>\n</div>\n<div class=\"WARNING\"><h5>WARNING</h5><p>This is warning text</p>\n</div>\n<div class=\"a\" id=\"diva\"><p>this is div with class a and id diva\ntext also in div</p>\n</div>\n<div class=\"b\" cause=\"divb\"><p>this is div with class b and cause divb</p>\n</div>\n<div class=\"IMPORTANT\"><h5>IMPORTANT</h5><p>This is imoprtant text follow div</p>\n</div>\n", content);
        }

        [Theory]
        [Trait("Related", "DfmMarkdown")]
        [InlineData(@"> [!div]", "<div></div>\n")]
        [InlineData(@"> [!div `id=""error""]", "<div></div>\n")]
        [InlineData(@"> [!div `id=""right""`]", "<div id=\"right\"></div>\n")]
        public void TestSectionCornerCase(string source, string expected)
        {
            var content = DocfxFlavoredMarked.Markup(source);
            Assert.Equal(expected, content);
        }

        [Fact]
        [Trait("Related", "DfmMarkdown")]
        public void TestSection_AzureSingleSelector()
        {
            var source = @"> [!div class=""op_single_selector""]
> * [Universal Windows](../articles/notification-hubs-windows-store-dotnet-get-started/.md)
> * [Windows Phone](../articles/notification-hubs-windows-phone-get-started/.md)
> * [iOS](../articles/notification-hubs-ios-get-started/.md)
> * [Android](../articles/notification-hubs-android-get-started/.md)
> * [Kindle](../articles/notification-hubs-kindle-get-started/.md)
> * [Baidu](../articles/notification-hubs-baidu-get-started/.md)
> * [Xamarin.iOS](../articles/partner-xamarin-notification-hubs-ios-get-started/.md)
> * [Xamarin.Android](../articles/partner-xamarin-notification-hubs-android-get-started/.md)
> 
> 
";
            var expected = @"<div class=""op_single_selector""><ul>
<li><a href=""../articles/notification-hubs-windows-store-dotnet-get-started/.md"" data-raw-source=""[Universal Windows](../articles/notification-hubs-windows-store-dotnet-get-started/.md)"">Universal Windows</a></li>
<li><a href=""../articles/notification-hubs-windows-phone-get-started/.md"" data-raw-source=""[Windows Phone](../articles/notification-hubs-windows-phone-get-started/.md)"">Windows Phone</a></li>
<li><a href=""../articles/notification-hubs-ios-get-started/.md"" data-raw-source=""[iOS](../articles/notification-hubs-ios-get-started/.md)"">iOS</a></li>
<li><a href=""../articles/notification-hubs-android-get-started/.md"" data-raw-source=""[Android](../articles/notification-hubs-android-get-started/.md)"">Android</a></li>
<li><a href=""../articles/notification-hubs-kindle-get-started/.md"" data-raw-source=""[Kindle](../articles/notification-hubs-kindle-get-started/.md)"">Kindle</a></li>
<li><a href=""../articles/notification-hubs-baidu-get-started/.md"" data-raw-source=""[Baidu](../articles/notification-hubs-baidu-get-started/.md)"">Baidu</a></li>
<li><a href=""../articles/partner-xamarin-notification-hubs-ios-get-started/.md"" data-raw-source=""[Xamarin.iOS](../articles/partner-xamarin-notification-hubs-ios-get-started/.md)"">Xamarin.iOS</a></li>
<li><a href=""../articles/partner-xamarin-notification-hubs-android-get-started/.md"" data-raw-source=""[Xamarin.Android](../articles/partner-xamarin-notification-hubs-android-get-started/.md)"">Xamarin.Android</a></li>
</ul>
</div>
";
            var marked = DocfxFlavoredMarked.Markup(source);
            Assert.Equal(expected.Replace("\r\n", "\n"), marked);
        }

        [Fact]
        [Trait("Related", "DfmMarkdown")]
        public void TestSection_AzureMultiSelector()
        {
            var source = @"> [!div class=""op_multi_selector"" title1=""Platform"" title2=""Backend""]
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
> 
";
            var expected = @"<div class=""op_multi_selector"" title1=""Platform"" title2=""Backend""><ul>
<li><a href=""./mobile-services-dotnet-backend-ios-get-started-push.md"" data-raw-source=""[(iOS | .NET)](./mobile-services-dotnet-backend-ios-get-started-push.md)"">(iOS | .NET)</a></li>
<li><a href=""./mobile-services-javascript-backend-ios-get-started-push.md"" data-raw-source=""[(iOS | JavaScript)](./mobile-services-javascript-backend-ios-get-started-push.md)"">(iOS | JavaScript)</a></li>
<li><a href=""./mobile-services-dotnet-backend-windows-universal-dotnet-get-started-push.md"" data-raw-source=""[(Windows universal C# | .NET)](./mobile-services-dotnet-backend-windows-universal-dotnet-get-started-push.md)"">(Windows universal C# | .NET)</a></li>
<li><a href=""./mobile-services-javascript-backend-windows-universal-dotnet-get-started-push.md"" data-raw-source=""[(Windows universal C# | Javascript)](./mobile-services-javascript-backend-windows-universal-dotnet-get-started-push.md)"">(Windows universal C# | Javascript)</a></li>
<li><a href=""./mobile-services-dotnet-backend-windows-phone-get-started-push.md"" data-raw-source=""[(Windows Phone | .NET)](./mobile-services-dotnet-backend-windows-phone-get-started-push.md)"">(Windows Phone | .NET)</a></li>
<li><a href=""./mobile-services-javascript-backend-windows-phone-get-started-push.md"" data-raw-source=""[(Windows Phone | Javascript)](./mobile-services-javascript-backend-windows-phone-get-started-push.md)"">(Windows Phone | Javascript)</a></li>
<li><a href=""./mobile-services-dotnet-backend-android-get-started-push.md"" data-raw-source=""[(Android | .NET)](./mobile-services-dotnet-backend-android-get-started-push.md)"">(Android | .NET)</a></li>
<li><a href=""./mobile-services-javascript-backend-android-get-started-push.md"" data-raw-source=""[(Android | Javascript)](./mobile-services-javascript-backend-android-get-started-push.md)"">(Android | Javascript)</a></li>
<li><a href=""./partner-xamarin-mobile-services-ios-get-started-push.md"" data-raw-source=""[(Xamarin iOS | Javascript)](./partner-xamarin-mobile-services-ios-get-started-push.md)"">(Xamarin iOS | Javascript)</a></li>
<li><a href=""./partner-xamarin-mobile-services-android-get-started-push.md"" data-raw-source=""[(Xamarin Android | Javascript)](./partner-xamarin-mobile-services-android-get-started-push.md)"">(Xamarin Android | Javascript)</a></li>
</ul>
</div>
";
            var marked = DocfxFlavoredMarked.Markup(source);
            Assert.Equal(expected.Replace("\r\n", "\n"), marked);
        }

        [Fact]
        [Trait("Related", "DfmMarkdown")]
        public void TestDfm_EncodeInStrongEM()
        {
            var source = @"tag started with non-alphabet should be encoded <1-100>, <_hello>, <?world>, <1_2 href=""good"">, <1 att='bcd'>, <a?world> <a_b href=""good"">.
tag started with alphabet should not be encode: <abc> <a-hello> <AC att='bcd'>";

            var expected = @"<p>tag started with non-alphabet should be encoded &lt;1-100&gt;, &lt;_hello&gt;, &lt;?world&gt;, &lt;1_2 href=&quot;good&quot;&gt;, &lt;1 att=&#39;bcd&#39;&gt;, &lt;a?world&gt; &lt;a_b href=&quot;good&quot;&gt;.
tag started with alphabet should not be encode: <abc> <a-hello> <AC att='bcd'></p>
";
            var marked = DocfxFlavoredMarked.Markup(source);
            Assert.Equal(expected.Replace("\r\n", "\n"), marked);
        }

        [Fact]
        [Trait("Related", "DfmMarkdown")]
        public void TestDfmImageLink_WithSpecialCharactorsInAltText()
        {
            var source = @"![This is image alt text with quotation ' and double quotation ""hello"" world](girl.png)";

            var expected = @"<p><img src=""girl.png"" alt=""This is image alt text with quotation &#39; and double quotation &quot;hello&quot; world""/></p>
";
            var marked = DocfxFlavoredMarked.Markup(source);
            Assert.Equal(expected.Replace("\r\n", "\n"), marked);
        }

        [Fact]
        [Trait("Related", "DfmMarkdown")]
        [Trait("A wrong case need to be fixed in dfm", "' in title should be traslated to &#39; instead of &amp;#39;")]
        public void TestDfmLink_LinkWithSpecialCharactorsInTitle()
        {
            var source = @"[text's string](https://www.google.com.sg/?gfe_rd=cr&ei=Xk ""Google's homepage"")";
            var expected = @"<p><a href=""https://www.google.com.sg/?gfe_rd=cr&amp;ei=Xk"" title=""Google&#39;s homepage"" data-raw-source=""[text&#39;s string](https://www.google.com.sg/?gfe_rd=cr&amp;ei=Xk &quot;Google&#39;s homepage&quot;)"">text&#39;s string</a></p>
";
            var marked = DocfxFlavoredMarked.Markup(source);
            Assert.Equal(expected.Replace("\r\n", "\n"), marked);
        }

        [Fact]
        [Trait("Related", "DfmMarkdown")]
        public void TestDfmLink_WithSpecialCharactorsInTitle()
        {
            var source = @"[This is link text with quotation ' and double quotation ""hello"" world](girl.png ""title is ""hello"" world."")";

            var expected = @"<p><a href=""girl.png"" title=""title is &quot;hello&quot; world."" data-raw-source=""[This is link text with quotation &#39; and double quotation &quot;hello&quot; world](girl.png &quot;title is &quot;hello&quot; world.&quot;)"">This is link text with quotation &#39; and double quotation &quot;hello&quot; world</a></p>
";
            var marked = DocfxFlavoredMarked.Markup(source);
            Assert.Equal(expected.Replace("\r\n", "\n"), marked);
        }

        [Fact]
        [Trait("Related", "DfmMarkdown")]
        public void TestPathUtility_AbsoluteLinkWithBracketAndBracket()
        {
            var source = @"[User-Defined Date/Time Formats (Format Function)](http://msdn2.microsoft.com/library/73ctwf33\(VS.90\).aspx)";
            var expected = @"<p><a href=""http://msdn2.microsoft.com/library/73ctwf33(VS.90).aspx"" data-raw-source=""[User-Defined Date/Time Formats (Format Function)](http://msdn2.microsoft.com/library/73ctwf33\(VS.90\).aspx)"">User-Defined Date/Time Formats (Format Function)</a></p>
";
            var marked = DocfxFlavoredMarked.Markup(source);
            Assert.Equal(expected.Replace("\r\n", "\n"), marked);
        }

        [Fact]
        [Trait("Related", "DfmMarkdown")]
        public void TestTable_RegexPerfWithUselessTableHeaderAndUselessTableRow()
        {
            var source = @"ID | Category                                                                                                             | ER  | Addresses                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                             | Ports            
-- | -------------------------------------------------------------------------------------------------------------------- | --- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -----------------
3  | Default<BR>Required                                                                                                  | No  | `r1.res.office365.com r3.res.office365.com r4.res.office365.com xsi.outlook.com`                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                      | **TCP:** 443, 80 
";
            var expected = @"<table>
<thead>
<tr>
<th>ID</th>
<th>Category</th>
<th>ER</th>
<th>Addresses</th>
<th>Ports</th>
</tr>
</thead>
<tbody>
<tr>
<td>3</td>
<td>Default<BR>Required</td>
<td>No</td>
<td><code>r1.res.office365.com r3.res.office365.com r4.res.office365.com xsi.outlook.com</code></td>
<td><strong>TCP:</strong> 443, 80</td>
</tr>
</tbody>
</table>
";
            var marked = DocfxFlavoredMarked.Markup(source);
            Assert.Equal(expected.Replace("\r\n", "\n"), marked);
        }
    }
}
