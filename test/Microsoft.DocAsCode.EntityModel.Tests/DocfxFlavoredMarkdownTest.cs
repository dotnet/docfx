// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.Tests
{
    using System.Collections.Generic;
    using System.IO;
    using System.Text.RegularExpressions;
    using System.Linq;
    using System.Xml;

    using Xunit;

    using Microsoft.DocAsCode.EntityModel.MarkdownValidators;

    public class DocfxFlavoredMarkdownTest
    {
        [Theory]
        [Trait("Related", "DfmMarkdown")]
        [InlineData("", "")]
        [InlineData("<address@example.com>", "<p><a href=\"mailto:address@example.com\">address@example.com</a></p>\n")]
        [InlineData(@"<Insert OneGet Details - meeting on 10/30 for details.>", @"<Insert OneGet Details - meeting on 10/30 for details.>")]
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
        [InlineData("a\n```\nc\n```",
            "<p>a</p>\n<pre><code>c\n</code></pre>")]
        [InlineData(@"* Unordered list item 1
* Unordered list item 2
1. This Is Heading, Not Ordered List
-------------------------------------
", "<ul>\n<li>Unordered list item 1</li>\n<li>Unordered list item 2</li>\n</ul>\n<h2 id=\"1-this-is-heading-not-ordered-list\">1. This Is Heading, Not Ordered List</h2>\n")]
        [InlineData(@" @api__1",
            "<p> <xref href=\"api__1\"></xref></p>\n")]
        [InlineData(@"@api1 @api__1 @api!1 @api@a abc@api.com a.b.c@api.com @'a p ';@""a!pi"",@api...@api",
            "<p><xref href=\"api1\"></xref> <xref href=\"api__1\"></xref> <xref href=\"api!1\"></xref> <xref href=\"api@a\"></xref> abc@api.com a.b.c@api.com <xref href=\"a p \"></xref>;<xref href=\"a!pi\"></xref>,<xref href=\"api\"></xref>...<xref href=\"api\"></xref></p>\n")]
        public void TestDfmInGeneral(string source, string expected)
        {
            Assert.Equal(expected, DocfxFlavoredMarked.Markup(source));
        }

        [Fact]
        [Trait("Related", "DfmMarkdown")]
        public void TestBlockLevelInclusion()
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
[!include[linkAndRefRoot](b/linkAndRefRoot.md)]
[!include[refc](a/refc.md ""This is root"")]
[!include[refc_using_cache](a/refc.md)]
[!include[empty](empty.md)]
[!include[external](http://microsoft.com/a.md)]";

            var linkAndRefRoot = @"
Paragraph1
[link](a.md)
[!include[link2](../link/link2.md)]
![Image](img/img.jpg)
[!include[root](../root.md)]";
            var link2 = @"[link](md/c.md)";
            var refc = @"[!include[c](../c/c.md ""This is root"")]";
            var c = @"**Hello**";
            WriteToFile("r/root.md", root);

            WriteToFile("r/a/refc.md", refc);
            WriteToFile("r/b/linkAndRefRoot.md", linkAndRefRoot);
            WriteToFile("r/link/link2.md", link2);
            WriteToFile("r/c/c.md", c);
            WriteToFile("r/empty.md", string.Empty);
            var marked = DocfxFlavoredMarked.Markup(root, Path.GetFullPath("r/root.md"));
            Assert.Equal("<!-- BEGIN INCLUDE: Include content from &quot;r/b/linkAndRefRoot.md&quot; --><p>Paragraph1\n<a href=\"b/a.md\">link</a>\n<!-- BEGIN INCLUDE: Include content from &quot;r/link/link2.md&quot; --><a href=\"link/md/c.md\">link</a><!--END INCLUDE -->\n<img src=\"b/img/img.jpg\" alt=\"Image\">\n<!-- BEGIN ERROR INCLUDE: Unable to resolve [!include[root](../root.md)]: Circular dependency found in &quot;r/b/linkAndRefRoot.md&quot; -->[!include[root](../root.md)]<!--END ERROR INCLUDE --></p>\n<!--END INCLUDE --><!-- BEGIN INCLUDE: Include content from &quot;r/a/refc.md&quot; --><!-- BEGIN INCLUDE: Include content from &quot;r/c/c.md&quot; --><p><strong>Hello</strong></p>\n<!--END INCLUDE --><!--END INCLUDE --><!-- BEGIN INCLUDE: Include content from &quot;r/a/refc.md&quot; --><!-- BEGIN INCLUDE: Include content from &quot;r/c/c.md&quot; --><p><strong>Hello</strong></p>\n<!--END INCLUDE --><!--END INCLUDE --><!-- BEGIN INCLUDE: Include content from &quot;r/empty.md&quot; --><!--END INCLUDE --><!-- BEGIN ERROR INCLUDE: Absolute path &quot;http://microsoft.com/a.md&quot; is not supported. -->[!include[external](http://microsoft.com/a.md)]<!--END ERROR INCLUDE -->", marked);
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
            var ref3 = @"**Hello**";
            File.WriteAllText("root.md", root);
            File.WriteAllText("ref1.md", ref1);
            File.WriteAllText("ref2.md", ref2);
            File.WriteAllText("ref3.md", ref3);

            var marked = DocfxFlavoredMarked.Markup(root, "root.md");
            Assert.Equal("<p>Inline <!-- BEGIN INCLUDE: Include content from &quot;ref1.md&quot; --><!-- BEGIN INCLUDE: Include content from &quot;ref2.md&quot; -->## Inline inclusion do not parse header <!-- BEGIN ERROR INCLUDE: Unable to resolve [!include[root](root.md &quot;This is root&quot;)]: Circular dependency found in &quot;ref2.md&quot; -->[!include[root](root.md \"This is root\")]<!--END ERROR INCLUDE --><!--END INCLUDE --><!--END INCLUDE -->\nInline <!-- BEGIN INCLUDE: Include content from &quot;ref3.md&quot; --><strong>Hello</strong><!--END INCLUDE --></p>\n", marked);
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

Skip the note
", @"<p>the following is note type</p>
<blockquote class=""NOTE""><p>note text 1-1
note text 1-2<br>note text 2-1
This is also note<br>This is also note with br</p>
</blockquote><p>Skip the note</p>
")]
        [InlineData(@"the following is not note type
  > no-note text 1-1
  > [!NOTE]
  > no-note text 1-2  
  > no-note text 2-1
", @"<p>the following is not note type</p>
<blockquote>
<p>no-note text 1-1
[!NOTE]
no-note text 1-2<br>no-note text 2-1</p>
</blockquote>
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
<p>[!NOTE]
</p>
<p>no-note text 2-1<br>no-note text 2-2</p>
</blockquote>
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
        public void TestSectionNoteInBlockQuote(string source, string expected)
        {
            var markedContent = DocfxFlavoredMarked.Markup(source);
            Assert.Equal(expected.Replace("\r\n", "\n"), markedContent);
        }


        [Theory]
        [Trait("Related", "DfmMarkdown")]
        [InlineData(@"<!-- BEGINSECTION class=""All"" id=""All"" -->

<!-- BEGINSECTION class=""A"" id=""A"" -->

this is A

<!-- ENDSECTION -->

<!-- BEGINSECTION class=""B"" id=""B"" -->

this is B

<!-- ENDSECTION -->

<!-- ENDSECTION -->")]
        public void TestSectionBlockLevelRecursive(string source)
        {
            var markedContent = DocfxFlavoredMarked.Markup(source);
            Assert.Equal("<div class=\"All\" id=\"All\"><div class=\"A\" id=\"A\"><p>this is A</p>\n</div><div class=\"B\" id=\"B\"><p>this is B</p>\n</div></div>", markedContent);
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

        //[Fact]
        [Trait("Related", "DfmMarkdown")]
        public void TestDfmTagValidate()
        {
            var builder = new DfmEngineBuilder(new MarkdownLite.Options() { Mangle = false });
            var mrb = new MarkdownRewriterBuilder(null);
            mrb.AddValidators(
                new MarkdownTagValidationRule
                {
                    TagNames = new List<string> { "em", "div" },
                    MessageFormatter = "Invalid tag({0})!",
                    Behavior = TagRewriteBehavior.Error,
                    OpeningTagOnly = true,
                },
                new MarkdownTagValidationRule
                {
                    TagNames = new List<string> { "h1" },
                    MessageFormatter = "Invalid tag({0}), Removed!",
                    Behavior = TagRewriteBehavior.ErrorAndRemove,
                });
            builder.Rewriter = mrb.Create();

            var engine = builder.CreateDfmEngine(new DfmRenderer());
            var listener = new TestLoggerListener("test!!!!");
            Logger.RegisterListener(listener);
            string result;
            using (new LoggerPhaseScope("test!!!!"))
            {
                result = engine.Markup(@"<div><i>x</i><EM>y</EM><h1>z</h1></div>", "test");
            }
            Logger.UnregisterListener(listener);
            Assert.Equal("<div><i>x</i><EM>y</EM>z</div>", result);
            Assert.Equal(4, listener.Items.Count);
            Assert.Equal(new[] { "Invalid tag(div)!", "Invalid tag(EM)!", "Invalid tag(h1), Removed!", "Invalid tag(h1), Removed!" }, from item in listener.Items select item.Message);
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
            var marked = DocfxFlavoredMarked.Markup(root, Path.GetFullPath("api.json"));
            Assert.Equal("<pre><code class=\"language-FakeREST\" name=\"REST\">\n{\n   &quot;method&quot;: &quot;GET&quot;,\n   &quot;resourceFormat&quot;: &quot;https://outlook.office.com/api/v1.0/me/events?$select=Subject,Organizer,Start,End&quot;,\n   &quot;requestUrl&quot;: &quot;https://outlook.office.com/api/v1.0/me/events?$select=Subject,Organizer,Start,End&quot;,\n   &quot;requestHeaders&quot;: {\n                &quot;Accept&quot;: &quot;application/json&quot;\n   }\n}\n</code></pre><pre><code class=\"language-FakeREST-i\" name=\"REST-i\" title=\"This is root\">\n{\n   &quot;method&quot;: &quot;GET&quot;,\n   &quot;resourceFormat&quot;: &quot;https://outlook.office.com/api/v1.0/me/events?$select=Subject,Organizer,Start,End&quot;,\n   &quot;requestUrl&quot;: &quot;https://outlook.office.com/api/v1.0/me/events?$select=Subject,Organizer,Start,End&quot;,\n   &quot;requestHeaders&quot;: {\n                &quot;Accept&quot;: &quot;application/json&quot;\n   }\n}\n</code></pre><pre><code name=\"No Language\">\n{\n   &quot;method&quot;: &quot;GET&quot;,\n   &quot;resourceFormat&quot;: &quot;https://outlook.office.com/api/v1.0/me/events?$select=Subject,Organizer,Start,End&quot;,\n   &quot;requestUrl&quot;: &quot;https://outlook.office.com/api/v1.0/me/events?$select=Subject,Organizer,Start,End&quot;,\n   &quot;requestHeaders&quot;: {\n                &quot;Accept&quot;: &quot;application/json&quot;\n   }\n}\n</code></pre><pre><code class=\"language-js\" name=\"empty\">\n{\n   &quot;method&quot;: &quot;GET&quot;,\n   &quot;resourceFormat&quot;: &quot;https://outlook.office.com/api/v1.0/me/events?$select=Subject,Organizer,Start,End&quot;,\n   &quot;requestUrl&quot;: &quot;https://outlook.office.com/api/v1.0/me/events?$select=Subject,Organizer,Start,End&quot;,\n   &quot;requestHeaders&quot;: {\n                &quot;Accept&quot;: &quot;application/json&quot;\n   }\n}\n</code></pre>", marked);
        }

        [Theory]
        [Trait("Owner", "humao")]
        [Trait("Related", "DfmMarkdown")]
        [InlineData(@"[!code-csharp[Main](Program.cs)]", @"<pre><code class=""language-csharp"" name=""Main"">namespace ConsoleApplication1
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
            string s = &quot;test&quot;;
            int i = 100;
        }
    }
    // &lt;/snippetprogram&gt;
}
</code></pre>")]
        [InlineData(@"[!code[Main](Program.cs#L12-L16 ""This is root"")]", @"<pre><code name=""Main"" title=""This is root"">        static void Main(string[] args)
        {
            string s = &quot;test&quot;;
            int i = 100;
        }
</code></pre>")]
        [InlineData(@"[!code[Main](Program.cs#L12-L100 ""This is root"")]", @"<pre><code name=""Main"" title=""This is root"">        static void Main(string[] args)
        {
            string s = &quot;test&quot;;
            int i = 100;
        }
    }
    // &lt;/snippetprogram&gt;
}
</code></pre>")]
        [InlineData(@"[!code-csharp[Main](Program.cs#namespace ""This is root"")]", @"<pre><code class=""language-csharp"" name=""Main"" title=""This is root"">    using System;
    using System.Collections.Generic;
    using System.IO;
</code></pre>")]
        [InlineData(@"[!code-csharp[Main](Program.cs#NAMESPACE ""This is root"")]", @"<pre><code class=""language-csharp"" name=""Main"" title=""This is root"">    using System;
    using System.Collections.Generic;
    using System.IO;
</code></pre>")]
        [InlineData(@"[!code-csharp[Main](Program.cs#program ""This is root"")]", @"<pre><code class=""language-csharp"" name=""Main"" title=""This is root"">    class Program
    {
        static void Main(string[] args)
        {
            string s = &quot;test&quot;;
            int i = 100;
        }
    }
</code></pre>")]
        [InlineData(@"[!code-csharp[Main](Program.cs#snippetprogram ""This is root"")]", @"<pre><code class=""language-csharp"" name=""Main"" title=""This is root"">    class Program
    {
        static void Main(string[] args)
        {
            string s = &quot;test&quot;;
            int i = 100;
        }
    }
</code></pre>")]
        [InlineData(@"[!code-csharp[Main](Program.cs?name=namespace ""This is root"")]", @"<pre><code class=""language-csharp"" name=""Main"" title=""This is root"">    using System;
    using System.Collections.Generic;
    using System.IO;
</code></pre>")]
        [InlineData(@"[!code[Main](Program.cs?start=5&end=9 ""This is root"")]", @"<pre><code name=""Main"" title=""This is root"">    using System.Collections.Generic;
    using System.IO;
    // &lt;/namespace&gt;

    // &lt;snippetprogram&gt;
</code></pre>")]
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
            string s = ""test"";
            int i = 100;
        }
    }
    // </snippetprogram>
}";
            File.WriteAllText("Program.cs", content.Replace("\r\n", "\n"));

            // act
            var marked = DocfxFlavoredMarked.Markup(fencesPath, Path.GetFullPath("Program.cs"));

            // assert
            Assert.Equal(expectedContent.Replace("\r\n", "\n"), marked);
        }

        private static void WriteToFile(string file, string content)
        {
            var dir = Path.GetDirectoryName(file);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(file, content);
        }
    }
}
