// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite.Tests
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.IO;

    using Microsoft.DocAsCode.MarkdownLite;

    using Xunit;

    [Collection("docfx STA")]
    public class PerfTest
    {
        [Fact]
        [Trait("Related", "Markdown")]
        [Trait("Related", "Perf")]
        public void TestPerf()
        {
            const int RepeatCount = 800;
            string source = GetSource(RepeatCount);
            var builder = new GfmEngineBuilder(new Options());
            var engine = builder.CreateEngine(new HtmlRenderer());
            for (int i = 0; i < 2; i++)
            {
                var result = engine.Markup(source);
                Assert.True(Enumerable.SequenceEqual(GetExpectedLines(RepeatCount), GetLines(result)));
            }
            GC.Collect();
        }

        private static string GetSource(int RepeatCount)
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
            return string.Concat(Enumerable.Repeat(source, RepeatCount));
        }

        private static IEnumerable<string> GetLines(string text)
        {
            var sr = new StringReader(text);
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                yield return line;
            }
        }

        private static IEnumerable<string> GetExpectedLines(int count)
        {
            for (int i = 0; i < count; i++)
            {
                string idPostFix;
                if (i == 0)
                {
                    idPostFix = string.Empty;
                }
                else
                {
                    idPostFix = "-" + i.ToString();
                }
                yield return $@"<h1 id=""heading{idPostFix}"">Heading</h1>";
                yield return $@"<h2 id=""sub-heading{idPostFix}"">Sub-heading</h2>";
                yield return $@"<h3 id=""another-deeper-heading{idPostFix}"">Another deeper heading</h3>";
                yield return @"<p>Paragraphs are separated";
                yield return @"by a blank line.</p>";
                yield return @"<p>Leave 2 spaces at the end of a line to do a<br>line break</p>";
                yield return @"<p>Text attributes <em>italic</em>, <strong>bold</strong>, ";
                yield return @"<code>monospace</code>, <del>strikethrough</del> .</p>";
                yield return @"<p>A <a href=""http://example.com"" data-raw-source=""[link](http://example.com)"">link</a>.</p>";
                yield return @"<p>Shopping list:</p>";
                yield return @"<ul>";
                yield return @"<li>apples</li>";
                yield return @"<li>oranges</li>";
                yield return @"<li>pears</li>";
                yield return @"</ul>";
                yield return @"<p>Numbered list:</p>";
                yield return @"<ol>";
                yield return @"<li>apples</li>";
                yield return @"<li>oranges</li>";
                yield return @"<li>pears</li>";
                yield return @"</ol>";
            }
        }
    }
}
