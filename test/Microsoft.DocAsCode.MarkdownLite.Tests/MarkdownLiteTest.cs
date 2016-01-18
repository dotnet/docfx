// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite.Tests
{
    using System;
    using System.Collections.Immutable;

    using Microsoft.DocAsCode.MarkdownLite;

    using Xunit;
    using System.Linq;
    public class MarkdownLiteTest
    {
        [Theory]
        [Trait("Related", "Markdown")]
        #region Inline Data
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
        [InlineData(@"a
```
c
```",
            @"<p>a</p>
<pre><code>c
</code></pre>")]
        [InlineData(@"* First
  |  | Header1 | Header2 |
  ------- | ------- | --------
  | Row1 | Cell11 | Cell12 |
* Second", @"<ul>
<li>First<table>
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
<td>Row1</td>
<td>Cell11</td>
<td>Cell12</td>
<td></td>
</tr>
</tbody>
</table>
</li>
<li>Second</li>
</ul>
")]
        [InlineData(@"1. First

  |  | Header1 | Header2 |
  ------- | ------- | --------
  | Row1 | Cell11 | Cell12 |
2. Second", @"<ol>
<li><p>First</p>
<table>
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
<td>Row1</td>
<td>Cell11</td>
<td>Cell12</td>
<td></td>
</tr>
</tbody>
</table>
</li>
<li>Second</li>
</ol>
")]
        [InlineData(@"Hello world
* list
  this should be same line with the above one
  
  this should be another line",
            @"<p>Hello world</p>
<ul>
<li><p>list
this should be same line with the above one</p>
<p>this should be another line</p>
</li>
</ul>
")]
        [InlineData("[A] (link1)", @"<p><a href=""link1"">A</a></p>
")]
        #endregion
        public void TestGfmInGeneral(string source, string expected)
        {
            var builder = new GfmEngineBuilder(new Options());
            var engine = builder.CreateEngine(new HtmlRenderer());
            var result = engine.Markup(source);
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "Markdown")]
        public void TestGfmWithValidator()
        {
            const string source = "#Hello World";
            const string expected = "<h1 id=\"hello-world\">Hello World</h1>\n";
            const string expectedMessage = "a space is expected after '#'";
            string message = null;
            var builder = new GfmEngineBuilder(new Options());
            builder.Rewriter =
                MarkdownTokenRewriterFactory.FromValidators(
                    MarkdownTokenValidatorFactory.FromLambda(
                        (MarkdownHeadingBlockToken token) =>
                        {
                            if (!token.RawMarkdown.StartsWith("# "))
                            {
                                message = expectedMessage;
                            }
                        }));
            var engine = builder.CreateEngine(new HtmlRenderer());
            var result = engine.Markup(source);
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
            Assert.Equal(expectedMessage, message);
        }

        [Fact]
        [Trait("Related", "Markdown")]
        public void TestGfmWithRewrite()
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
            const string expected = @"<p>Paragraphs are separated
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
            builder.Rewriter =
                MarkdownTokenRewriterFactory.FromLambda(
                    (IMarkdownRewriteEngine e, MarkdownHeadingBlockToken t) => new MarkdownIgnoreToken(t.Rule, t.Context, t.RawMarkdown) // ignore all heading
                );
            var engine = builder.CreateEngine(new HtmlRenderer());
            var result = engine.Markup(source);
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "Markdown")]
        public void ParseWithBadRewrite()
        {
            const string source = @"
# Heading
";

            var builder = new GfmEngineBuilder(new Options());
            builder.Rewriter =
                MarkdownTokenRewriterFactory.Loop(
                    MarkdownTokenRewriterFactory.Composite(
                        MarkdownTokenRewriterFactory.FromLambda(
                            (IMarkdownRewriteEngine e, MarkdownHeadingBlockToken t) => new MarkdownTextToken(t.Rule, t.Context, t.RawMarkdown, t.RawMarkdown)
                        ),
                        MarkdownTokenRewriterFactory.FromLambda(
                            (IMarkdownRewriteEngine e, MarkdownTextToken t) => new MarkdownHeadingBlockToken(t.Rule, t.Context, new InlineContent(ImmutableArray<IMarkdownToken>.Empty), "aaaa", 1, t.RawMarkdown)
                        )
                    ),
                10);
            var engine = builder.CreateEngine(new HtmlRenderer());
            Assert.Throws<InvalidOperationException>(() => engine.Markup(source));
        }

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
            var expected1000 = string.Concat(Enumerable.Repeat(expected.Replace("\r\n", "\n"), 1000));
            var engine = builder.CreateEngine(new HtmlRenderer());
            for (int i = 0; i < 10; i++)
            {
                var result = engine.Markup(source1000);
                Assert.Equal(expected1000, result);
            }
        }
    }
}
