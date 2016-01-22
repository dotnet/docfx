// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.Tests
{
    using System.Collections.Generic;
    using System.Composition.Hosting;
    using System.IO;
    using System.Linq;
    using System.Xml;

    using Xunit;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Dfm;
    using Microsoft.DocAsCode.Dfm.MarkdownValidators;
    using Microsoft.DocAsCode.MarkdownLite;

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
            "<h1 id=\"hello-crosslink1-crosslink2-dummy\">Hello <xref href=\"CrossLink1\" data-throw-if-not-resolved=\"False\" data-raw=\"@CrossLink1\"></xref> <xref href=\"CrossLink2\" data-throw-if-not-resolved=\"False\" data-raw=\"@&#39;CrossLink2&#39;\"></xref>dummy</h1>\n<p><xref href=\"World\" data-throw-if-not-resolved=\"False\" data-raw=\"@World\"></xref></p>\n")]
        [InlineData("a\n```\nc\n```",
            "<p>a</p>\n<pre><code>c\n</code></pre>")]
        [InlineData(@"* Unordered list item 1
* Unordered list item 2
1. This Is Heading, Not Ordered List
-------------------------------------
", "<ul>\n<li>Unordered list item 1</li>\n<li>Unordered list item 2</li>\n</ul>\n<h2 id=\"1-this-is-heading-not-ordered-list\">1. This Is Heading, Not Ordered List</h2>\n")]
        [InlineData(@" *hello* abc @api__1",
            "<p> <em>hello</em> abc <xref href=\"api__1\" data-throw-if-not-resolved=\"False\" data-raw=\"@api__1\"></xref></p>\n")]
        [InlineData("@1abc", "<p>@1abc</p>\n")]
        [InlineData(@"@api1 @api__1 @api!1 @api@a abc@api.com a.b.c@api.com @'a p ';@""a!pi"",@api...@api",
            "<p><xref href=\"api1\" data-throw-if-not-resolved=\"False\" data-raw=\"@api1\"></xref> <xref href=\"api__1\" data-throw-if-not-resolved=\"False\" data-raw=\"@api__1\"></xref> <xref href=\"api!1\" data-throw-if-not-resolved=\"False\" data-raw=\"@api!1\"></xref> <xref href=\"api@a\" data-throw-if-not-resolved=\"False\" data-raw=\"@api@a\"></xref> abc@api.com a.b.c@api.com <xref href=\"a p \" data-throw-if-not-resolved=\"False\" data-raw=\"@&#39;a p &#39;\"></xref>;<xref href=\"a!pi\" data-throw-if-not-resolved=\"False\" data-raw=\"@&quot;a!pi&quot;\"></xref>,<xref href=\"api\" data-throw-if-not-resolved=\"False\" data-raw=\"@api\"></xref>...<xref href=\"api\" data-throw-if-not-resolved=\"False\" data-raw=\"@api\"></xref></p>\n")]
        [InlineData("[name](xref:uid \"title\")", "<p><xref href=\"uid\" title=\"title\" data-throw-if-not-resolved=\"True\" data-raw=\"[name](xref:uid &quot;title&quot;)\">name</xref></p>\n")]
        [InlineData("[name](@uid \"title\")", "<p><xref href=\"uid\" title=\"title\" data-throw-if-not-resolved=\"True\" data-raw=\"[name](@uid &quot;title&quot;)\">name</xref></p>\n")]
        [InlineData("<xref:uid>text", "<p><xref href=\"uid\" data-throw-if-not-resolved=\"True\" data-raw=\"&lt;xref:uid&gt;\"></xref>text</p>\n")]
        [InlineData("<xref:'uid with space'>text", "<p><xref href=\"uid with space\" data-throw-if-not-resolved=\"True\" data-raw=\"&lt;xref:&#39;uid with space&#39;&gt;\"></xref>text</p>\n")]
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
<hr>
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
<div class=""NOTE""><h5>NOTE</h5><p>note text 1-1
note text 1-2<br>note text 2-1
This is also note<br>This is also note with br</p>
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
<div class=""NOTE""><h5>NOTE</h5><p>no-note text 1-2<br>no-note text 2-1</p>
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
<div class=""NOTE""><h5>NOTE</h5><p>no-note text 2-1<br>no-note text 2-2</p>
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
        public void TestSectionNoteInBlockQuote(string source, string expected)
        {
            var markedContent = DocfxFlavoredMarked.Markup(source);
            Assert.Equal(expected.Replace("\r\n", "\n"), markedContent);
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

>```cs-i
    var outlookClient = await CreateOutlookClientAsync(""Calendar"");
    var events = await outlookClient.Me.Events.Take(10).ExecuteAsync();
            foreach (var calendarEvent in events.CurrentPage)
            {
                System.Diagnostics.Debug.WriteLine(""Event '{0}'."", calendarEvent.Subject);
            }
```

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
        public void TestDfmTagValidate()
        {
            var builder = new DfmEngineBuilder(new Options() { Mangle = false });
            var mrb = new MarkdownValidatorBuilder(
                new ContainerConfiguration()
                    .WithAssembly(typeof(DocfxFlavoredMarkdownTest).Assembly)
                    .CreateContainer());
            mrb.AddTagValidators(
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
                });
            mrb.AddValidators(HtmlMarkdownTokenValidatorProvider.ContractName);
            builder.Rewriter = mrb.Create();

            var engine = builder.CreateDfmEngine(new DfmRenderer());
            var listener = new TestLoggerListener("test!!!!" + "." + MarkdownValidatorBuilder.MarkdownValidatePhaseName);
            Logger.RegisterListener(listener);
            string result;
            using (new LoggerPhaseScope("test!!!!"))
            {
                result = engine.Markup(@"<div><i>x</i><EM>y</EM><h1>z</h1></div>", "test");
            }
            Logger.UnregisterListener(listener);
            Assert.Equal("<div><i>x</i><EM>y</EM><h1>z</h1></div>", result);
            Assert.Equal(5, listener.Items.Count);
            Assert.Equal(new[] { HtmlMarkdownTokenValidatorProvider.WarningMessage,  "Invalid tag(div)!", "Invalid tag(EM)!", "Warning tag(h1)!", "Warning tag(h1)!" }, from item in listener.Items select item.Message);
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
            Assert.Equal("<pre><code class=\"lang-FakeREST\" name=\"REST\">\n{\n   &quot;method&quot;: &quot;GET&quot;,\n   &quot;resourceFormat&quot;: &quot;https://outlook.office.com/api/v1.0/me/events?$select=Subject,Organizer,Start,End&quot;,\n   &quot;requestUrl&quot;: &quot;https://outlook.office.com/api/v1.0/me/events?$select=Subject,Organizer,Start,End&quot;,\n   &quot;requestHeaders&quot;: {\n                &quot;Accept&quot;: &quot;application/json&quot;\n   }\n}\n</code></pre><pre><code class=\"lang-FakeREST-i\" name=\"REST-i\" title=\"This is root\">\n{\n   &quot;method&quot;: &quot;GET&quot;,\n   &quot;resourceFormat&quot;: &quot;https://outlook.office.com/api/v1.0/me/events?$select=Subject,Organizer,Start,End&quot;,\n   &quot;requestUrl&quot;: &quot;https://outlook.office.com/api/v1.0/me/events?$select=Subject,Organizer,Start,End&quot;,\n   &quot;requestHeaders&quot;: {\n                &quot;Accept&quot;: &quot;application/json&quot;\n   }\n}\n</code></pre><pre><code name=\"No Language\">\n{\n   &quot;method&quot;: &quot;GET&quot;,\n   &quot;resourceFormat&quot;: &quot;https://outlook.office.com/api/v1.0/me/events?$select=Subject,Organizer,Start,End&quot;,\n   &quot;requestUrl&quot;: &quot;https://outlook.office.com/api/v1.0/me/events?$select=Subject,Organizer,Start,End&quot;,\n   &quot;requestHeaders&quot;: {\n                &quot;Accept&quot;: &quot;application/json&quot;\n   }\n}\n</code></pre><pre><code class=\"lang-js\" name=\"empty\">\n{\n   &quot;method&quot;: &quot;GET&quot;,\n   &quot;resourceFormat&quot;: &quot;https://outlook.office.com/api/v1.0/me/events?$select=Subject,Organizer,Start,End&quot;,\n   &quot;requestUrl&quot;: &quot;https://outlook.office.com/api/v1.0/me/events?$select=Subject,Organizer,Start,End&quot;,\n   &quot;requestHeaders&quot;: {\n                &quot;Accept&quot;: &quot;application/json&quot;\n   }\n}\n</code></pre>", marked);
        }

        [Theory]
        [Trait("Owner", "humao")]
        [Trait("Related", "DfmMarkdown")]
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
        [InlineData(@"[!code-csharp[Main](Program.cs#namespace ""This is root"")]", @"<pre><code class=""lang-csharp"" name=""Main"" title=""This is root"">    using System;
    using System.Collections.Generic;
    using System.IO;
</code></pre>")]
        [InlineData(@"[!code-csharp[Main](Program.cs#NAMESPACE ""This is root"")]", @"<pre><code class=""lang-csharp"" name=""Main"" title=""This is root"">    using System;
    using System.Collections.Generic;
    using System.IO;
</code></pre>")]
        [InlineData(@"[!code-csharp[Main](Program.cs#program ""This is root"")]", @"<pre><code class=""lang-csharp"" name=""Main"" title=""This is root"">    class Program
    {
        static void Main(string[] args)
        {
            string s = &quot;test&quot;;
            int i = 100;
        }
    }
</code></pre>")]
        [InlineData(@"[!code-csharp[Main](Program.cs#snippetprogram ""This is root"")]", @"<pre><code class=""lang-csharp"" name=""Main"" title=""This is root"">    class Program
    {
        static void Main(string[] args)
        {
            string s = &quot;test&quot;;
            int i = 100;
        }
    }
</code></pre>")]
        [InlineData(@"[!code-csharp[Main](Program.cs?name=namespace ""This is root"")]", @"<pre><code class=""lang-csharp"" name=""Main"" title=""This is root"">    using System;
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
