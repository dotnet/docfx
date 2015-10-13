// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.Tests
{
    using MarkdownLite;
    using Xunit;

    public class MarkdownLiteTest
    {
        [Theory]
        [Trait("Related", "Markdown")]
        [InlineData("", "")]
        [InlineData("# Hello World", "<h1 id=\"hello-world\">Hello World</h1>\n")]
        [InlineData("Hot keys: <kbd>Ctrl+[</kbd> and <kbd>Ctrl+]</kbd>", "<p>Hot keys: <kbd>Ctrl+[</kbd> and <kbd>Ctrl+]</kbd></p>\n")]
        [InlineData("<div>Some text here</div>", "<div>Some text here</div>")]
        [InlineData(
            @"|                  | Header1                        | Header2              |
 ----------------- | ---------------------------- | ------------------
| Single backticks | `'Isn't this fun?'`            | 'Isn't this fun?' |
| Quotes           | `""Isn't this fun?""`            | ""Isn't this fun?"" |
| Dashes           | `-- is en-dash, --- is em-dash` | -- is en-dash, --- is em-dash |",
            "<table>\n<thead>\n<tr>\n<th></th>\n<th></th>\n<th>Header1</th>\n<th>Header2</th>\n</tr>\n</thead>\n<tbody>\n<tr>\n<td></td>\n<td>Single backticks</td>\n<td><code>&#39;Isn&#39;t this fun?&#39;</code></td>\n<td>&#39;Isn&#39;t this fun?&#39;</td>\n<td></td>\n</tr>\n<tr>\n<td></td>\n<td>Quotes</td>\n<td><code>&quot;Isn&#39;t this fun?&quot;</code></td>\n<td>&quot;Isn&#39;t this fun?&quot;</td>\n<td></td>\n</tr>\n<tr>\n<td></td>\n<td>Dashes</td>\n<td><code>-- is en-dash, --- is em-dash</code></td>\n<td>-- is en-dash, --- is em-dash</td>\n<td></td>\n</tr>\n</tbody>\n</table>\n")]
        [InlineData(@"
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
", "<h1 id=\"heading\">Heading</h1>\n<h2 id=\"sub-heading\">Sub-heading</h2>\n<h3 id=\"another-deeper-heading\">Another deeper heading</h3>\n<p>Paragraphs are separated\nby a blank line.</p>\n<p>Leave 2 spaces at the end of a line to do a<br>line break</p>\n<p>Text attributes <em>italic</em>, <strong>bold</strong>, \n<code>monospace</code>, <del>strikethrough</del> .</p>\n<p>A <a href=\"http://example.com\">link</a>.</p>\n<p>Shopping list:</p>\n<ul>\n<li>apples</li>\n<li>oranges</li>\n<li>pears</li>\n</ul>\n<p>Numbered list:</p>\n<ol>\n<li>apples</li>\n<li>oranges</li>\n<li>pears</li>\n</ol>\n")]
        public void Parse(string source, string expected)
        {
            Assert.Equal(expected, Marked.Markup(source));
        }

        [Theory]
        [Trait("Related", "Markdown")]
        [InlineData("", "")]
        [InlineData("# Hello World", "<h1 id=\"hello-world\">Hello World</h1>\n")]
        [InlineData("Hot keys: <kbd>Ctrl+[</kbd> and <kbd>Ctrl+]</kbd>", "<p>Hot keys: <kbd>Ctrl+[</kbd> and <kbd>Ctrl+]</kbd></p>\n")]
        [InlineData("<div>Some text here</div>", "<div>Some text here</div>")]
        [InlineData(
            @"|                  | Header1                        | Header2              |
 ----------------- | ---------------------------- | ------------------
| Single backticks | `'Isn't this fun?'`            | 'Isn't this fun?' |
| Quotes           | `""Isn't this fun?""`            | ""Isn't this fun?"" |
| Dashes           | `-- is en-dash, --- is em-dash` | -- is en-dash, --- is em-dash |",
            "<table>\n<thead>\n<tr>\n<th></th>\n<th></th>\n<th>Header1</th>\n<th>Header2</th>\n</tr>\n</thead>\n<tbody>\n<tr>\n<td></td>\n<td>Single backticks</td>\n<td><code>&#39;Isn&#39;t this fun?&#39;</code></td>\n<td>&#39;Isn&#39;t this fun?&#39;</td>\n<td></td>\n</tr>\n<tr>\n<td></td>\n<td>Quotes</td>\n<td><code>&quot;Isn&#39;t this fun?&quot;</code></td>\n<td>&quot;Isn&#39;t this fun?&quot;</td>\n<td></td>\n</tr>\n<tr>\n<td></td>\n<td>Dashes</td>\n<td><code>-- is en-dash, --- is em-dash</code></td>\n<td>-- is en-dash, --- is em-dash</td>\n<td></td>\n</tr>\n</tbody>\n</table>\n")]
        // todo : waiting gfm
        [InlineData(@"
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
", "<h1 id=\"heading\">Heading</h1>\n<h2 id=\"sub-heading\">Sub-heading</h2>\n<h3 id=\"another-deeper-heading\">Another deeper heading</h3>\n<p>Paragraphs are separated\nby a blank line.</p>\n<p>Leave 2 spaces at the end of a line to do a<br>line break</p>\n<p>Text attributes <em>italic</em>, <strong>bold</strong>, \n<code>monospace</code>, <del>strikethrough</del> .</p>\n<p>A <a href=\"http://example.com\">link</a>.</p>\n<p>Shopping list:</p>\n<ul>\n<li>apples</li>\n<li>oranges</li>\n<li>pears</li>\n</ul>\n<p>Numbered list:</p>\n<ol>\n<li>apples</li>\n<li>oranges</li>\n<li>pears</li>\n</ol>\n")]
        public void Parse2(string source, string expected)
        {
            var builder = new GfmEngineBuilder(new Options());
            var engine = builder.CreateEngine(new MarkdownRenderer());
            var result = engine.Markup(source);
            Assert.Equal(expected, result);
        }
    }
}
