// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite.Tests
{
    using System.Linq;
    using System.Text.RegularExpressions;

    using Microsoft.DocAsCode.MarkdownLite;

    using Xunit;

    public class PerfTest
    {
        [Fact]
        [Trait("Related", "Markdown")]
        [Trait("Related", "Perf")]
        public void TestPerf()
        {
            const string source = @"
Heading
=======
 
Sub-heading
-----------
  
### Another deeper heading
  
Paragraphs are separated
by a blank line.
 
Leave 2 spaces at the end of a line to do a  
line break
 
Text attributes *italic*, **bold**, 
`monospace`, ~~strikethrough~~ .
 
A [link](http://example.com).

Shopping list:
 
* apples
* oranges
* pears
 
Numbered list:
 
1. apples
2. oranges
3. pears
";
            const string expected = @"<h1 id=""heading"">Heading</h1>
<h2 id=""sub-heading"">Sub-heading</h2>
<h3 id=""another-deeper-heading"">Another deeper heading</h3>
<p>Paragraphs are separated
by a blank line.</p>
<p>Leave 2 spaces at the end of a line to do a<br>line break</p>
<p>Text attributes <em>italic</em>, <strong>bold</strong>, 
<code>monospace</code>, <del>strikethrough</del> .</p>
<p>A <a href=""http://example.com"">link</a>.</p>
<p>Shopping list:</p>
<ul>
<li>apples</li>
<li>oranges</li>
<li>pears</li>
</ul>
<p>Numbered list:</p>
<ol>
<li>apples</li>
<li>oranges</li>
<li>pears</li>
</ol>
";

            var builder = new GfmEngineBuilder(new Options());
            var source1000 = string.Concat(Enumerable.Repeat(source, 1000));
            var expectedArray = Enumerable.Repeat(expected.Replace("\r\n", "\n"), 1000).ToArray();
            var rewriteIdRegex = new Regex(@"(id=)""([\p{L}\p{Nd}-]+)""", RegexOptions.Compiled);
            for (var i = 1; i < expectedArray.Length; i++)
            {
                expectedArray[i] = rewriteIdRegex.Replace(expectedArray[i], string.Concat(@"$1""$2-", i - 1, @""""));
            }
            var expected1000 = string.Concat(expectedArray);
            var engine = builder.CreateEngine(new HtmlRenderer());
            for (int i = 0; i < 2; i++)
            {
                var result = engine.Markup(source1000);
                Assert.Equal(expected1000, result);
            }
        }
    }
}
