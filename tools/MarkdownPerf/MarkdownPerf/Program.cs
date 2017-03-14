// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MarkdownPerf
{
    using System.Linq;

    using Microsoft.DocAsCode.MarkdownLite;

    internal sealed class Program
    {
        private static void Main(string[] args)
        {
            const int RepeatCount = 800;
            string source = GetSource(RepeatCount);
            var builder = new GfmEngineBuilder(new Options());
            var engine = builder.CreateEngine(new HtmlRenderer());
            for (int i = 0; i < 100; i++)
            {
                var result = engine.Markup(source);
            }
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
    }
}
