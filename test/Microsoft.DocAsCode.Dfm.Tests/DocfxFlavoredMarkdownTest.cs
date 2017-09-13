// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Composition.Hosting;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;
    using System.Xml;

    using Xunit;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Build.Engine;
    using Microsoft.DocAsCode.Dfm;
    using Microsoft.DocAsCode.Dfm.MarkdownValidators;
    using Microsoft.DocAsCode.MarkdownLite;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Tests.Common;
    using Microsoft.DocAsCode.Common.Git;

    public class DocfxFlavoredMarkdownTest
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
            Assert.Equal(expected.Replace("\r\n", "\n"), DocfxFlavoredMarked.Markup(source));
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
        public void TestCodeFenceWithSpaceInFileName()
        {
            var source = @"  [!code-csharp  [  Test Space  ] ( test space in\) link.cs#abc ""title test"" ) ]  ";
            var result = DocfxFlavoredMarked.Markup(source, "a.md");
            Assert.Equal("<!-- Can not find reference test space in) link.cs -->\n", result);
        }

        [Fact]
        [Trait("Related", "DfmMarkdown")]
        public void TestBlockLevelInclusion()
        {
            // -r
            //  |- root.md
            //  |- empty.md
            //  |- a
            //  |  |- refc.md
            //  |- b
            //  |  |- linkAndRefRoot.md
            //  |  |- a.md
            //  |  |- img
            //  |  |   |- img.jpg
            //  |- c
            //  |  |- c.md
            //  |- link
            //     |- link2.md
            //     |- md
            //         |- c.md
            var root = @"
[!include[linkAndRefRoot](b/linkAndRefRoot.md)]
[!include[refc](a/refc.md ""This is root"")]
[!include[refc_using_cache](a/refc.md)]
[!include[empty](empty.md)]
[!include[external](http://microsoft.com/a.md)]";

            var linkAndRefRoot = @"
Paragraph1
[link](a.md)
[!include-[link2](../link/link2.md)]
![Image](img/img.jpg)
[!include-[root](../root.md)]";
            var link2 = @"[link](md/c.md)";
            var refc = @"[!include[c](../c/c.md ""This is root"")]";
            var c = @"**Hello**";
            WriteToFile("r/root.md", root);

            WriteToFile("r/a/refc.md", refc);
            WriteToFile("r/b/linkAndRefRoot.md", linkAndRefRoot);
            WriteToFile("r/link/link2.md", link2);
            WriteToFile("r/c/c.md", c);
            WriteToFile("r/empty.md", string.Empty);
            var dependency = new HashSet<string>();
            var marked = DocfxFlavoredMarked.Markup(root, "r/root.md", dependency: dependency);
            Assert.Equal(@"<p>Paragraph1
<a href=""~/r/b/a.md"" data-raw-source=""[link](a.md)"">link</a>
<a href=""~/r/link/md/c.md"" data-raw-source=""[link](md/c.md)"">link</a>
<img src=""~/r/b/img/img.jpg"" alt=""Image"">
<!-- BEGIN ERROR INCLUDE: Unable to resolve [!include-[root](../root.md)]: Circular dependency found in &quot;r/b/linkAndRefRoot.md&quot; -->[!include-[root](../root.md)]<!--END ERROR INCLUDE --></p>
<p><strong>Hello</strong></p>
<p><strong>Hello</strong></p>
<!-- BEGIN ERROR INCLUDE: Unable to resolve [!include[external](http://microsoft.com/a.md)]: Absolute path &quot;http://microsoft.com/a.md&quot; is not supported. -->[!include[external](http://microsoft.com/a.md)]<!--END ERROR INCLUDE -->".Replace("\r\n", "\n"), marked);
            Assert.Equal(
                new[]
                {
                    "a/refc.md",
                    "b/linkAndRefRoot.md",
                    "c/c.md",
                    "empty.md",
                    "link/link2.md",
                    "root.md",
                },
                dependency.OrderBy(x => x));
        }

        [Fact]
        [Trait("Related", "DfmMarkdown")]
        public void TestBlockLevelInclusionWithWorkingFolder()
        {
            // -r
            //  |- root.md
            //  |- b
            //  |  |- linkAndRefRoot.md
            var root = @"[!include[linkAndRefRoot](~/r/b/linkAndRefRoot.md)]";
            var linkAndRefRoot = @"Paragraph1";
            WriteToFile("r/root.md", root);
            WriteToFile("r/b/linkAndRefRoot.md", linkAndRefRoot);
            var marked = DocfxFlavoredMarked.Markup(root, "r/root.md");
            var expected = @"<p>Paragraph1</p>" + "\n";
            Assert.Equal(expected, marked);
        }

        [Fact]
        [Trait("Related", "DfmMarkdown")]
        public void TestBlockLevelInclusionWithSameFile()
        {
            // -r
            //  |- r.md
            //  |- a
            //  |  |- a.md
            //  |- b
            //  |  |- token.md
            //  |- c
            //     |- d
            //        |- d.md
            //  |- img
            //  |  |- img.jpg
            var r = @"
[!include[](a/a.md)]
[!include[](c/d/d.md)]
";
            var a = @"
[!include[](../b/token.md)]";
            var token = @"
![](../img/img.jpg)
[](#anchor)
[a](../a/a.md)
[](invalid.md)
[d](../c/d/d.md#anchor)
";
            var d = @"
[!include[](../../b/token.md)]";
            WriteToFile("r/r.md", r);
            WriteToFile("r/a/a.md", a);
            WriteToFile("r/b/token.md", token);
            WriteToFile("r/c/d/d.md", d);
            var dependency = new HashSet<string>();
            var marked = DocfxFlavoredMarked.Markup(a, "r/a/a.md", dependency: dependency);
            var expected = @"<p><img src=""~/r/img/img.jpg"" alt="""">
<a href=""#anchor"" data-raw-source=""[](#anchor)""></a>
<a href=""~/r/a/a.md"" data-raw-source=""[a](../a/a.md)"">a</a>
<a href=""~/r/b/invalid.md"" data-raw-source=""[](invalid.md)""></a>
<a href=""~/r/c/d/d.md#anchor"" data-raw-source=""[d](../c/d/d.md#anchor)"">d</a></p>".Replace("\r\n", "\n") + "\n";
            Assert.Equal(expected, marked);
            Assert.Equal(
                new[] { "../b/token.md" },
                dependency.OrderBy(x => x));

            dependency.Clear();
            marked = DocfxFlavoredMarked.Markup(d, "r/c/d/d.md", dependency: dependency);
            Assert.Equal(expected, marked);
            Assert.Equal(
                new[] { "../../b/token.md" },
                dependency.OrderBy(x => x));

            dependency.Clear();
            marked = DocfxFlavoredMarked.Markup(r, "r/r.md", dependency: dependency);
            Assert.Equal($@"{expected}{expected}", marked);
            Assert.Equal(
                new[] { "a/a.md", "b/token.md", "c/d/d.md" },
                dependency.OrderBy(x => x));
        }

        [Fact]
        [Trait("Related", "DfmMarkdown")]
        public void TestInclusion_InlineLevel()
        {
            // 1. Prepare data
            var root = @"
Inline [!include[ref1](ref1.md ""This is root"")]
Inline [!include[ref3](ref3.md ""This is root"")]
";

            var ref1 = @"[!include[ref2](ref2.md ""This is root"")]";
            var ref2 = @"## Inline inclusion do not parse header [!include[root](root.md ""This is root"")]";
            var ref3 = @"**Hello**  ";
            File.WriteAllText("root.md", root);
            File.WriteAllText("ref1.md", ref1);
            File.WriteAllText("ref2.md", ref2);
            File.WriteAllText("ref3.md", ref3);

            var dependency = new HashSet<string>();
            var marked = DocfxFlavoredMarked.Markup(root, "root.md", dependency: dependency);
            Assert.Equal("<p>Inline ## Inline inclusion do not parse header <!-- BEGIN ERROR INCLUDE: Unable to resolve [!include[root](root.md &quot;This is root&quot;)]: Circular dependency found in &quot;ref2.md&quot; -->[!include[root](root.md &quot;This is root&quot;)]<!--END ERROR INCLUDE -->\nInline <strong>Hello</strong></p>\n", marked);
            Assert.Equal(
                new[] { "ref1.md", "ref2.md", "ref3.md", "root.md" },
                dependency.OrderBy(x => x));
        }

        [Fact]
        [Trait("Related", "DfmMarkdown")]
        public void TestInInlineInclusionMarkupFromContent()
        {
            var reference = @"---
uid: reference.md
---
## Inline inclusion do not parse header

[link](testLink.md)";

            var expected = @"## Inline inclusion do not parse header

<a href=""testLink.md"" data-raw-source=""[link](testLink.md)"" sourceFile=""reference.md"" sourceStartLineNumber=""6"" sourceEndLineNumber=""6"">link</a>";

            DfmServiceProvider provider = new DfmServiceProvider();
            var service = provider.CreateMarkdownService(new MarkdownServiceParameters());

            var parents = ImmutableStack.Create("reference.md");

            var dfmservice = (DfmServiceProvider.DfmService)service;
            var marked = dfmservice
                .Builder
                .CreateDfmEngine(dfmservice.Renderer)
                .Markup(reference,
                    ((MarkdownBlockContext)dfmservice.Builder.CreateParseContext())
                    .GetInlineContext().SetFilePathStack(parents).SetIsInclude());

            Assert.Equal(expected.Replace("\r\n", "\n"), marked);
        }

        [Fact]
        [Trait("Related", "DfmMarkdown")]
        public void TestInBlockInclusionMarkupFromContent()
        {
            var reference = @"---
uid: reference.md
---
## Block inclusion should parse header

[link](testLink.md)";

            var expected = @"<h2 id=""block-inclusion-should-parse-header"" sourceFile=""reference.md"" sourceStartLineNumber=""4"" sourceEndLineNumber=""4"">Block inclusion should parse header</h2>
<p sourceFile=""reference.md"" sourceStartLineNumber=""6"" sourceEndLineNumber=""6""><a href=""testLink.md"" data-raw-source=""[link](testLink.md)"" sourceFile=""reference.md"" sourceStartLineNumber=""6"" sourceEndLineNumber=""6"">link</a></p>
";

            DfmServiceProvider provider = new DfmServiceProvider();
            var service = provider.CreateMarkdownService(new MarkdownServiceParameters());

            var parents = ImmutableStack.Create("reference.md");

            var dfmservice = (DfmServiceProvider.DfmService)service;
            var marked = dfmservice
                .Builder
                .CreateDfmEngine(dfmservice.Renderer)
                .Markup(reference,
                    ((MarkdownBlockContext)dfmservice.Builder.CreateParseContext())
                    .SetFilePathStack(parents).SetIsInclude());

            Assert.Equal(expected.Replace("\r\n", "\n"), marked);
        }

        [Fact]
        [Trait("Related", "DfmMarkdown")]
        public void TestBlockInclude_ShouldExcludeBracketInRegex()
        {
            // 1. Prepare data
            var root = @"[!INCLUDE [azure-probe-intro-include](inc1.md)].

[!INCLUDE [azure-arm-classic-important-include](inc2.md)] [Resource Manager model](inc1.md).


[!INCLUDE [azure-ps-prerequisites-include.md](inc3.md)]";

            var expected = @"<p>inc1.</p>
<p>inc2 <a href=""inc1.md"" data-raw-source=""[Resource Manager model](inc1.md)"">Resource Manager model</a>.</p>
<p>inc3</p>
";

            var inc1 = @"inc1";
            var inc2 = @"inc2";
            var inc3 = @"inc3";
            File.WriteAllText("root.md", root);
            File.WriteAllText("inc1.md", inc1);
            File.WriteAllText("inc2.md", inc2);
            File.WriteAllText("inc3.md", inc3);

            var dependency = new HashSet<string>();
            var marked = DocfxFlavoredMarked.Markup(root, "root.md", dependency: dependency);
            Assert.Equal(expected.Replace("\r\n", "\n"), marked);
            Assert.Equal(
              new[] { "inc1.md", "inc2.md", "inc3.md" },
              dependency.OrderBy(x => x));
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

        [Fact]
        [Trait("Related", "DfmMarkdown")]
        public void TestDfm_InvalidYamlHeader_YamlUtilityThrowException()
        {
            var source = @"---
- Jon Schlinkert
- Brian Woodward

---";
            var expected = @"<hr/>
<ul>
<li>Jon Schlinkert</li>
<li>Brian Woodward</li>
</ul>
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
        public void TestPathUtility_AbsoluteLinkWithBracketAndBrackt()
        {
            var source = @"[User-Defined Date/Time Formats (Format Function)](http://msdn2.microsoft.com/library/73ctwf33\(VS.90\).aspx)";
            var expected = @"<p><a href=""http://msdn2.microsoft.com/library/73ctwf33(VS.90).aspx"" data-raw-source=""[User-Defined Date/Time Formats (Format Function)](http://msdn2.microsoft.com/library/73ctwf33\(VS.90\).aspx)"">User-Defined Date/Time Formats (Format Function)</a></p>
";
            var marked = DocfxFlavoredMarked.Markup(source);
            Assert.Equal(expected.Replace("\r\n", "\n"), marked);
        }

        [Fact]
        [Trait("Related", "DfmMarkdown")]
        public void TestDfmTagValidate()
        {
            var builder = new DfmEngineBuilder(new Options() { Mangle = false });
            var mrb = new MarkdownValidatorBuilder(
                new CompositionContainer(
                    new ContainerConfiguration()
                        .WithAssembly(typeof(DocfxFlavoredMarkdownTest).Assembly)
                        .CreateContainer()));
            mrb.AddTagValidators(new[]
            {
                new MarkdownTagValidationRule
                {
                    TagNames = new List<string> { "em", "div" },
                    MessageFormatter = "Invalid tag({0})!",
                    Behavior = TagValidationBehavior.Error,
                    OpeningTagOnly = true,
                },
                new MarkdownTagValidationRule
                {
                    TagNames = new List<string> { "h1" },
                    MessageFormatter = "Warning tag({0})!",
                    Behavior = TagValidationBehavior.Warning,
                },
                new MarkdownTagValidationRule
                {
                    TagNames = new List<string> { "script" },
                    MessageFormatter = "Warning tag({0})!",
                    Behavior = TagValidationBehavior.Warning,
                },
                new MarkdownTagValidationRule
                {
                    TagNames = new List<string> { "pre" },
                    MessageFormatter = "Warning tag({0})!",
                    Behavior = TagValidationBehavior.Warning,
                },
            });
            mrb.AddValidators(new[]
            {
                new MarkdownValidationRule
                {
                    ContractName =  HtmlMarkdownTokenValidatorProvider.ContractName,
                }
            });
            builder.Rewriter = mrb.CreateRewriter();

            var engine = builder.CreateDfmEngine(new DfmRenderer());
            var listener = TestLoggerListener.CreateLoggerListenerWithPhaseEqualFilter("test!!!!" + "." + MarkdownValidatorBuilder.MarkdownValidatePhaseName);
            Logger.RegisterListener(listener);
            string result;
            using (new LoggerPhaseScope("test!!!!"))
            {
                result = engine.Markup(@"<div><i>x</i><EM>y</EM><h1>z<pre><code>a*b*c</code></pre></h1></div>

<script>alert(1);</script>", "test");
            }
            Logger.UnregisterListener(listener);
            Assert.Equal(@"<div><i>x</i><EM>y</EM><h1>z<pre><code>a*b*c</code></pre></h1></div>

<script>alert(1);</script>".Replace("\r\n", "\n"), result);
            Assert.Equal(8, listener.Items.Count);
            Assert.Equal(new[]
            {
                HtmlMarkdownTokenValidatorProvider.WarningMessage,
                "Invalid tag(div)!",
                "Invalid tag(EM)!",
                "Warning tag(h1)!",
                "Warning tag(pre)!",
                "Warning tag(h1)!",
                "Html Tag!",
                "Warning tag(script)!",
            }, from item in listener.Items select item.Message);
        }

        [Fact]
        [Trait("Related", "DfmMarkdown")]
        public void TestDfmFencesBlockLevel()
        {
            var root = @"
[!code-FakeREST[REST](api.json)]
[!Code-FakeREST-i[REST-i](api.json ""This is root"")]
[!CODE[No Language](api.json)]
[!code-js[empty](api.json)]
";

            var apiJsonContent = @"
{
   ""method"": ""GET"",
   ""resourceFormat"": ""https://outlook.office.com/api/v1.0/me/events?$select=Subject,Organizer,Start,End"",
   ""requestUrl"": ""https://outlook.office.com/api/v1.0/me/events?$select=Subject,Organizer,Start,End"",
   ""requestHeaders"": {
                ""Accept"": ""application/json""
   }
}";
            File.WriteAllText("api.json", apiJsonContent.Replace("\r\n", "\n"));
            var dependency = new HashSet<string>();
            var marked = DocfxFlavoredMarked.Markup(root, "api.json", dependency: dependency);
            Assert.Equal("<pre><code class=\"lang-FakeREST\" name=\"REST\">\n{\n   &quot;method&quot;: &quot;GET&quot;,\n   &quot;resourceFormat&quot;: &quot;https://outlook.office.com/api/v1.0/me/events?$select=Subject,Organizer,Start,End&quot;,\n   &quot;requestUrl&quot;: &quot;https://outlook.office.com/api/v1.0/me/events?$select=Subject,Organizer,Start,End&quot;,\n   &quot;requestHeaders&quot;: {\n                &quot;Accept&quot;: &quot;application/json&quot;\n   }\n}\n</code></pre><pre><code class=\"lang-FakeREST-i\" name=\"REST-i\" title=\"This is root\">\n{\n   &quot;method&quot;: &quot;GET&quot;,\n   &quot;resourceFormat&quot;: &quot;https://outlook.office.com/api/v1.0/me/events?$select=Subject,Organizer,Start,End&quot;,\n   &quot;requestUrl&quot;: &quot;https://outlook.office.com/api/v1.0/me/events?$select=Subject,Organizer,Start,End&quot;,\n   &quot;requestHeaders&quot;: {\n                &quot;Accept&quot;: &quot;application/json&quot;\n   }\n}\n</code></pre><pre><code name=\"No Language\">\n{\n   &quot;method&quot;: &quot;GET&quot;,\n   &quot;resourceFormat&quot;: &quot;https://outlook.office.com/api/v1.0/me/events?$select=Subject,Organizer,Start,End&quot;,\n   &quot;requestUrl&quot;: &quot;https://outlook.office.com/api/v1.0/me/events?$select=Subject,Organizer,Start,End&quot;,\n   &quot;requestHeaders&quot;: {\n                &quot;Accept&quot;: &quot;application/json&quot;\n   }\n}\n</code></pre><pre><code class=\"lang-js\" name=\"empty\">\n{\n   &quot;method&quot;: &quot;GET&quot;,\n   &quot;resourceFormat&quot;: &quot;https://outlook.office.com/api/v1.0/me/events?$select=Subject,Organizer,Start,End&quot;,\n   &quot;requestUrl&quot;: &quot;https://outlook.office.com/api/v1.0/me/events?$select=Subject,Organizer,Start,End&quot;,\n   &quot;requestHeaders&quot;: {\n                &quot;Accept&quot;: &quot;application/json&quot;\n   }\n}\n</code></pre>", marked);
            Assert.Equal(
                new[] { "api.json" },
                dependency.OrderBy(x => x));
        }

        [Fact]
        [Trait("Related", "DfmMarkdown")]
        public void TestDfmFencesInlineLevel_Legacy()
        {
            var root = @"
[!code-FakeREST[REST](api.json)][!Code-FakeREST-i[REST-i](api.json ""This is root"")][!CODE[No Language](api.json)][!code-js[empty](api.json)]
";

            var apiJsonContent = @"
{
   ""method"": ""GET"",
   ""resourceFormat"": ""https://outlook.office.com/api/v1.0/me/events?$select=Subject,Organizer,Start,End"",
   ""requestUrl"": ""https://outlook.office.com/api/v1.0/me/events?$select=Subject,Organizer,Start,End"",
   ""requestHeaders"": {
                ""Accept"": ""application/json""
   }
}";
            File.WriteAllText("api.json", apiJsonContent.Replace("\r\n", "\n"));
            var dependency = new HashSet<string>();
            var option = DocfxFlavoredMarked.CreateDefaultOptions();
            option.LegacyMode = true;
            var engine = DocfxFlavoredMarked.CreateBuilder(null, null, option).CreateDfmEngine(new DfmRenderer());
            var marked = engine.Markup(root, "api.json", dependency: dependency);
            Assert.Equal("<p><pre><code class=\"lang-FakeREST\" name=\"REST\">\n{\n   &quot;method&quot;: &quot;GET&quot;,\n   &quot;resourceFormat&quot;: &quot;https://outlook.office.com/api/v1.0/me/events?$select=Subject,Organizer,Start,End&quot;,\n   &quot;requestUrl&quot;: &quot;https://outlook.office.com/api/v1.0/me/events?$select=Subject,Organizer,Start,End&quot;,\n   &quot;requestHeaders&quot;: {\n                &quot;Accept&quot;: &quot;application/json&quot;\n   }\n}\n</code></pre><pre><code class=\"lang-FakeREST-i\" name=\"REST-i\" title=\"This is root\">\n{\n   &quot;method&quot;: &quot;GET&quot;,\n   &quot;resourceFormat&quot;: &quot;https://outlook.office.com/api/v1.0/me/events?$select=Subject,Organizer,Start,End&quot;,\n   &quot;requestUrl&quot;: &quot;https://outlook.office.com/api/v1.0/me/events?$select=Subject,Organizer,Start,End&quot;,\n   &quot;requestHeaders&quot;: {\n                &quot;Accept&quot;: &quot;application/json&quot;\n   }\n}\n</code></pre><pre><code name=\"No Language\">\n{\n   &quot;method&quot;: &quot;GET&quot;,\n   &quot;resourceFormat&quot;: &quot;https://outlook.office.com/api/v1.0/me/events?$select=Subject,Organizer,Start,End&quot;,\n   &quot;requestUrl&quot;: &quot;https://outlook.office.com/api/v1.0/me/events?$select=Subject,Organizer,Start,End&quot;,\n   &quot;requestHeaders&quot;: {\n                &quot;Accept&quot;: &quot;application/json&quot;\n   }\n}\n</code></pre><pre><code class=\"lang-js\" name=\"empty\">\n{\n   &quot;method&quot;: &quot;GET&quot;,\n   &quot;resourceFormat&quot;: &quot;https://outlook.office.com/api/v1.0/me/events?$select=Subject,Organizer,Start,End&quot;,\n   &quot;requestUrl&quot;: &quot;https://outlook.office.com/api/v1.0/me/events?$select=Subject,Organizer,Start,End&quot;,\n   &quot;requestHeaders&quot;: {\n                &quot;Accept&quot;: &quot;application/json&quot;\n   }\n}\n</code></pre></p>\n", marked);
            Assert.Equal(
                new[] { "api.json" },
                dependency.OrderBy(x => x));
        }

        [Fact]
        [Trait("Related", "DfmMarkdown")]
        public void TestDfmFencesBlockLevelWithWhitespaceLeading()
        {
            var root = @"
 [!code-FakeREST[REST](api.json)]
  [!Code-FakeREST-i[REST-i](api.json ""This is root"")]
   [!CODE[No Language](api.json)]
  [!code-js[empty](api.json)]
";

            var apiJsonContent = @"
{
   ""method"": ""GET"",
   ""resourceFormat"": ""https://outlook.office.com/api/v1.0/me/events?$select=Subject,Organizer,Start,End"",
   ""requestUrl"": ""https://outlook.office.com/api/v1.0/me/events?$select=Subject,Organizer,Start,End"",
   ""requestHeaders"": {
                ""Accept"": ""application/json""
   }
}";
            File.WriteAllText("api.json", apiJsonContent.Replace("\r\n", "\n"));
            var dependency = new HashSet<string>();
            var marked = DocfxFlavoredMarked.Markup(root, "api.json", dependency: dependency);
            Assert.Equal("<pre><code class=\"lang-FakeREST\" name=\"REST\">\n{\n   &quot;method&quot;: &quot;GET&quot;,\n   &quot;resourceFormat&quot;: &quot;https://outlook.office.com/api/v1.0/me/events?$select=Subject,Organizer,Start,End&quot;,\n   &quot;requestUrl&quot;: &quot;https://outlook.office.com/api/v1.0/me/events?$select=Subject,Organizer,Start,End&quot;,\n   &quot;requestHeaders&quot;: {\n                &quot;Accept&quot;: &quot;application/json&quot;\n   }\n}\n</code></pre><pre><code class=\"lang-FakeREST-i\" name=\"REST-i\" title=\"This is root\">\n{\n   &quot;method&quot;: &quot;GET&quot;,\n   &quot;resourceFormat&quot;: &quot;https://outlook.office.com/api/v1.0/me/events?$select=Subject,Organizer,Start,End&quot;,\n   &quot;requestUrl&quot;: &quot;https://outlook.office.com/api/v1.0/me/events?$select=Subject,Organizer,Start,End&quot;,\n   &quot;requestHeaders&quot;: {\n                &quot;Accept&quot;: &quot;application/json&quot;\n   }\n}\n</code></pre><pre><code name=\"No Language\">\n{\n   &quot;method&quot;: &quot;GET&quot;,\n   &quot;resourceFormat&quot;: &quot;https://outlook.office.com/api/v1.0/me/events?$select=Subject,Organizer,Start,End&quot;,\n   &quot;requestUrl&quot;: &quot;https://outlook.office.com/api/v1.0/me/events?$select=Subject,Organizer,Start,End&quot;,\n   &quot;requestHeaders&quot;: {\n                &quot;Accept&quot;: &quot;application/json&quot;\n   }\n}\n</code></pre><pre><code class=\"lang-js\" name=\"empty\">\n{\n   &quot;method&quot;: &quot;GET&quot;,\n   &quot;resourceFormat&quot;: &quot;https://outlook.office.com/api/v1.0/me/events?$select=Subject,Organizer,Start,End&quot;,\n   &quot;requestUrl&quot;: &quot;https://outlook.office.com/api/v1.0/me/events?$select=Subject,Organizer,Start,End&quot;,\n   &quot;requestHeaders&quot;: {\n                &quot;Accept&quot;: &quot;application/json&quot;\n   }\n}\n</code></pre>", marked);
            Assert.Equal(
                new[] { "api.json" },
                dependency.OrderBy(x => x));
        }

        [Fact]
        [Trait("Related", "DfmMarkdown")]
        public void TestDfmFencesInlineLevel()
        {
            var root = @"
| Code in table | Header1 |
 ----------------- | ----------------------------
| [!code-FakeREST[REST](api.json)] | [!Code-FakeREST-i[REST-i](api.json ""This is root"")]
| [!CODE[No Language](api.json)] | [!code-js[empty](api.json)]
";

            var apiJsonContent = @"
{
   ""method"": ""GET"",
   ""resourceFormat"": ""https://outlook.office.com/api/v1.0/me/events?$select=Subject,Organizer,Start,End"",
   ""requestUrl"": ""https://outlook.office.com/api/v1.0/me/events?$select=Subject,Organizer,Start,End"",
   ""requestHeaders"": {
                ""Accept"": ""application/json""
   }
}";
            File.WriteAllText("api.json", apiJsonContent.Replace("\r\n", "\n"));
            var dependency = new HashSet<string>();
            var marked = DocfxFlavoredMarked.Markup(root, "api.json", dependency: dependency);
            const string expected = @"<table>
<thead>
<tr>
<th>Code in table</th>
<th>Header1</th>
</tr>
</thead>
<tbody>
<tr>
<td><pre><code class=""lang-FakeREST"" name=""REST"">
{
   &quot;method&quot;: &quot;GET&quot;,
   &quot;resourceFormat&quot;: &quot;https://outlook.office.com/api/v1.0/me/events?$select=Subject,Organizer,Start,End&quot;,
   &quot;requestUrl&quot;: &quot;https://outlook.office.com/api/v1.0/me/events?$select=Subject,Organizer,Start,End&quot;,
   &quot;requestHeaders&quot;: {
                &quot;Accept&quot;: &quot;application/json&quot;
   }
}
</code></pre></td>
<td><pre><code class=""lang-FakeREST-i"" name=""REST-i"" title=""This is root"">
{
   &quot;method&quot;: &quot;GET&quot;,
   &quot;resourceFormat&quot;: &quot;https://outlook.office.com/api/v1.0/me/events?$select=Subject,Organizer,Start,End&quot;,
   &quot;requestUrl&quot;: &quot;https://outlook.office.com/api/v1.0/me/events?$select=Subject,Organizer,Start,End&quot;,
   &quot;requestHeaders&quot;: {
                &quot;Accept&quot;: &quot;application/json&quot;
   }
}
</code></pre></td>
</tr>
<tr>
<td><pre><code name=""No Language"">
{
   &quot;method&quot;: &quot;GET&quot;,
   &quot;resourceFormat&quot;: &quot;https://outlook.office.com/api/v1.0/me/events?$select=Subject,Organizer,Start,End&quot;,
   &quot;requestUrl&quot;: &quot;https://outlook.office.com/api/v1.0/me/events?$select=Subject,Organizer,Start,End&quot;,
   &quot;requestHeaders&quot;: {
                &quot;Accept&quot;: &quot;application/json&quot;
   }
}
</code></pre></td>
<td><pre><code class=""lang-js"" name=""empty"">
{
   &quot;method&quot;: &quot;GET&quot;,
   &quot;resourceFormat&quot;: &quot;https://outlook.office.com/api/v1.0/me/events?$select=Subject,Organizer,Start,End&quot;,
   &quot;requestUrl&quot;: &quot;https://outlook.office.com/api/v1.0/me/events?$select=Subject,Organizer,Start,End&quot;,
   &quot;requestHeaders&quot;: {
                &quot;Accept&quot;: &quot;application/json&quot;
   }
}
</code></pre></td>
</tr>
</tbody>
</table>
";
            Assert.Equal(expected.Replace("\r\n", "\n"), marked);
            Assert.Equal(
                new[] { "api.json" },
                dependency.OrderBy(x => x));
        }

        [Fact]
        [Trait("Related", "DfmMarkdown")]
        public void TestDfmFencesBlockLevelWithWorkingFolder()
        {
            var root = @"[!code-REST[REST](~/api.json)]";
            var apiJsonContent = @"
{
   ""method"": ""GET"",
   ""resourceFormat"": ""https://outlook.office.com/api/v1.0/me/events?$select=Subject,Organizer,Start,End"",
   ""requestUrl"": ""https://outlook.office.com/api/v1.0/me/events?$select=Subject,Organizer,Start,End"",
   ""requestHeaders"": {
                ""Accept"": ""application/json""
   }
}";
            File.WriteAllText("api.json", apiJsonContent.Replace("\r\n", "\n"));
            var dependency = new HashSet<string>();
            var marked = DocfxFlavoredMarked.Markup(root, "api.json", dependency: dependency);
            Assert.Equal(@"<pre><code class=""lang-REST"" name=""REST"">
{
   &quot;method&quot;: &quot;GET&quot;,
   &quot;resourceFormat&quot;: &quot;https://outlook.office.com/api/v1.0/me/events?$select=Subject,Organizer,Start,End&quot;,
   &quot;requestUrl&quot;: &quot;https://outlook.office.com/api/v1.0/me/events?$select=Subject,Organizer,Start,End&quot;,
   &quot;requestHeaders&quot;: {
                &quot;Accept&quot;: &quot;application/json&quot;
   }
}
</code></pre>".Replace("\r\n", "\n"), marked);
            Assert.Equal(
                new[] { "~/api.json" },
                dependency.OrderBy(x => x));
        }

        [Theory]
        [Trait("Owner", "humao")]
        [Trait("Related", "DfmMarkdown")]
        #region Inline Data
        [InlineData(@"[!code-csharp[Main](Program.cs)]", @"<pre><code class=""lang-csharp"" name=""Main"">namespace ConsoleApplication1
{
    // &lt;namespace&gt;
    using System;
    using System.Collections.Generic;
    using System.IO;
    // &lt;/namespace&gt;

    // &lt;snippetprogram&gt;
    class Program
    {
        static void Main(string[] args)
        {
            string s = &quot;\ntest&quot;;
            int i = 100;
        }
    }
    // &lt;/snippetprogram&gt;

    #region Helper
    internal static class Helper
    {
        #region Foo
        public static void Foo()
        {
        }
        #endregion Foo
    }
    #endregion
}
</code></pre>")]
        [InlineData(@"[!code[Main](Program.cs#L12-L16 ""This is root"")]", @"<pre><code name=""Main"" title=""This is root"">static void Main(string[] args)
{
    string s = &quot;\ntest&quot;;
    int i = 100;
}
</code></pre>")]
        [InlineData(@"[!code[Main](Program.cs#L12-L100 ""This is root"")]", @"<pre><code name=""Main"" title=""This is root"">        static void Main(string[] args)
        {
            string s = &quot;\ntest&quot;;
            int i = 100;
        }
    }
    // &lt;/snippetprogram&gt;

    #region Helper
    internal static class Helper
    {
        #region Foo
        public static void Foo()
        {
        }
        #endregion Foo
    }
    #endregion
}
</code></pre>")]
        [InlineData(@"[!code-csharp[Main](Program.cs#namespace ""This is root"")]", @"<pre><code class=""lang-csharp"" name=""Main"" title=""This is root"">using System;
using System.Collections.Generic;
using System.IO;
</code></pre>")]
        [InlineData(@"[!code-csharp[Main](Program.cs#NAMESPACE ""This is root"")]", @"<pre><code class=""lang-csharp"" name=""Main"" title=""This is root"">using System;
using System.Collections.Generic;
using System.IO;
</code></pre>")]
        [InlineData(@"[!code-csharp[Main](Program.cs#program ""This is root"")]", @"<pre><code class=""lang-csharp"" name=""Main"" title=""This is root"">class Program
{
    static void Main(string[] args)
    {
        string s = &quot;\ntest&quot;;
        int i = 100;
    }
}
</code></pre>")]
        [InlineData(@"[!code-csharp[Main](Program.cs#snippetprogram ""This is root"")]", @"<pre><code class=""lang-csharp"" name=""Main"" title=""This is root"">class Program
{
    static void Main(string[] args)
    {
        string s = &quot;\ntest&quot;;
        int i = 100;
    }
}
</code></pre>")]
        [InlineData(@"[!code[Main](Program.cs?name=Foo ""This is root"")]", @"<pre><code name=""Main"" title=""This is root"">public static void Foo()
{
}
</code></pre>")]
        [InlineData(@"[!code-csharp[Main](Program.cs?name=namespace ""This is root"")]", @"<pre><code class=""lang-csharp"" name=""Main"" title=""This is root"">using System;
using System.Collections.Generic;
using System.IO;
</code></pre>")]
        [InlineData(@"[!code[Main](Program.cs?start=5&end=9 ""This is root"")]", @"<pre><code name=""Main"" title=""This is root"">using System.Collections.Generic;
using System.IO;
// &lt;/namespace&gt;

// &lt;snippetprogram&gt;
</code></pre>")]
        [InlineData(@"[!code[Main](Program.cs?name=Helper ""This is root"")]", @"<pre><code name=""Main"" title=""This is root"">internal static class Helper
{
    public static void Foo()
    {
    }
}
</code></pre>")]
        [InlineData(@"[!code[Main](Program.cs?range=1-2,10,20-21,29- ""This is root"")]", @"<pre><code name=""Main"" title=""This is root"">namespace ConsoleApplication1
{
    class Program
    #region Helper
    internal static class Helper
    #endregion
}
</code></pre>")]
        [InlineData(@"[!code[Main](Program.cs?range=1,21,24-26,1,10,12-16 ""This is root"")]", @"<pre><code name=""Main"" title=""This is root"">namespace ConsoleApplication1
    internal static class Helper
        public static void Foo()
        {
        }
namespace ConsoleApplication1
    class Program
        static void Main(string[] args)
        {
            string s = &quot;\ntest&quot;;
            int i = 100;
        }
</code></pre>")]
        [InlineData(@"[!code-csharp[Main](Program.cs?highlight=1)]", @"<pre><code class=""lang-csharp"" name=""Main"" highlight-lines=""1"">namespace ConsoleApplication1
{
    // &lt;namespace&gt;
    using System;
    using System.Collections.Generic;
    using System.IO;
    // &lt;/namespace&gt;

    // &lt;snippetprogram&gt;
    class Program
    {
        static void Main(string[] args)
        {
            string s = &quot;\ntest&quot;;
            int i = 100;
        }
    }
    // &lt;/snippetprogram&gt;

    #region Helper
    internal static class Helper
    {
        #region Foo
        public static void Foo()
        {
        }
        #endregion Foo
    }
    #endregion
}
</code></pre>")]
        [InlineData(@"[!code[Main](Program.cs?start=5&end=9&highlight=1 ""This is root"")]", @"<pre><code name=""Main"" title=""This is root"" highlight-lines=""1"">using System.Collections.Generic;
using System.IO;
// &lt;/namespace&gt;

// &lt;snippetprogram&gt;
</code></pre>")]
        [InlineData(@"[!code[Main](Program.cs?name=Helper&highlight=1 ""This is root"")]", @"<pre><code name=""Main"" title=""This is root"" highlight-lines=""1"">internal static class Helper
{
    public static void Foo()
    {
    }
}
</code></pre>")]
        [InlineData(@"[!code[Main](Program.cs?range=1-2,10,20-21,29-&highlight=1-2,7- ""This is root"")]", @"<pre><code name=""Main"" title=""This is root"" highlight-lines=""1-2,7-"">namespace ConsoleApplication1
{
    class Program
    #region Helper
    internal static class Helper
    #endregion
}
</code></pre>")]
        [InlineData(@"[!code[Main](Program.cs?range=1,21,24-26,1,10,12-16&highlight=8-12 ""This is root"")]", @"<pre><code name=""Main"" title=""This is root"" highlight-lines=""8-12"">namespace ConsoleApplication1
    internal static class Helper
        public static void Foo()
        {
        }
namespace ConsoleApplication1
    class Program
        static void Main(string[] args)
        {
            string s = &quot;\ntest&quot;;
            int i = 100;
        }
</code></pre>")]
        [InlineData(@"[!code-csharp[Main](Program.cs?dedent=0)]", @"<pre><code class=""lang-csharp"" name=""Main"">namespace ConsoleApplication1
{
    // &lt;namespace&gt;
    using System;
    using System.Collections.Generic;
    using System.IO;
    // &lt;/namespace&gt;

    // &lt;snippetprogram&gt;
    class Program
    {
        static void Main(string[] args)
        {
            string s = &quot;\ntest&quot;;
            int i = 100;
        }
    }
    // &lt;/snippetprogram&gt;

    #region Helper
    internal static class Helper
    {
        #region Foo
        public static void Foo()
        {
        }
        #endregion Foo
    }
    #endregion
}
</code></pre>")]
        [InlineData(@"[!code[Main](Program.cs?start=5&end=9&dedent=0 ""This is root"")]", @"<pre><code name=""Main"" title=""This is root"">    using System.Collections.Generic;
    using System.IO;
    // &lt;/namespace&gt;

    // &lt;snippetprogram&gt;
</code></pre>")]
        [InlineData(@"[!code[Main](Program.cs?name=Helper&dedent=8 ""This is root"")]", @"<pre><code name=""Main"" title=""This is root"">internal static class Helper
{
public static void Foo()
{
}
}
</code></pre>")]
        [InlineData(@"[!code[Main](Program.cs?range=1-2,10,20-21,29-&dedent=-4 ""Auto dedent if dedent < 0"")]", @"<!-- Dedent length -4 should be positive. Auto-dedent will be applied. -->
<pre><code name=""Main"" title=""Auto dedent if dedent &lt; 0"">namespace ConsoleApplication1
{
    class Program
    #region Helper
    internal static class Helper
    #endregion
}
</code></pre>")]
        #endregion
        public void TestDfmFencesBlockLevelWithQueryString(string fencesPath, string expectedContent)
        {
            // arrange
            var content = @"namespace ConsoleApplication1
{
    // <namespace>
    using System;
    using System.Collections.Generic;
    using System.IO;
    // </namespace>

    // <snippetprogram>
    class Program
    {
        static void Main(string[] args)
        {
            string s = ""\ntest"";
            int i = 100;
        }
    }
    // </snippetprogram>

    #region Helper
    internal static class Helper
    {
        #region Foo
        public static void Foo()
        {
        }
        #endregion Foo
    }
    #endregion
}";
            File.WriteAllText("Program.cs", content.Replace("\r\n", "\n"));

            // act
            var marked = DocfxFlavoredMarked.Markup(fencesPath, "Program.cs");

            // assert
            Assert.Equal(expectedContent.Replace("\r\n", "\n"), marked);
        }

        [Theory]
        [Trait("Related", "DfmMarkdown")]
        [InlineData(null, @"<pre><code class=""lang-csharp"">namespace ConsoleApplication1
{
    // &lt;namespace&gt;
    using System;
    using System.Collections.Generic;
    using System.IO;
    // &lt;/namespace&gt;

    // &lt;snippetprogram&gt;
    class Program
    {
        static void Main(string[] args)
        {
            string s = &quot;\ntest&quot;;
            int i = 100;
        }
    }
    // &lt;/snippetprogram&gt;

    #region Helper
    internal static class Helper
    {
        #region Foo
        public static void Foo()
        {
        }
        #endregion Foo
    }
    #endregion
}
</code></pre>")]
        [InlineData("", @"<pre><code class=""lang-csharp"">namespace ConsoleApplication1
{
    // &lt;namespace&gt;
    using System;
    using System.Collections.Generic;
    using System.IO;
    // &lt;/namespace&gt;

    // &lt;snippetprogram&gt;
    class Program
    {
        static void Main(string[] args)
        {
            string s = &quot;\ntest&quot;;
            int i = 100;
        }
    }
    // &lt;/snippetprogram&gt;

    #region Helper
    internal static class Helper
    {
        #region Foo
        public static void Foo()
        {
        }
        #endregion Foo
    }
    #endregion
}
</code></pre>")]
        [InlineData("?", "<!-- Length of queryStringAndFragment can not be 1 -->\n")]
        [InlineData("?range=1-2,10,20-21,29-&dedent=0&highlight=1-2,7-", @"<pre><code class=""lang-csharp"" highlight-lines=""1-2,7-"">namespace ConsoleApplication1
{
    class Program
    #region Helper
    internal static class Helper
    #endregion
}
</code></pre>")]
        [InlineData("#namespace", @"<pre><code class=""lang-csharp"">using System;
using System.Collections.Generic;
using System.IO;
</code></pre>")]
        public void TestDfmFencesRenderFromCodeContent(string queryStringAndFragment, string expectedContent)
        {
            // arrange
            var content = @"namespace ConsoleApplication1
{
    // <namespace>
    using System;
    using System.Collections.Generic;
    using System.IO;
    // </namespace>

    // <snippetprogram>
    class Program
    {
        static void Main(string[] args)
        {
            string s = ""\ntest"";
            int i = 100;
        }
    }
    // </snippetprogram>

    #region Helper
    internal static class Helper
    {
        #region Foo
        public static void Foo()
        {
        }
        #endregion Foo
    }
    #endregion
}";

            // act
            var renderer = new DfmCodeRenderer();
            var marked = renderer.RenderFencesFromCodeContent(content, "test.cs", queryStringAndFragment, null, "csharp");

            Assert.Equal(expectedContent.Replace("\r\n", "\n"), marked);
        }

        [Fact]
        public void CodeSnippetTagsShouldMatchCaseInsensitive()
        {
            //arange
            var content = @"// <tag1>
line1
// <tag2>
line2
// </tag2>
line3
// </TAG1>
// <unmatched>
";
            File.WriteAllText("Program.cs", content.Replace("\r\n", "\n"));

            // act
            var marked = DocfxFlavoredMarked.Markup("[!code[tag1](Program.cs#Tag1)]", "Program.cs");

            // assert
            var expected = "<pre><code name=\"tag1\">line1\nline2\nline3\n</code></pre>";
            Assert.Equal(expected.Replace("\r\n", "\n"), marked);
        }

        [Fact]
        public void CodeSnippetTagsShouldSucceedWhenDuplicateWithoutWarning()
        {
            //arange
            var content = @"// <tag1>
line1
// <tag1>
line2
// </tag1>
line3
// </TAG1>
// <tag2>
line4
// </tag2>
";
            File.WriteAllText("Program.cs", content.Replace("\r\n", "\n"));

            // act
            var listener = TestLoggerListener.CreateLoggerListenerWithPhaseEqualFilter("Extract Dfm Code");
            Logger.RegisterListener(listener);
            var marked = DocfxFlavoredMarked.Markup("[!code[tag2](Program.cs#Tag2)]", "Program.cs");
            Logger.UnregisterListener(listener);

            // assert
            Assert.Empty(listener.Items.Select(i => i.LogLevel == LogLevel.Warning));
            var expected = "<pre><code name=\"tag2\">line4\n</code></pre>";
            Assert.Equal(expected.Replace("\r\n", "\n"), marked);
        }

        [Fact]
        public void CodeSnippetTagsShouldSucceedWhenDuplicateWithWarningWhenReferenced()
        {
            //arange
            var content = @"// <tag1>
line1
// <tag1>
line2
// </tag1>
line3
// </TAG1>
// <tag2>
line4
// </tag2>
";
            File.WriteAllText("Program.cs", content.Replace("\r\n", "\n"));

            // act
            var listener = TestLoggerListener.CreateLoggerListenerWithPhaseStartFilter("Extract Dfm Code");
            Logger.RegisterListener(listener);
            var marked = DocfxFlavoredMarked.Markup("[!code[tag1](Program.cs#Tag1)]", "Program.cs");
            Logger.UnregisterListener(listener);

            // assert
            Assert.Equal(1, listener.Items.Count(i => i.LogLevel == LogLevel.Warning));
            var expected = "<pre><code name=\"tag1\">line2\n</code></pre>";
            Assert.Equal(expected.Replace("\r\n", "\n"), marked);
        }

        [Fact]
        public void CodeSnippetTagsShouldSucceedWhenReferencedFileContainsRegionWithoutName()
        {
            // arrange
            var content = @"#region
public class MyClass
#region
{
    #region main
    static void Main()
    {
    }
    #endregion
}
#endregion
#endregion";
            File.WriteAllText("Program.cs", content.Replace("\r\n", "\n"));

            // act
            var marked = DocfxFlavoredMarked.Markup("[!code[MyClass](Program.cs#main)]", "Program.cs");

            // assert
            var expected = @"<pre><code name=""MyClass"">static void Main()
{
}
</code></pre>";
            Assert.Equal(expected.Replace("\r\n", "\n"), marked);
        }

        [Fact]
        public void CodeSnippetShouldNotWorkInParagragh()
        {
            var marked = DocfxFlavoredMarked.Markup("text [!code[test](test.md)]", "test.md");
            var expected = @"<p>text [!code<a href=""test.md"" data-raw-source=""[test](test.md)"">test</a>]</p>
";
            Assert.Equal(expected.Replace("\r\n", "\n"), marked);
        }

        private static void WriteToFile(string file, string content)
        {
            var dir = Path.GetDirectoryName(file);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(file, content);
        }

        #region Fallback folders testing

        [Fact]
        [Trait("Related", "DfmMarkdown")]
        public void TestFallback_Inclusion_random_name()
        {
            // -root_folder (this is also docset folder)
            //  |- root.md
            //  |- a_folder
            //  |  |- a.md
            //  |- token_folder
            //  |  |- token1.md
            // -fallback_folder
            //  |- token_folder
            //     |- token2.md

            // 1. Prepare data
            var uniqueFolderName = Path.GetRandomFileName();
            var root = $@"1markdown root.md main content start.

[!include[a](a_folder_{uniqueFolderName}/a_{uniqueFolderName}.md ""This is a.md"")]

markdown root.md main content end.";

            var a = $@"1markdown a.md main content start.

[!include[token1](../token_folder_{uniqueFolderName}/token1_{uniqueFolderName}.md ""This is token1.md"")]
[!include[token1](../token_folder_{uniqueFolderName}/token2_{uniqueFolderName}.md ""This is token2.md"")]

markdown a.md main content end.";

            var token1 = $@"1markdown token1.md content start.

[!include[token2](token2_{uniqueFolderName}.md ""This is token2.md"")]

markdown token1.md content end.";

            var token2 = @"**1markdown token2.md main content**";

            WriteToFile($"{uniqueFolderName}/root_folder_{uniqueFolderName}/root_{uniqueFolderName}.md", root);
            WriteToFile($"{uniqueFolderName}/root_folder_{uniqueFolderName}/a_folder_{uniqueFolderName}/a_{uniqueFolderName}.md", a);
            WriteToFile($"{uniqueFolderName}/root_folder_{uniqueFolderName}/token_folder_{uniqueFolderName}/token1_{uniqueFolderName}.md", token1);
            WriteToFile($"{uniqueFolderName}/fallback_folder_{uniqueFolderName}/token_folder_{uniqueFolderName}/token2_{uniqueFolderName}.md", token2);

            var fallbackFolders = new List<string> { { Path.Combine(Directory.GetCurrentDirectory(), $"{uniqueFolderName}/fallback_folder_{uniqueFolderName}") } };
            var dependency = new HashSet<string>();
            var marked = DocfxFlavoredMarked.Markup(Path.Combine(Directory.GetCurrentDirectory(), $"{uniqueFolderName}/root_folder_{uniqueFolderName}"), root, fallbackFolders, $"root_{uniqueFolderName}.md", dependency: dependency);
            Assert.Equal($@"<p>1markdown root.md main content start.</p>
<p>1markdown a.md main content start.</p>
<p>1markdown token1.md content start.</p>
<p><strong>1markdown token2.md main content</strong></p>
<p>markdown token1.md content end.</p>
<p><strong>1markdown token2.md main content</strong></p>
<p>markdown a.md main content end.</p>
<p>markdown root.md main content end.</p>
".Replace("\r\n", "\n"), marked);
            Assert.Equal(
                new[] { $"../fallback_folder_{uniqueFolderName}/token_folder_{uniqueFolderName}/token2_{uniqueFolderName}.md", $"a_folder_{uniqueFolderName}/a_{uniqueFolderName}.md", $"token_folder_{uniqueFolderName}/token1_{uniqueFolderName}.md", $"token_folder_{uniqueFolderName}/token2_{uniqueFolderName}.md" },
                dependency.OrderBy(x => x));
        }

        [Fact]
        [Trait("Related", "DfmMarkdown")]
        public void TestFallbaclk_Inclusion_Token_Git()
        {
            // -root_folder (this is also docset folder)
            //  |- root.md
            //  |- a_folder
            //  |  |- a.md
            //  |- token_folder
            //  |  |- token1.md
            // -fallback_folder
            //  |- token_folder
            //     |- token2.md

            // 1. Prepare data
            var uniqueFolderName = Path.GetRandomFileName();
            var root = $@"1markdown root.md main content start.

[!include[a](a_folder_{uniqueFolderName}/a_{uniqueFolderName}.md ""This is a.md"")]

markdown root.md main content end.";

            var a = $@"1markdown a.md main content start.

[!include[token1](../token_folder_{uniqueFolderName}/token1_{uniqueFolderName}.md ""This is token1.md"")]
[!include[token1](../token_folder_{uniqueFolderName}/token2_{uniqueFolderName}.md ""This is token2.md"")]

markdown a.md main content end.";

            var token1 = $@"1markdown token1.md content start.

[!include[token2](token2_{uniqueFolderName}.md ""This is token2.md"")]

markdown token1.md content end.";

            var token2 = @"**1markdown token2.md main content**";

            WriteToFile($"{uniqueFolderName}/root_folder_{uniqueFolderName}/root_{uniqueFolderName}.md", root);
            WriteToFile($"{uniqueFolderName}/root_folder_{uniqueFolderName}/a_folder_{uniqueFolderName}/a_{uniqueFolderName}.md", a);
            WriteToFile($"{uniqueFolderName}/root_folder_{uniqueFolderName}/token_folder_{uniqueFolderName}/token1_{uniqueFolderName}.md", token1);
            WriteToFile($"{uniqueFolderName}/fallback_folder_{uniqueFolderName}/token_folder_{uniqueFolderName}/token2_{uniqueFolderName}.md", token2);

            var fallbackFolders = new List<string> { { Path.Combine(Directory.GetCurrentDirectory(), $"{uniqueFolderName}/fallback_folder_{uniqueFolderName}") } };
            foreach (var fallbackFolder in fallbackFolders)
            {
                Assert.True(GitUtility.InitRepo(fallbackFolder, $"https://github.com/docfxtest{uniqueFolderName}"));
                Assert.True(GitUtility.ApplyChange(fallbackFolder, "add fallback files"));
                File.Delete($"{uniqueFolderName}/fallback_folder_{uniqueFolderName}/token_folder_{uniqueFolderName}/token2_{uniqueFolderName}.md");
                Assert.True(GitUtility.ApplyChange(fallbackFolder, "delete fallback files"));
            }

            var original = Environment.GetEnvironmentVariable("FALL_BACK_TO_GIT");
            try
            {
                Environment.SetEnvironmentVariable("FALL_BACK_TO_GIT", "true");
                var dependency = new HashSet<string>();
                var marked = DocfxFlavoredMarked.Markup(Path.Combine(Directory.GetCurrentDirectory(), $"{uniqueFolderName}/root_folder_{uniqueFolderName}"), root, fallbackFolders, $"root_{uniqueFolderName}.md", dependency: dependency);
                Assert.Equal($@"<p>1markdown root.md main content start.</p>
<p>1markdown a.md main content start.</p>
<p>1markdown token1.md content start.</p>
<p><strong>1markdown token2.md main content</strong></p>
<p>markdown token1.md content end.</p>
<p><strong>1markdown token2.md main content</strong></p>
<p>markdown a.md main content end.</p>
<p>markdown root.md main content end.</p>
".Replace("\r\n", "\n"), marked);
                Assert.Equal(
                    new[] { $"../fallback_folder_{uniqueFolderName}/token_folder_{uniqueFolderName}/token2_{uniqueFolderName}.md", $"a_folder_{uniqueFolderName}/a_{uniqueFolderName}.md", $"token_folder_{uniqueFolderName}/token1_{uniqueFolderName}.md", $"token_folder_{uniqueFolderName}/token2_{uniqueFolderName}.md" },
                    dependency.OrderBy(x => x));
            }
            finally
            {
                Environment.SetEnvironmentVariable("FALL_BACK_TO_GIT", original);
            }
        }

        [Fact]
        [Trait("Related", "DfmMarkdown")]
        public void TestFallback_InclusionWithCodeFences()
        {
            // -root_folder (this is also docset folder)
            //  |- root.md
            //  |- a_folder
            //     |- a.md
            //  |- code_folder
            //     |- sample1.cs
            // -fallback_folder
            //  |- a_folder
            //     |- code_in_a.cs
            //  |- code_folder
            //     |- sample2.cs

            // 1. Prepare data
            var root = @"markdown root.md main content start.

mardown a content in root.md content start

[!include[a](a_folder/a.md ""This is a.md"")]

mardown a content in root.md content end

sample 1 code in root.md content start

[!CODE-cs[this is sample 1 code](code_folder/sample1.cs)]

sample 1 code in root.md content end

sample 2 code in root.md content start

[!CODE-cs[this is sample 2 code](code_folder/sample2.cs)]

sample 2 code in root.md content end

markdown root.md main content end.";

            var a = @"markdown a.md main content start.

code_in_a code in a.md content start

[!CODE-cs[this is code_in_a code](code_in_a.cs)]

code_in_a in a.md content end

markdown a.md a.md content end.";

            var code_in_a = @"namespace code_in_a{}";

            var sample1 = @"namespace sample1{}";

            var sample2 = @"namespace sample2{}";

            var uniqueFolderName = Path.GetRandomFileName();
            WriteToFile($"{uniqueFolderName}/root_folder/root.md", root);
            WriteToFile($"{uniqueFolderName}/root_folder/a_folder/a.md", a);
            WriteToFile($"{uniqueFolderName}/root_folder/code_folder/sample1.cs", sample1);
            WriteToFile($"{uniqueFolderName}/fallback_folder/a_folder/code_in_a.cs", code_in_a);
            WriteToFile($"{uniqueFolderName}/fallback_folder/code_folder/sample2.cs", sample2);

            var fallbackFolders = new List<string> { { Path.Combine(Directory.GetCurrentDirectory(), $"{uniqueFolderName}/fallback_folder") } };

            // Verify root.md markup result
            var rootDependency = new HashSet<string>();
            var rootMarked = DocfxFlavoredMarked.Markup(Path.Combine(Directory.GetCurrentDirectory(), $"{uniqueFolderName}/root_folder"), root, fallbackFolders, "root.md", dependency: rootDependency);
            Assert.Equal(@"<p>markdown root.md main content start.</p>
<p>mardown a content in root.md content start</p>
<p>markdown a.md main content start.</p>
<p>code_in_a code in a.md content start</p>
<pre><code class=""lang-cs"" name=""this is code_in_a code"">namespace code_in_a{}
</code></pre><p>code_in_a in a.md content end</p>
<p>markdown a.md a.md content end.</p>
<p>mardown a content in root.md content end</p>
<p>sample 1 code in root.md content start</p>
<pre><code class=""lang-cs"" name=""this is sample 1 code"">namespace sample1{}
</code></pre><p>sample 1 code in root.md content end</p>
<p>sample 2 code in root.md content start</p>
<pre><code class=""lang-cs"" name=""this is sample 2 code"">namespace sample2{}
</code></pre><p>sample 2 code in root.md content end</p>
<p>markdown root.md main content end.</p>
".Replace("\r\n", "\n"), rootMarked);
            Assert.Equal(
                new[] { "../fallback_folder/a_folder/code_in_a.cs", "../fallback_folder/code_folder/sample2.cs", "a_folder/a.md", "a_folder/code_in_a.cs", "code_folder/sample1.cs", "code_folder/sample2.cs" },
                rootDependency.OrderBy(x => x));

            // Verify a.md markup result
            var aDependency = new HashSet<string>();
            var aMarked = DocfxFlavoredMarked.Markup(Path.Combine(Directory.GetCurrentDirectory(), $"{uniqueFolderName}/root_folder"), a, fallbackFolders, "a_folder/a.md", dependency: aDependency);
            Assert.Equal(@"<p>markdown a.md main content start.</p>
<p>code_in_a code in a.md content start</p>
<pre><code class=""lang-cs"" name=""this is code_in_a code"">namespace code_in_a{}
</code></pre><p>code_in_a in a.md content end</p>
<p>markdown a.md a.md content end.</p>
".Replace("\r\n", "\n"), aMarked);
            Assert.Equal(
                new[] { "../../fallback_folder/a_folder/code_in_a.cs", "code_in_a.cs" },
                aDependency.OrderBy(x => x));
        }

        #endregion
    }
}
