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
            @"<table>
<thead>
<tr>
<th></th>
<th></th>
<th>Header1</th>
<th>Header2</th>
</tr>
</thead>
<tbody>
<tr>
<td></td>
<td>Single backticks</td>
<td><code>&#39;Isn&#39;t this fun?&#39;</code></td>
<td>&#39;Isn&#39;t this fun?&#39;</td>
<td></td>
</tr>
<tr>
<td></td>
<td>Quotes</td>
<td><code>&quot;Isn&#39;t this fun?&quot;</code></td>
<td>&quot;Isn&#39;t this fun?&quot;</td>
<td></td>
</tr>
<tr>
<td></td>
<td>Dashes</td>
<td><code>-- is en-dash, --- is em-dash</code></td>
<td>-- is en-dash, --- is em-dash</td>
<td></td>
</tr>
</tbody>
</table>
")]
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
", @"<h1 id=""heading"">Heading</h1>
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
")]
        [InlineData(@"-   [A](link1)
-   [B](link2)
    -   [B
        1](link3)
    -   [B'2](link4)", @"<ul>
<li><a href=""link1"">A</a></li>
<li><a href=""link2"">B</a><ul>
<li><a href=""link3"">B
1</a></li>
<li><a href=""link4"">B&#39;2</a></li>
</ul>
</li>
</ul>
")]
        [InlineData(@"a
```
code
```", @"<p>a</p>
<pre><code>code
</code></pre>")]
        [InlineData(@"1. asdf
2. sdfa
3. adsf
       - j
       - j

           ![](a)", @"<ol>
<li>asdf</li>
<li>sdfa</li>
<li><p>adsf</p>
<pre><code>- j
- j

    ![](a)
</code></pre></li>
</ol>
")]
        [InlineData(@"1. asdf
2. sdfa
3. adsf
   - j
   - j

         ![](a)", @"<ol>
<li>asdf</li>
<li>sdfa</li>
<li><p>adsf</p>
<ul>
<li>j</li>
<li><p>j</p>
<pre><code>![](a)
</code></pre></li>
</ul>
</li>
</ol>
")]
        [InlineData(@"1. asdf
2. sdfa
3. adsf
   - j
   - j

   ![](a)", @"<ol>
<li>asdf</li>
<li>sdfa</li>
<li><p>adsf</p>
<ul>
<li>j</li>
<li>j</li>
</ul>
<p><img src=""a"" alt=""""></p>
</li>
</ol>
")]
        [InlineData(@"* X
  > a

  > b
* Y", @"<ul>
<li><p>X</p>
<blockquote>
<p>a</p>
<p>b</p>
</blockquote>
</li>
<li>Y</li>
</ul>
")]
        public void Parse(string source, string expected)
        {
            var builder = new GfmEngineBuilder(new Options());
            var engine = builder.CreateEngine(new MarkdownRenderer());
            var result = engine.Markup(source);
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }
    }
}
