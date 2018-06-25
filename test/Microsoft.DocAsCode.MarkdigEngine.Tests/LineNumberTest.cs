// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Tests
{
    using System.Collections.Generic;
    using System.IO;

    using MarkdigEngine.Extensions;

    using Microsoft.DocAsCode.Plugins;
    using Xunit;

    [Collection("docfx STA")]
    public class LineNumberTest
    {
        [Fact]
        [Trait("Related", "LineNumber")]
        public void LineNumberTest_General()
        {
            // prepare
            string content = @"
# a simple test for line number
- list member 1
- list member 2
***
[Two Line Link](
http://spec.commonmark.org/0.27/)";

            // act
            var marked = TestUtility.Markup(content, "Topic.md");

            // assert
            var expected = @"<h1 id=""a-simple-test-for-line-number"" sourceFile=""Topic.md"" sourceStartLineNumber=""2"">a simple test for line number</h1>
<ul sourceFile=""Topic.md"" sourceStartLineNumber=""3"">
<li sourceFile=""Topic.md"" sourceStartLineNumber=""3"">list member 1</li>
<li sourceFile=""Topic.md"" sourceStartLineNumber=""4"">list member 2</li>
</ul>
<hr sourceFile=""Topic.md"" sourceStartLineNumber=""5"" />
<p sourceFile=""Topic.md"" sourceStartLineNumber=""6""><a href=""http://spec.commonmark.org/0.27/"" sourceFile=""Topic.md"" sourceStartLineNumber=""6"">Two Line Link</a></p>
";
            Assert.Equal(expected.Replace("\r\n", "\n"), marked.Html);
        }

        [Fact]
        [Trait("Related", "LineNumber")]
        public void LineNumberTest_CodeSnippet()
        {
            //arange
            var content = @"// <tag>
line1
// </tag>";

            if (!Directory.Exists("LineNumber"))
            {
                Directory.CreateDirectory("LineNumber");
            }

            File.WriteAllText("LineNumber/Program.cs", content.Replace("\r\n", "\n"));

            // act
            var parameter = new MarkdownServiceParameters
            {
                BasePath = ".",
                Extensions = new Dictionary<string, object>
                {
                    { "EnableSourceInfo", true }
                }
            };
            var service = new MarkdigMarkdownService(parameter);
            var marked = service.Markup(@"[!code[tag-test](LineNumber/Program.cs#Tag)]", "Topic.md");

            // assert
            var expected = @"<pre><code sourceFile=""Topic.md"" sourceStartLineNumber=""1"" name=""tag-test"">line1
</code></pre>";
            Assert.Equal(expected.Replace("\r\n", "\n"), marked.Html);
        }

        [Fact]
        [Trait("Related", "LineNumber")]
        public void LineNumberTest_Inclusion()
        {
            var root = @"
# Root content
This is inline [!include[ref-inline](a.md)] inclusion
[!include[ref-block](b.md)]";

            var refa = @"[inline](
http://spec.commonmark.org/0.27/)";

            var refb = @"[block](
http://spec.commonmark.org/0.27/)";

            if (!Directory.Exists("LineNumber"))
            {
                Directory.CreateDirectory("LineNumber");
            }

            File.WriteAllText("LineNumber/root.md", root);
            File.WriteAllText("LineNumber/a.md", refa);
            File.WriteAllText("LineNumber/b.md", refb);

            var result = TestUtility.Markup(root, "LineNumber/root.md");
            var expected = @"<h1 id=""root-content"" sourceFile=""LineNumber/root.md"" sourceStartLineNumber=""2"">Root content</h1>
<p sourceFile=""LineNumber/root.md"" sourceStartLineNumber=""3"">This is inline <a href=""http://spec.commonmark.org/0.27/"" sourceFile=""LineNumber/a.md"" sourceStartLineNumber=""1"">inline</a> inclusion</p>
<p sourceFile=""LineNumber/b.md"" sourceStartLineNumber=""1""><a href=""http://spec.commonmark.org/0.27/"" sourceFile=""LineNumber/b.md"" sourceStartLineNumber=""1"">block</a></p>
";
            Assert.Equal(expected.Replace("\r\n", "\n"), result.Html);
        }
    }
}
