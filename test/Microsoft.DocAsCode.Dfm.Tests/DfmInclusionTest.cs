// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm.Tests
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;

    using Xunit;

    using Microsoft.DocAsCode.Build.Engine;
    using Microsoft.DocAsCode.Dfm;
    using Microsoft.DocAsCode.MarkdownLite;
    using Microsoft.DocAsCode.Plugins;

    [Collection("docfx STA")]
    public class DfmInclusionTest
    {
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

        private static void WriteToFile(string file, string content)
        {
            var dir = Path.GetDirectoryName(file);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }
            File.WriteAllText(file, content);
        }
    }
}
