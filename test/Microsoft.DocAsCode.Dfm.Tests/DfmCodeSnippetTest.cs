// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm.Tests
{
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

    [Collection("docfx STA")]
    public class DfmCodeSnippetTest
    {
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
            var token = new DfmFencesBlockToken(null, null, null, "test.cs", new SourceInfo(), "csharp", null, queryStringAndFragment);
            var marked = renderer.RenderFencesFromCodeContent(content, token);

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
    }
}
