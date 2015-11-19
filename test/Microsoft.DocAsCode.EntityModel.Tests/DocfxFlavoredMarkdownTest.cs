// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.Tests
{
    using System.Xml;
    using System.IO;
    using Xunit;

    public class DocfxFlavoredMarkdownTest
    {
        [Theory]
        [Trait("Related", "DfmMarkdown")]
        [InlineData("", "")]
        [InlineData("<address@example.com>", "<p><a href=\"mailto:address@example.com\">address@example.com</a></p>\n")]
        [InlineData(@"<Insert OneGet Deatils - meeting on 10/30 for details.>", @"<Insert OneGet Deatils - meeting on 10/30 for details.>")]
        [InlineData("<http://example.com/>", "<p><a href=\"http://example.com/\">http://example.com/</a></p>\n")]
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
            Assert.Equal(expected, DocfxFlavoredMarked.Markup(source));
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
[!inc[external](http://microsoft.com/a.md)]";

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
            Assert.Equal("<!-- BEGIN INC: Include content from &quot;r/b/linkAndRefRoot.md&quot; --><p>Paragraph1\n<a href=\"b/a.md\">link</a>\n<!-- BEGIN INC: Include content from &quot;r/link/link2.md&quot; --><a href=\"link/md/c.md\">link</a><!--END INC -->\n<img src=\"b/img/img.jpg\" alt=\"Image\">\n<!-- BEGIN ERROR INC: Unable to resolve [!inc[root](../root.md)]: Circular dependency found in &quot;r/b/linkAndRefRoot.md&quot; -->[!inc[root](../root.md)]<!--END ERROR INC --></p>\n<!--END INC --><!-- BEGIN INC: Include content from &quot;r/a/refc.md&quot; --><!-- BEGIN INC: Include content from &quot;r/c/c.md&quot; --><p><strong>Hello</strong></p>\n<!--END INC --><!--END INC --><!-- BEGIN INC: Include content from &quot;r/a/refc.md&quot; --><!-- BEGIN INC: Include content from &quot;r/c/c.md&quot; --><p><strong>Hello</strong></p>\n<!--END INC --><!--END INC --><!-- BEGIN INC: Include content from &quot;r/empty.md&quot; --><!--END INC --><!-- BEGIN ERROR INC: Absolute path &quot;http://microsoft.com/a.md&quot; is not supported. -->[!inc[external](http://microsoft.com/a.md)]<!--END ERROR INC -->", marked);
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
            Assert.Equal("<p>Inline <!-- BEGIN INC: Include content from &quot;ref1.md&quot; --><!-- BEGIN INC: Include content from &quot;ref2.md&quot; -->## Inline inclusion do not parse header <!-- BEGIN ERROR INC: Unable to resolve [!inc[root](root.md &quot;This is root&quot;)]: Circular dependency found in &quot;ref2.md&quot; -->[!inc[root](root.md \"This is root\")]<!--END ERROR INC --><!--END INC --><!--END INC -->\nInline <!-- BEGIN INC: Include content from &quot;ref3.md&quot; --><strong>Hello</strong><!--END INC --></p>\n", marked);
        }

        [Theory]
        [Trait("Related", "DfmMarkdown")]
        [InlineData(@"<!-- BEGINSECTION class=""tabbedCodeSnippets"" data-resources=""OutlookServices.Calendar"" -->

```cs-i
    var outlookClient = await CreateOutlookClientAsync(""Calendar"");
    var events = await outlookClient.Me.Events.Take(10).ExecuteAsync();
            foreach (var calendarEvent in events.CurrentPage)
            {
                System.Diagnostics.Debug.WriteLine(""Event '{0}'."", calendarEvent.Subject);
            }
```

```javascript-i
outlookClient.me.events.getEvents().fetch().then(function(result) {
        result.currentPage.forEach(function(event) {
        console.log('Event ""' + event.subject + '""')
    });
}, function(error)
    {
        console.log(error);
    });
```

<!-- ENDSECTION -->")]
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
    }
}
