// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Tests
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;
    using Markdig;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.MarkdigEngine.Extensions;
    using Microsoft.DocAsCode.Plugins;

    using Xunit;

    [Collection("docfx STA")]
    public class InclusionTest
    {
        [Fact]
        [Trait("Related", "Inclusion")]
        public void TestBlockLevelInclusion_General()
        {
            var root = @"
# Hello World

Test Include File

[!include[refa](a.md)]

";

            var refa = @"---
title: include file
description: include file
---

# Hello Include File A

This is a file A included by another file. [!include[refb](b.md)]

";

            var refb = @"---
title: include file
description: include file
---

# Hello Include File B
";
            TestUtility.WriteToFile("r/root.md", root);
            TestUtility.WriteToFile("r/a.md", refa);
            TestUtility.WriteToFile("r/b.md", refb);

            var result = TestUtility.MarkupWithoutSourceInfo(root, "r/root.md");
            var expected = @"<h1 id=""hello-world"">Hello World</h1>
<p>Test Include File</p>
<h1 id=""hello-include-file-a"">Hello Include File A</h1>
<p>This is a file A included by another file. # Hello Include File B</p>
";
            Assert.Equal(expected.Replace("\r\n", "\n"), result.Html);

            var dependency = result.Dependency;
            var expectedDependency = new List<string> { "a.md", "b.md" };
            Assert.Equal(expectedDependency.ToImmutableList(), dependency);
        }

        [Fact]
        [Trait("Related", "IncludeFile")]
        public void TestBlockLevelInclusion_Esacape()
        {
            var root = @"
# Hello World

Test Include File

[!include[refa](a\(x\).md)]

";

            var refa = @"
# Hello Include File A

This is a file A included by another file.
";

            TestUtility.WriteToFile("r/root.md", root);
            TestUtility.WriteToFile("r/a(x).md", refa);

            var result = TestUtility.MarkupWithoutSourceInfo(root, "r/root.md");
            var expected = @"<h1 id=""hello-world"">Hello World</h1>
<p>Test Include File</p>
<h1 id=""hello-include-file-a"">Hello Include File A</h1>
<p>This is a file A included by another file.</p>
";
            Assert.Equal(expected.Replace("\r\n", "\n"), result.Html);

            var dependency = result.Dependency;
            var expectedDependency = new List<string> { "a(x).md" };
            Assert.Equal(expectedDependency.ToImmutableList(), dependency);
        }

        [Fact]
        [Trait("Related", "Inclusion")]
        public void TestBlockLevelInclusion_RelativePath()
        {
            var root = @"
# Hello World

Test Include File

[!include[refa](~/r/a.md)]

";

            var refa = @"
# Hello Include File A

This is a file A included by another file.
";

            TestUtility.WriteToFile("r/root.md", root);
            TestUtility.WriteToFile("r/a.md", refa);

            var result = TestUtility.MarkupWithoutSourceInfo(root, "r/root.md");
            var expected = @"<h1 id=""hello-world"">Hello World</h1>
<p>Test Include File</p>
<h1 id=""hello-include-file-a"">Hello Include File A</h1>
<p>This is a file A included by another file.</p>
";
            Assert.Equal(expected.Replace("\r\n", "\n"), result.Html);

            var dependency = result.Dependency;
            var expectedDependency = new List<string> { "a.md" };
            Assert.Equal(expectedDependency.ToImmutableList(), dependency);
        }

        [Fact]
        [Trait("Related", "Inclusion")]
        public void TestBlockLevelInclusion_CycleInclude()
        {
            var root = @"
# Hello World

Test Include File

[!include[refa](a.md)]

";

            var refa = @"
# Hello Include File A

This is a file A included by another file.

[!include[refb](b.md)]

";

            var refb = @"
# Hello Include File B

[!include[refa](a.md)]
";
            TestUtility.WriteToFile("r/root.md", root);
            TestUtility.WriteToFile("r/a.md", refa);
            TestUtility.WriteToFile("r/b.md", refb);

            var listener = TestLoggerListener.CreateLoggerListenerWithPhaseEqualFilter("CircularReferenceTest");
            Logger.RegisterListener(listener);
            using (new LoggerPhaseScope("CircularReferenceTest"))
            {
                var result = TestUtility.MarkupWithoutSourceInfo(root, "r/root.md");
                var expected = @"<h1 id=""hello-world"">Hello World</h1>
<p>Test Include File</p>
<h1 id=""hello-include-file-a"">Hello Include File A</h1>
<p>This is a file A included by another file.</p>
<h1 id=""hello-include-file-b"">Hello Include File B</h1>
[!include[refa](a.md)]";

                Assert.Equal(expected.Replace("\r\n", "\n"), result.Html);
            }
            Logger.UnregisterListener(listener);
            Assert.Collection(listener.Items, log => Assert.Equal(
                "Found circular reference: r/root.md --> ~/r/a.md --> ~/r/b.md --> ~/r/a.md",
                log.Message));
        }

        [Fact]
        [Trait("Related", "Inclusion")]
        public void TestInlineLevelInclusion_General()
        {
            var root = @"
# Hello World

Test Inline Included File: \\[!include[refa](~/r/a.md)].

Test Escaped Inline Included File: \[!include[refa](~/r/a.md)].
";

            var refa = "This is a **included** token";

            TestUtility.WriteToFile("r/root.md", root);
            TestUtility.WriteToFile("r/a.md", refa);

            var result = TestUtility.MarkupWithoutSourceInfo(root, "r/root.md"); ;
            var expected = @"<h1 id=""hello-world"">Hello World</h1>
<p>Test Inline Included File: \This is a <strong>included</strong> token.</p>
<p>Test Escaped Inline Included File: [!include<a href=""%7E/r/a.md"">refa</a>].</p>
";
            Assert.Equal(expected.Replace("\r\n", "\n"), result.Html);

            var dependency = result.Dependency;
            var expectedDependency = new List<string> { "a.md" };
            Assert.Equal(expectedDependency.ToImmutableList(), dependency);
        }

        [Fact]
        [Trait("Related", "Inclusion")]
        public void TestInlineLevelInclusion_CycleInclude()
        {
            var root = @"
# Hello World

Test Inline Included File: [!include[refa](~/r/a.md)].

";

            var refa = "This is a **included** token with [!include[root](~/r/root.md)]";

            TestUtility.WriteToFile("r/root.md", root);
            TestUtility.WriteToFile("r/a.md", refa);

            var result = TestUtility.MarkupWithoutSourceInfo(root, "r/root.md");
            var expected = @"<h1 id=""hello-world"">Hello World</h1>
<p>Test Inline Included File: This is a <strong>included</strong> token with [!include[root](~/r/root.md)].</p>
";

            Assert.Equal(expected.Replace("\r\n", "\n"), result.Html);
        }

        [Fact]
        [Trait("Related", "Inclusion")]
        public void TestInlineLevelInclusion_Block()
        {
            var root = @"
# Hello World

Test Inline Included File: [!include[refa](~/r/a.md)].

";

            var refa = @"## This is a included token

block content in Inline Inclusion.";

            TestUtility.WriteToFile("r/root.md", root);
            TestUtility.WriteToFile("r/a.md", refa);

            var result = TestUtility.MarkupWithoutSourceInfo(root, "r/root.md");
            var expected = @"<h1 id=""hello-world"">Hello World</h1>
<p>Test Inline Included File: ## This is a included tokenblock content in Inline Inclusion..</p>
";
            Assert.Equal(expected.Replace("\r\n", "\n"), result.Html);

            var dependency = result.Dependency;
            var expectedDependency = new List<string> { "a.md" };
            Assert.Equal(expectedDependency.ToImmutableList(), dependency);
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
            TestUtility.WriteToFile("r/root.md", root);

            TestUtility.WriteToFile("r/a/refc.md", refc);
            TestUtility.WriteToFile("r/b/linkAndRefRoot.md", linkAndRefRoot);
            TestUtility.WriteToFile("r/link/link2.md", link2);
            TestUtility.WriteToFile("r/c/c.md", c);
            TestUtility.WriteToFile("r/empty.md", string.Empty);
            var marked = TestUtility.MarkupWithoutSourceInfo(root, "r/root.md");
            var dependency = marked.Dependency;
            var expected = @"<p>Paragraph1
<a href=""%7E/r/b/a.md"">link</a>
<a href=""%7E/r/link/md/c.md"">link</a>
<img src=""%7E/r/b/img/img.jpg"" alt=""Image"" />
[!include[root](../root.md)]</p>
<p><strong>Hello</strong></p>
<p><strong>Hello</strong></p>
[!include[external](http://microsoft.com/a.md)]".Replace("\r\n", "\n");

            Assert.Equal(expected, marked.Html);
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
                dependency.OrderBy(x => x).ToArray());
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
            TestUtility.WriteToFile("r/r.md", r);
            TestUtility.WriteToFile("r/a/a.md", a);
            TestUtility.WriteToFile("r/b/token.md", token);
            TestUtility.WriteToFile("r/c/d/d.md", d);
            var marked = TestUtility.MarkupWithoutSourceInfo(a, "r/a/a.md");
            var expected = @"<p><img src=""%7E/r/img/img.jpg"" alt="""" />
<a href=""#anchor""></a>
<a href=""%7E/r/a/a.md"">a</a>
<a href=""%7E/r/b/invalid.md""></a>
<a href=""%7E/r/c/d/d.md#anchor"">d</a></p>".Replace("\r\n", "\n") + "\n";
            var dependency = marked.Dependency;
            Assert.Equal(expected, marked.Html);
            Assert.Equal(
                new[] { "../b/token.md" },
                dependency.OrderBy(x => x).ToArray());

            marked = TestUtility.MarkupWithoutSourceInfo(d, "r/c/d/d.md");
            dependency = marked.Dependency;
            Assert.Equal(expected, marked.Html);
            Assert.Equal(
                new[] { "../../b/token.md" },
                dependency.OrderBy(x => x).ToArray());

            dependency.Clear();
            marked = TestUtility.MarkupWithoutSourceInfo(r, "r/r.md");
            dependency = marked.Dependency;
            Assert.Equal($@"{expected}{expected}", marked.Html);
            Assert.Equal(
                new[] { "a/a.md", "b/token.md", "c/d/d.md" },
                dependency.OrderBy(x => x).ToArray());
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
            TestUtility.WriteToFile("r/root.md", root);
            TestUtility.WriteToFile("r/b/linkAndRefRoot.md", linkAndRefRoot);
            var marked = TestUtility.MarkupWithoutSourceInfo(root, "r/root.md");
            var expected = @"<p>Paragraph1</p>" + "\n";
            Assert.Equal(expected, marked.Html);
        }

        #region Fallback folders testing

        [Fact(Skip = "won't support")]
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

            TestUtility.WriteToFile($"{uniqueFolderName}/root_folder_{uniqueFolderName}/root_{uniqueFolderName}.md", root);
            TestUtility.WriteToFile($"{uniqueFolderName}/root_folder_{uniqueFolderName}/a_folder_{uniqueFolderName}/a_{uniqueFolderName}.md", a);
            TestUtility.WriteToFile($"{uniqueFolderName}/root_folder_{uniqueFolderName}/token_folder_{uniqueFolderName}/token1_{uniqueFolderName}.md", token1);
            TestUtility.WriteToFile($"{uniqueFolderName}/fallback_folder_{uniqueFolderName}/token_folder_{uniqueFolderName}/token2_{uniqueFolderName}.md", token2);

            var fallbackFolders = new List<string> { { Path.Combine(Directory.GetCurrentDirectory(), $"{uniqueFolderName}/fallback_folder_{uniqueFolderName}") } };
            var parameter = new MarkdownServiceParameters
            {
                BasePath = "."
            };
            var service = new MarkdigMarkdownService(parameter);
            //var marked = service.Markup(Path.Combine(Directory.GetCurrentDirectory(), $"{uniqueFolderName}/root_folder_{uniqueFolderName}"), root, fallbackFolders, $"root_{uniqueFolderName}.md");
            var marked = service.Markup("place", "holder");
            var dependency = marked.Dependency;

            Assert.Equal($@"<p>1markdown root.md main content start.</p>
<p>1markdown a.md main content start.</p>
<p>1markdown token1.md content start.</p>
<p><strong>1markdown token2.md main content</strong></p>
<p>markdown token1.md content end.</p>
<p><strong>1markdown token2.md main content</strong></p>
<p>markdown a.md main content end.</p>
<p>markdown root.md main content end.</p>
".Replace("\r\n", "\n"), marked.Html);
            Assert.Equal(
                new[] { $"../fallback_folder_{uniqueFolderName}/token_folder_{uniqueFolderName}/token2_{uniqueFolderName}.md", $"a_folder_{uniqueFolderName}/a_{uniqueFolderName}.md", $"token_folder_{uniqueFolderName}/token1_{uniqueFolderName}.md", $"token_folder_{uniqueFolderName}/token2_{uniqueFolderName}.md" },
                dependency.OrderBy(x => x).ToArray());
        }

        [Fact(Skip = "won't support")]
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
            TestUtility.WriteToFile($"{uniqueFolderName}/root_folder/root.md", root);
            TestUtility.WriteToFile($"{uniqueFolderName}/root_folder/a_folder/a.md", a);
            TestUtility.WriteToFile($"{uniqueFolderName}/root_folder/code_folder/sample1.cs", sample1);
            TestUtility.WriteToFile($"{uniqueFolderName}/fallback_folder/a_folder/code_in_a.cs", code_in_a);
            TestUtility.WriteToFile($"{uniqueFolderName}/fallback_folder/code_folder/sample2.cs", sample2);

            var fallbackFolders = new List<string> { { Path.Combine(Directory.GetCurrentDirectory(), $"{uniqueFolderName}/fallback_folder") } };

            // Verify root.md markup result
            var parameter = new MarkdownServiceParameters
            {
                BasePath = "."
            };
            var service = new MarkdigMarkdownService(parameter);
            //var rootMarked = service.Markup(Path.Combine(Directory.GetCurrentDirectory(), $"{uniqueFolderName}/root_folder"), root, fallbackFolders, "root.md");
            var rootMarked = service.Markup("place", "holder");
            var rootDependency = rootMarked.Dependency;
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
".Replace("\r\n", "\n"), rootMarked.Html);
            Assert.Equal(
                new[] { "../fallback_folder/a_folder/code_in_a.cs", "../fallback_folder/code_folder/sample2.cs", "a_folder/a.md", "a_folder/code_in_a.cs", "code_folder/sample1.cs", "code_folder/sample2.cs" },
                rootDependency.OrderBy(x => x).ToArray());

            // Verify a.md markup result
            //var aMarked = service.Markup(Path.Combine(Directory.GetCurrentDirectory(), $"{uniqueFolderName}/root_folder"), a, fallbackFolders, "a_folder/a.md");
            var aMarked = service.Markup("place", "holder");
            var aDependency = aMarked.Dependency;
            Assert.Equal(@"<p>markdown a.md main content start.</p>
<p>code_in_a code in a.md content start</p>
<pre><code class=""lang-cs"" name=""this is code_in_a code"">namespace code_in_a{}
</code></pre><p>code_in_a in a.md content end</p>
<p>markdown a.md a.md content end.</p>
".Replace("\r\n", "\n"), aMarked.Html);
            Assert.Equal(
                new[] { "../../fallback_folder/a_folder/code_in_a.cs", "code_in_a.cs" },
                aDependency.OrderBy(x => x).ToArray());
        }

        #endregion

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

            var marked = TestUtility.MarkupWithoutSourceInfo(root, "root.md");
            var dependency = marked.Dependency;
            var expected = "<p>Inline ## Inline inclusion do not parse header [!include[root](root.md)]\nInline <strong>Hello</strong></p>\n";

            Assert.Equal(expected, marked.Html);
            Assert.Equal(
                new[] { "ref1.md", "ref2.md", "ref3.md", "root.md" },
                dependency.OrderBy(x => x).ToArray());
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
<p>inc2 <a href=""inc1.md"">Resource Manager model</a>.</p>
<p>inc3</p>
";

            var inc1 = @"inc1";
            var inc2 = @"inc2";
            var inc3 = @"inc3";
            File.WriteAllText("root.md", root);
            File.WriteAllText("inc1.md", inc1);
            File.WriteAllText("inc2.md", inc2);
            File.WriteAllText("inc3.md", inc3);

            var marked = TestUtility.MarkupWithoutSourceInfo(root, "root.md");
            var dependency = marked.Dependency;
            Assert.Equal(expected.Replace("\r\n", "\n"), marked.Html);
            Assert.Equal(
              new[] { "inc1.md", "inc2.md", "inc3.md" },
              dependency.OrderBy(x => x).ToArray());
        }

        [Fact]
        [Trait("BugItem", "1101156")]
        [Trait("Related", "Inclusion")]
        public void TestBlockInclude_ImageRelativePath()
        {
            var root = @"
# Hello World

Test Include File

[!include[refa](../../include/a.md)]

";

            var refa = @"
# Hello Include File A

![img](./media/refb.png)
";

            var rootPath = "r/parent_folder/child_folder/root.md";
            TestUtility.WriteToFile(rootPath, root);
            TestUtility.WriteToFile("r/include/a.md", refa);

            var result = TestUtility.MarkupWithoutSourceInfo(root, rootPath);
            var expected = @"<h1 id=""hello-world"">Hello World</h1>
<p>Test Include File</p>
<h1 id=""hello-include-file-a"">Hello Include File A</h1>
<p><img src=""%7E/r/include/media/refb.png"" alt=""img"" /></p>
";
            Assert.Equal(expected.Replace("\r\n", "\n"), result.Html);

            var dependency = result.Dependency;
            var expectedDependency = new List<string> { "../../include/a.md" };
            Assert.Equal(expectedDependency.ToImmutableList(), dependency);
        }

        [Fact]
        [Trait("Related", "Inclusion")]
        public void TestBlockInclude_WithYamlHeader()
        {
            var root = @"
# Hello World

Test Include File

[!include[refa](../../include/a.md)]

";

            var refa = @"---
a: b
---
body";

            var rootPath = "r/parent_folder/child_folder/root.md";
            TestUtility.WriteToFile(rootPath, root);
            TestUtility.WriteToFile("r/include/a.md", refa);

            var result = TestUtility.MarkupWithoutSourceInfo(root, rootPath);
            var expected = @"<h1 id=""hello-world"">Hello World</h1>
<p>Test Include File</p>
<p>body</p>
";
            Assert.Equal(expected.Replace("\r\n", "\n"), result.Html);

            var dependency = result.Dependency;
            var expectedDependency = new List<string> { "../../include/a.md" };
            Assert.Equal(expectedDependency.ToImmutableList(), dependency);
        }

        [Fact]
        [Trait("Related", "Inclusion")]
        public void TestBlockInclude_Does_Not_Replace_AutoLink()
        {
            var root = "https://docs.microsoft.com";
            var context = new MarkdownContext(getLink: (path, relativeTo, resultRelativeTo) => "REPLACE IT");
            var pipeline = new MarkdownPipelineBuilder().UseDocfxExtensions(context).Build();
            var result = Markdown.ToHtml(root, pipeline);

            Assert.Equal("<p><a href=\"https://docs.microsoft.com\">https://docs.microsoft.com</a></p>", result.Trim());
        }

        [Fact]
        [Trait("Related", "Inclusion")]
        public void TestInclusionContext_CurrentFile_RootFile()
        {
            var root = "[!include[](embed)]";

            var context = new MarkdownContext(
                readFile: (path, relativeTo) =>
                {
                    Assert.Equal("embed", path);
                    Assert.Equal("root", relativeTo);

                    Assert.Equal("root", InclusionContext.RootFile);
                    Assert.Equal("root", InclusionContext.File);

                    return ("embed [content](c.md)", "embed");
                },
                getLink: (path, relativeTo, resultRelativeTo) =>
                {
                    Assert.Equal("c.md", path);
                    Assert.Equal("embed", relativeTo);
                    Assert.Equal("root", resultRelativeTo);

                    Assert.Equal("root", InclusionContext.RootFile);
                    Assert.Equal("embed", InclusionContext.File);

                    return "2333";
                });

            var pipeline = new MarkdownPipelineBuilder().UseDocfxExtensions(context).Build();

            Assert.Equal(null, InclusionContext.RootFile);
            Assert.Equal(null, InclusionContext.File);

            using (InclusionContext.PushFile("root"))
            {
                Assert.Equal("root", InclusionContext.RootFile);
                Assert.Equal("root", InclusionContext.File);

                var result = Markdown.ToHtml(root, pipeline);

                Assert.Equal("<p>embed <a href=\"2333\">content</a></p>", result.Trim());
                Assert.Equal("root", InclusionContext.RootFile);
                Assert.Equal("root", InclusionContext.File);
            }
            Assert.Equal(null, InclusionContext.RootFile);
            Assert.Equal(null, InclusionContext.File);
        }
    }
}
