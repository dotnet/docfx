// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.Tests
{
    using MarkdownLite;
    using System;
    using System.Diagnostics;
    using System.IO;
    using Xunit;

    public class DocfxFlavoredMarkdownTest
    {
        [Theory]
        [Trait("Related", "DfmMarkdown")]
        [InlineData("", "")]
        [InlineData("# Hello World", "<h1 id=\"hello-world\">Hello World</h1>\n")]
        [InlineData("Hot keys: <kbd>Ctrl+[</kbd> and <kbd>Ctrl+]</kbd>", "<p>Hot keys: <kbd>Ctrl+[</kbd> and <kbd>Ctrl+]</kbd></p>\n")]
        [InlineData("<div>Some text here</div>", "<div>Some text here</div>")]
        [InlineData(@"---
a: b
b:
  c: e
---", "<yamlheader>a: b\nb:\n  c: e</yamlheader>")]
        [InlineData(@"# Hello @CrossLink1 @'CrossLink2'dummy 
@World",
            "<h1 id=\"hello-crosslink1-crosslink2-dummy\">Hello <xref href=\"CrossLink1\"></xref> <xref href=\"CrossLink2\"></xref>dummy</h1>\n<p><xref href=\"World\"></xref></p>\n")]
        public void Parse(string source, string expected)
        {
            var options = new DocfxFlavoredOptions();
            Assert.Equal(expected, DocfxFlavoredMarked.Markup(source, options: options));
        }

        [Fact]
        [Trait("Related", "DfmMarkdown")]
        public void TestInclusion_BlockLevel()
        {
            // -r
            //  |- root.md
            //  |- empty.md
            //  |- a
            //  |  |- root.md
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
[!inc[linkAndRefRoot](b/linkAndRefRoot.md)]
[!inc[refc](a/refc.md ""This is root"")]
[!inc[refc_using_cache](a/refc.md)]
[!inc[empty](empty.md)]
[!inc[external](http://microsoft.com/a.md)]

";

            var linkAndRefRoot = @"
Paragraph1
[link](a.md)
[!inc[link2](../link/link2.md)]
![Image](img/img.jpg)
[!inc[root](../root.md)]";
            var link2 = @"[link](md/c.md)";
            var refc = @"[!inc[c](../c/c.md ""This is root"")]";
            var c = @"**Hello**";
            WriteToFile("r/root.md", root);

            WriteToFile("r/a/refc.md", refc);
            WriteToFile("r/b/linkAndRefRoot.md", linkAndRefRoot);
            WriteToFile("r/link/link2.md", link2);
            WriteToFile("r/c/c.md", c);
            WriteToFile("r/empty.md", string.Empty);
            var marked = DocfxFlavoredMarked.Markup(root, Path.GetFullPath("r/root.md"));
            Assert.Equal("<p>Paragraph1\n<a href=\"b/a.md\">link</a>\n<a href=\"link/md/c.md\">link</a>\n<img src=\"b/img/img.jpg\" alt=\"Image\">\n<error>Unable to resolve &quot;[!inc[root](r/root.md)]&quot;: Circular dependency found in &quot;r/b/linkAndRefRoot.md&quot;</error></p>\n<p><strong>Hello</strong></p>\n<p><strong>Hello</strong></p>\n<pre><error>Absolute path &quot;http://microsoft.com/a.md&quot; is not supported.</error></pre>", marked);
        }

        private static void WriteToFile(string file, string content)
        {
            var dir = Path.GetDirectoryName(file);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(file, content);
        }

        [Fact]
        [Trait("Related", "DfmMarkdown")]
        public void TestInclusion_InlineLevel()
        {
            // 1. Prepare data
            var root = @"
Inline [!inc[ref1](ref1.md ""This is root"")]
Inline [!inc[ref3](ref3.md ""This is root"")]
";

            var ref1 = @"[!inc[ref2](ref2.md ""This is root"")]";
            var ref2 = @"## Inline inclusion do not parse header [!inc[root](root.md ""This is root"")]";
            var ref3 = @"**Hello**";
            File.WriteAllText("root.md", root);
            File.WriteAllText("ref1.md", ref1);
            File.WriteAllText("ref2.md", ref2);
            File.WriteAllText("ref3.md", ref3);

            var marked = DocfxFlavoredMarked.Markup(root, "root.md");
            Assert.Equal("<p>Inline ## Inline inclusion do not parse header <error>Unable to resolve &quot;[!inc[root](root.md &#39;This is root&#39;)]&quot;: Circular dependency found in &quot;ref2.md&quot;</error>\nInline <strong>Hello</strong></p>\n", marked);
        }
    }
}
