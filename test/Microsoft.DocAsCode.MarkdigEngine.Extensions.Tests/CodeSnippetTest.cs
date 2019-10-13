// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Tests
{
    using System.Collections.Generic;
    using System.IO;
    using Xunit;

    public class CodeSnippetTest
    {
        [Fact]
        public void CodeSnippetNotFound()
        {
            var source = @"[!code-csharp[name](Program1.cs)]";

            var expected = @"<div class=""WARNING"">
<h5>警告</h5>
<p>你要查找的示例似乎已移动！ 不要担心，我们正在努力解决此问题。</p>
</div>";

            TestUtility.VerifyMarkup(
                source,
                expected,
                errors: new[] { "codesnippet-not-found" },
                tokens: new Dictionary<string, string>
                {
                    { "codeIncludeNotFound", "你要查找的示例似乎已移动！ 不要担心，我们正在努力解决此问题。"},
                    { "warning", "<h5>警告</h5>" }
                });
        }

        [Fact]
        public void CodeSnippetGeneral()
        {
            //arange
            var content = @"    line for start & end
    // <tag1>
    line1
    // </tag1>
" + " \tline for indent & range";

            var source = @"[!code-csharp[name](Program.cs?start=1&end=1&name=tag&range=5-&highlight=1,2-2,4-&dedent=3#tag1)]";

            // assert
            var expected = @"<pre><code class=""lang-csharp"" name=""name"" highlight-lines=""1,2,4-""> line1
</code></pre>";

            TestUtility.VerifyMarkup(source, expected, files: new Dictionary<string, string>
            {
                { "Program.cs", content }
            });
        }

        [Fact]
        public void NotebookCodeSnippetGeneral()
        {
            var content = @"{
 ""cells"": [
  {
   ""cell_type"": ""code"",
   ""execution_count"": null,
   ""metadata"": {
    ""name"": ""import""
   },
   ""outputs"": [],
   ""source"": [
    ""import azureml.core\n"",
    ""print(azureml.core.VERSION)""
   ]
  }
 ]
}";
            var source = @"[!notebook-python[](Program.ipynb?name=import)]";

            var expected = @"<pre><code class=""lang-python"">import azureml.core
print(azureml.core.VERSION)</code></pre>";

            TestUtility.VerifyMarkup(source, expected, files: new Dictionary<string, string>
            {
                { "Program.ipynb", content },
            });
        }

        [Fact]
        public void NotebookCodeSnippetTagNotFound()
        {
            var content = @"{
 ""cells"": [
  {
   ""cell_type"": ""code"",
   ""execution_count"": null,
   ""metadata"": {
    ""name"": ""import""
   },
   ""outputs"": [],
   ""source"": [
    ""import azureml.core\n"",
    ""print(azureml.core.VERSION)""
   ]
  }
 ]
}";

            var source = @"[!notebook-python[](Program.ipynb?name=nonexistent)]";
            var expected = @"<pre><code class=""lang-python""></code></pre>";

            TestUtility.VerifyMarkup(
                source,
                expected,
                errors: new[] { "tag-not-found" },
                files: new Dictionary<string, string>
                {
                    { "Program.ipynb", content },
                });
        }

        [Fact]
        public void NotebookCodeSnippetMultipleTagFound()
        {
            //arange
            var content = @"{
 ""cells"": [
  {
   ""cell_type"": ""code"",
   ""execution_count"": null,
   ""metadata"": {
    ""name"": ""import""
   },
   ""outputs"": [],
   ""source"": [
    ""import azureml.core\n"",
    ""print(azureml.core.VERSION)""
   ]
  },
 {
   ""cell_type"": ""code"",
   ""execution_count"": null,
   ""metadata"": {
    ""name"": ""import""
   },
   ""outputs"": [],
   ""source"": [
    ""import azureml.core\n"",
    ""print(azureml.core.VERSION)""
   ]
  }
 ]
}";

            var source = @"[!notebook-python[](Program.ipynb?name=import)]";
            var expected = @"<pre><code class=""lang-python""></code></pre>";

            TestUtility.VerifyMarkup(
                source,
                expected,
                errors: new[] { "mutiple-tags-with-same-name" },
                files: new Dictionary<string, string>
                {
                    { "Program.ipynb", content },
                });
        }

        [Fact]
        public void CodeSnippetShouldNotWorkInParagragh()
        {
            var source = "text [!code[test](CodeSnippet.cs)]";

            var expected = @"<p>text [!code<a href=""CodeSnippet.cs"">test</a>]</p>";

            TestUtility.VerifyMarkup(source, expected);
        }

        [Fact]
        public void CodeSnippetTagsShouldMatchCaseInsensitive()
        {
            var content = "\t// <tag1>\t\nline1\n// <tag2>\nline2\n// </tag2>\nline3\n// </TAG1>\n// <unmatched>\n";

            var source = "[!code[tag1](Program.cs#Tag1)]";

            var expected = "<pre><code name=\"tag1\">line1\nline2\nline3\n</code></pre>";

            TestUtility.VerifyMarkup(source, expected, files: new Dictionary<string, string>
            {
                { "Program.cs", content },
            });
        }

        [Fact]
        public void CodeSnippetTagsShouldSucceedWhenDuplicateWithoutWarning()
        {
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

            var source = "[!code[tag2](Program.cs#Tag2)]";

            var expected = "<pre><code name=\"tag2\">line4\n</code></pre>";

            TestUtility.VerifyMarkup(source, expected, files: new Dictionary<string, string>
            {
                { "Program.cs", content },
            });
        }

        [Fact]
        public void CodeSnippetTagsShouldSucceedWhenDuplicateWithWarningWhenReferenced()
        {
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

            var source = "[!code[tag1](Program.cs#Tag1)]";

            var expected = "<pre><code name=\"tag1\">line2\n</code></pre>";

            TestUtility.VerifyMarkup(source, expected, files: new Dictionary<string, string>
            {
                { "Program.cs", content },
            });
        }

        [Fact]
        public void CodeSnippetTagsShouldSucceedWhenReferencedFileContainsRegionWithoutName()
        {
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

            var source = "[!code[MyClass](Program.cs#main)]";

            var expected = @"<pre><code name=""MyClass"">static void Main()
{
}
</code></pre>";

            TestUtility.VerifyMarkup(source, expected, files: new Dictionary<string, string>
            {
                { "Program.cs", content },
            });
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
            var expected = "<pre><code class=\"lang-FakeREST\" name=\"REST\">\n{\n   &quot;method&quot;: &quot;GET&quot;,\n   &quot;resourceFormat&quot;: &quot;https://outlook.office.com/api/v1.0/me/events?$select=Subject,Organizer,Start,End&quot;,\n   &quot;requestUrl&quot;: &quot;https://outlook.office.com/api/v1.0/me/events?$select=Subject,Organizer,Start,End&quot;,\n   &quot;requestHeaders&quot;: {\n                &quot;Accept&quot;: &quot;application/json&quot;\n   }\n}\n</code></pre><pre><code class=\"lang-FakeREST-i\" name=\"REST-i\" title=\"This is root\">\n{\n   &quot;method&quot;: &quot;GET&quot;,\n   &quot;resourceFormat&quot;: &quot;https://outlook.office.com/api/v1.0/me/events?$select=Subject,Organizer,Start,End&quot;,\n   &quot;requestUrl&quot;: &quot;https://outlook.office.com/api/v1.0/me/events?$select=Subject,Organizer,Start,End&quot;,\n   &quot;requestHeaders&quot;: {\n                &quot;Accept&quot;: &quot;application/json&quot;\n   }\n}\n</code></pre><pre><code name=\"No Language\">\n{\n   &quot;method&quot;: &quot;GET&quot;,\n   &quot;resourceFormat&quot;: &quot;https://outlook.office.com/api/v1.0/me/events?$select=Subject,Organizer,Start,End&quot;,\n   &quot;requestUrl&quot;: &quot;https://outlook.office.com/api/v1.0/me/events?$select=Subject,Organizer,Start,End&quot;,\n   &quot;requestHeaders&quot;: {\n                &quot;Accept&quot;: &quot;application/json&quot;\n   }\n}\n</code></pre><pre><code class=\"lang-js\" name=\"empty\">\n{\n   &quot;method&quot;: &quot;GET&quot;,\n   &quot;resourceFormat&quot;: &quot;https://outlook.office.com/api/v1.0/me/events?$select=Subject,Organizer,Start,End&quot;,\n   &quot;requestUrl&quot;: &quot;https://outlook.office.com/api/v1.0/me/events?$select=Subject,Organizer,Start,End&quot;,\n   &quot;requestHeaders&quot;: {\n                &quot;Accept&quot;: &quot;application/json&quot;\n   }\n}\n</code></pre>";

            TestUtility.VerifyMarkup(root, expected, files: new Dictionary<string, string>
            {
                { "api.json", apiJsonContent },
            });
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
        [InlineData(@"[!code[Main](Program.cs?range=1-2,10,20-21,29-&dedent=-4 ""Auto dedent if dedent < 0"")]", @"<pre><code name=""Main"" title=""Auto dedent if dedent &lt; 0"">namespace ConsoleApplication1
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

            TestUtility.VerifyMarkup(fencesPath, expectedContent, files: new Dictionary<string, string>
            {
                { "Program.cs", content },
            });
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

            var expected = "<pre><code class=\"lang-FakeREST\" name=\"REST\">\n{\n   &quot;method&quot;: &quot;GET&quot;,\n   &quot;resourceFormat&quot;: &quot;https://outlook.office.com/api/v1.0/me/events?$select=Subject,Organizer,Start,End&quot;,\n   &quot;requestUrl&quot;: &quot;https://outlook.office.com/api/v1.0/me/events?$select=Subject,Organizer,Start,End&quot;,\n   &quot;requestHeaders&quot;: {\n                &quot;Accept&quot;: &quot;application/json&quot;\n   }\n}\n</code></pre><pre><code class=\"lang-FakeREST-i\" name=\"REST-i\" title=\"This is root\">\n{\n   &quot;method&quot;: &quot;GET&quot;,\n   &quot;resourceFormat&quot;: &quot;https://outlook.office.com/api/v1.0/me/events?$select=Subject,Organizer,Start,End&quot;,\n   &quot;requestUrl&quot;: &quot;https://outlook.office.com/api/v1.0/me/events?$select=Subject,Organizer,Start,End&quot;,\n   &quot;requestHeaders&quot;: {\n                &quot;Accept&quot;: &quot;application/json&quot;\n   }\n}\n</code></pre><pre><code name=\"No Language\">\n{\n   &quot;method&quot;: &quot;GET&quot;,\n   &quot;resourceFormat&quot;: &quot;https://outlook.office.com/api/v1.0/me/events?$select=Subject,Organizer,Start,End&quot;,\n   &quot;requestUrl&quot;: &quot;https://outlook.office.com/api/v1.0/me/events?$select=Subject,Organizer,Start,End&quot;,\n   &quot;requestHeaders&quot;: {\n                &quot;Accept&quot;: &quot;application/json&quot;\n   }\n}\n</code></pre><pre><code class=\"lang-js\" name=\"empty\">\n{\n   &quot;method&quot;: &quot;GET&quot;,\n   &quot;resourceFormat&quot;: &quot;https://outlook.office.com/api/v1.0/me/events?$select=Subject,Organizer,Start,End&quot;,\n   &quot;requestUrl&quot;: &quot;https://outlook.office.com/api/v1.0/me/events?$select=Subject,Organizer,Start,End&quot;,\n   &quot;requestHeaders&quot;: {\n                &quot;Accept&quot;: &quot;application/json&quot;\n   }\n}\n</code></pre>";

            TestUtility.VerifyMarkup(root, expected, files: new Dictionary<string, string>
            {
                { "api.json", apiJsonContent },
            });
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

            var expected = @"<pre><code class=""lang-REST"" name=""REST"">
{
   &quot;method&quot;: &quot;GET&quot;,
   &quot;resourceFormat&quot;: &quot;https://outlook.office.com/api/v1.0/me/events?$select=Subject,Organizer,Start,End&quot;,
   &quot;requestUrl&quot;: &quot;https://outlook.office.com/api/v1.0/me/events?$select=Subject,Organizer,Start,End&quot;,
   &quot;requestHeaders&quot;: {
                &quot;Accept&quot;: &quot;application/json&quot;
   }
}
</code></pre>";

            TestUtility.VerifyMarkup(root, expected, files: new Dictionary<string, string>
            {
                { "api.json", apiJsonContent },
            });
        }

        [Fact]
        public void CodeSnippetShouldVerifyTagname()
        {
            //arange
            var content = @"    line for start & end
    // <tag1>
    line1
    // <Content=""my"">
    // </tag1>
" + " \tline for indent & range";

            var markdown = @"[!code-csharp[name](Program.cs#tag1)]";

            // assert
            var expected = @"<pre><code class=""lang-csharp"" name=""name"">line1
// &lt;Content=&quot;my&quot;&gt;
</code></pre>";

            TestUtility.VerifyMarkup(markdown, expected, files: new Dictionary<string, string>
            {
                { "Program.cs", content },
            });
        }


        [Fact(Skip = "won't support")]
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
            TestUtility.VerifyMarkup(root, expected, files: new Dictionary<string, string>
            {
                { "api.json", apiJsonContent },
            });
        }

        [Fact(Skip = "won't support")]
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
            var expected = "<p><pre><code class=\"lang-FakeREST\" name=\"REST\">\n{\n   &quot;method&quot;: &quot;GET&quot;,\n   &quot;resourceFormat&quot;: &quot;https://outlook.office.com/api/v1.0/me/events?$select=Subject,Organizer,Start,End&quot;,\n   &quot;requestUrl&quot;: &quot;https://outlook.office.com/api/v1.0/me/events?$select=Subject,Organizer,Start,End&quot;,\n   &quot;requestHeaders&quot;: {\n                &quot;Accept&quot;: &quot;application/json&quot;\n   }\n}\n</code></pre><pre><code class=\"lang-FakeREST-i\" name=\"REST-i\" title=\"This is root\">\n{\n   &quot;method&quot;: &quot;GET&quot;,\n   &quot;resourceFormat&quot;: &quot;https://outlook.office.com/api/v1.0/me/events?$select=Subject,Organizer,Start,End&quot;,\n   &quot;requestUrl&quot;: &quot;https://outlook.office.com/api/v1.0/me/events?$select=Subject,Organizer,Start,End&quot;,\n   &quot;requestHeaders&quot;: {\n                &quot;Accept&quot;: &quot;application/json&quot;\n   }\n}\n</code></pre><pre><code name=\"No Language\">\n{\n   &quot;method&quot;: &quot;GET&quot;,\n   &quot;resourceFormat&quot;: &quot;https://outlook.office.com/api/v1.0/me/events?$select=Subject,Organizer,Start,End&quot;,\n   &quot;requestUrl&quot;: &quot;https://outlook.office.com/api/v1.0/me/events?$select=Subject,Organizer,Start,End&quot;,\n   &quot;requestHeaders&quot;: {\n                &quot;Accept&quot;: &quot;application/json&quot;\n   }\n}\n</code></pre><pre><code class=\"lang-js\" name=\"empty\">\n{\n   &quot;method&quot;: &quot;GET&quot;,\n   &quot;resourceFormat&quot;: &quot;https://outlook.office.com/api/v1.0/me/events?$select=Subject,Organizer,Start,End&quot;,\n   &quot;requestUrl&quot;: &quot;https://outlook.office.com/api/v1.0/me/events?$select=Subject,Organizer,Start,End&quot;,\n   &quot;requestHeaders&quot;: {\n                &quot;Accept&quot;: &quot;application/json&quot;\n   }\n}\n</code></pre></p>\n";

            TestUtility.VerifyMarkup(root, expected, files: new Dictionary<string, string>
            {
                { "api.json", apiJsonContent },
            });
        }
    }
}
