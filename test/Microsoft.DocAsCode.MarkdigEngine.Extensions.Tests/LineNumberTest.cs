// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Tests
{
    using System.Collections.Generic;
    using Xunit;

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

            // assert
            var expected = @"<h1 id=""a-simple-test-for-line-number"" sourceFile=""Topic.md"" sourceStartLineNumber=""2"">a simple test for line number</h1>
<ul sourceFile=""Topic.md"" sourceStartLineNumber=""3"">
<li sourceFile=""Topic.md"" sourceStartLineNumber=""3"">list member 1</li>
<li sourceFile=""Topic.md"" sourceStartLineNumber=""4"">list member 2</li>
</ul>
<hr sourceFile=""Topic.md"" sourceStartLineNumber=""5"" />
<p sourceFile=""Topic.md"" sourceStartLineNumber=""6""><a href=""http://spec.commonmark.org/0.27/"" sourceFile=""Topic.md"" sourceStartLineNumber=""6"">Two Line Link</a></p>
";
            TestUtility.VerifyMarkup(content, expected, lineNumber: true, filePath: "Topic.md");
        }

        [Fact]
        [Trait("Related", "LineNumber")]
        public void LineNumberTest_CodeSnippet()
        {
            var content = @"// <tag>
line1
// </tag>";

            var source = @"[!code[tag-test](LineNumber/Program.cs#Tag)]";

            var expected = @"<pre><code sourceFile=""Topic.md"" sourceStartLineNumber=""1"" name=""tag-test"">line1
</code></pre>";

            TestUtility.VerifyMarkup(source, expected, lineNumber: true, filePath: "Topic.md", files: new Dictionary<string, string>
            {
                { "LineNumber/Program.cs", content },
            });
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

            var expected = @"<h1 id=""root-content"" sourceFile=""LineNumber/root.md"" sourceStartLineNumber=""2"">Root content</h1>
<p sourceFile=""LineNumber/root.md"" sourceStartLineNumber=""3"">This is inline <a href=""http://spec.commonmark.org/0.27/"" sourceFile=""LineNumber/a.md"" sourceStartLineNumber=""1"">inline</a> inclusion</p>
<p sourceFile=""LineNumber/b.md"" sourceStartLineNumber=""1""><a href=""http://spec.commonmark.org/0.27/"" sourceFile=""LineNumber/b.md"" sourceStartLineNumber=""1"">block</a></p>
";
            TestUtility.VerifyMarkup(
                root,
                expected,
                lineNumber: true,
                filePath: "LineNumber/root.md",
                files: new Dictionary<string, string>
                {
                    { "LineNumber/a.md", refa },
                    { "LineNumber/b.md", refb },
                });
        }
    }
}
