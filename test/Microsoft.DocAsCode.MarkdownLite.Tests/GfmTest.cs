// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite.Tests
{
    using Microsoft.DocAsCode.MarkdownLite;

    using Xunit;

    public class GfmTest
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
        [InlineData(@"# Test Ref
For more information about user navigation properties, see the documentation for [User].

[User]: ./entity-and-complex-type-reference.md#UserEntity",
            @"<h1 id=""test-ref"">Test Ref</h1>
<p>For more information about user navigation properties, see the documentation for <a href=""./entity-and-complex-type-reference.md#UserEntity"">User</a>.</p>
")]
        [InlineData(@"> [abc][1]
> > [1]


[1]: 11111",
            @"<blockquote>
<p><a href=""11111"">abc</a></p>
<blockquote>
<p><a href=""11111"">1</a></p>
</blockquote>
</blockquote>
")]
        [InlineData(@"[a](a(b).c)",
            @"<p><a href=""a(b).c"">a</a></p>
")]
        [InlineData(@"[a](a(b(c)).d 'text')",
            @"<p><a href=""a(b(c)).d"" title=""text"">a</a></p>
")]
        [InlineData(@"__a__*b*__c__",
            @"<p><strong>a</strong><em>b</em><strong>c</strong></p>
")]
        [InlineData(@"1. a
2. b

3. c





4. d
",
            @"<ol>
<li>a</li>
<li><p>b</p>
</li>
<li><p>c</p>
</li>
<li><p>d</p>
</li>
</ol>
")]
        [InlineData(@"1.  a
2.  b

3.  c

break list!
1.  d
",
            @"<ol>
<li>a</li>
<li><p>b</p>
</li>
<li><p>c</p>
</li>
</ol>
<p>break list!</p>
<ol>
<li>d</li>
</ol>
")]
        [InlineData(@"1. some text
 ```
 --
 ```",
            @"<ol>
<li>some text<pre><code>--
</code></pre></li>
</ol>
")]
        [InlineData(
            @"a\<b <span>c</span>",
            @"<p>a&lt;b <span>c</span></p>
")]
        [InlineData(
            @"***A***",
            @"<p><strong><em>A</em></strong></p>
")]
        [InlineData(
            @"***A*B**",
            @"<p><strong><em>A</em>B</strong></p>
")]
        [InlineData(
            @"***A**B*",
            @"<p><em>**A</em><em>B</em></p>
")]
        [InlineData(
            @"***A*****B*****C***",
            @"<p><strong><em>A</em></strong><strong>B</strong><strong><em>C</em></strong></p>
")]
        [InlineData(
            @"****A****",
            @"<p>*<strong><em>A</em></strong>*</p>
")]
        [InlineData(
            @"***A*B **  C***",
            @"<p><strong><em>A</em>B **  C</strong>*</p>
")]
        [InlineData(
            @"**A*B***",
            @"<p><strong>A*B</strong>*</p>
")]
        [InlineData(
            @"*A**B***",
            @"<p><em>A</em><em>B</em>**</p>
")]
        [InlineData(
            @"***A*B*C*D**",
            @"<p><strong><em>A</em>B<em>C</em>D</strong></p>
")]
        [InlineData(
            @"***A*B
**  C***",
            @"<p><strong><em>A</em>B
**  C</strong>*</p>
")]
        [InlineData(
            @"a**************",
            @"<p>a**************</p>
")]
        [InlineData(
            @"a* A*",
            @"<p>a* A*</p>
")]
        [InlineData(
            @"* A
* B


1. C
2. D
",
            @"<ul>
<li>A</li>
<li>B</li>
</ul>
<ol>
<li>C</li>
<li>D</li>
</ol>
")]
        [InlineData(
            @"<!--aaa-->
aaa",
            @"<!--aaa-->
<p>aaa</p>
")]
        [InlineData(
            @"[a\](b)",
            @"<p>[a](b)</p>
")]
        [InlineData(
            @"[a](b\)",
            @"<p>[a](b)</p>
")]
        [InlineData(
            @"[a\\](b)",
            @"<p><a href=""b"">a\</a></p>
")]
        [InlineData(
            @"[a](b\\)",
            @"<p><a href=""b\"">a</a></p>
")]
        [InlineData(
            @"<!--a-->[b](c)<!--d-->e
<!--f-->",
            @"<p><!--a--><a href=""c"">b</a><!--d-->e
<!--f--></p>
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
        public void TestTable_WithEmptyCell()
        {
            // 1. Prepare data
            var source = @"# hello
|  Name |  Type |  Notes |  Read/Write |  Description |
|:-------|:-------|:-------|:-------|:-------|
| value | Edm.String |  |  |
| endDate | Edm.DateTime |  |  | The date and time at which the password expires. |
| value | Edm.String |  |  |  |
";

            var expected = @"<h1 id=""hello"">hello</h1>
<table>
<thead>
<tr>
<th style=""text-align:left"">Name</th>
<th style=""text-align:left"">Type</th>
<th style=""text-align:left"">Notes</th>
<th style=""text-align:left"">Read/Write</th>
<th style=""text-align:left"">Description</th>
</tr>
</thead>
<tbody>
<tr>
<td style=""text-align:left"">value</td>
<td style=""text-align:left"">Edm.String</td>
<td style=""text-align:left""></td>
<td style=""text-align:left""></td>
<td style=""text-align:left""></td>
</tr>
<tr>
<td style=""text-align:left"">endDate</td>
<td style=""text-align:left"">Edm.DateTime</td>
<td style=""text-align:left""></td>
<td style=""text-align:left""></td>
<td style=""text-align:left"">The date and time at which the password expires.</td>
</tr>
<tr>
<td style=""text-align:left"">value</td>
<td style=""text-align:left"">Edm.String</td>
<td style=""text-align:left""></td>
<td style=""text-align:left""></td>
<td style=""text-align:left""></td>
</tr>
</tbody>
</table>
";
            TestGfmInGeneral(source, expected);
        }

        [Fact]
        [Trait("Related", "Markdown")]
        public void TestGfmBuilder_CommentRuleShouldBeforeAutoLink()
        {
            var source = @"<!--
https://en.wikipedia.org/wiki/Draft:Microsoft_SQL_Server_Libraries/Drivers
-->";

            var expected = @"<!--
https://en.wikipedia.org/wiki/Draft:Microsoft_SQL_Server_Libraries/Drivers
-->";
            TestGfmInGeneral(source, expected);
        }

        [Fact]
        [Trait("Related", "Markdown")]
        public void TestGfmBuilder_CodeTag()
        {
            var source = @"<pre><code>//*************************************************
        // Test!
        //*************************************************</code></pre>
";
            var expected = source;
            TestGfmInGeneral(source, expected);
        }

        [Fact]
        [Trait("Related", "Markdown")]
        public void TestTable_WithRefLink()
        {
            var source = @"# Test table
| header-1 | header-2 | header-3 |
|:-------- |:--------:| --------:|
| *1-1* | [User] | test |

[User]: ./entity-and-complex-type-reference.md#UserEntity";

            var expected = @"<h1 id=""test-table"">Test table</h1>
<table>
<thead>
<tr>
<th style=""text-align:left"">header-1</th>
<th style=""text-align:center"">header-2</th>
<th style=""text-align:right"">header-3</th>
</tr>
</thead>
<tbody>
<tr>
<td style=""text-align:left""><em>1-1</em></td>
<td style=""text-align:center""><a href=""./entity-and-complex-type-reference.md#UserEntity"">User</a></td>
<td style=""text-align:right"">test</td>
</tr>
</tbody>
</table>
";
            TestGfmInGeneral(source, expected);
        }

        [Fact]
        [Trait("Related", "Markdown")]
        public void TestGfmImageLink_WithSpecialCharactorsInAltText()
        {
            var source = @"![This is image alt text with quotation ' and double quotation ""hello"" world](girl.png)";

            var expected = @"<p><img src=""girl.png"" alt=""This is image alt text with quotation &#39; and double quotation &quot;hello&quot; world""></p>
";
            TestGfmInGeneral(source, expected);
        }

        [Fact]
        [Trait("Related", "Markdown")]
        public void TestGfmLink_WithSpecialCharactorsInTitle()
        {
            var source = @"[This is link text with quotation ' and double quotation ""hello"" world](girl.png ""title is ""hello"" world."")";

            var expected = @"<p><a href=""girl.png"" title=""title is &amp;quot;hello&amp;quot; world."">This is link text with quotation &#39; and double quotation &quot;hello&quot; world</a></p>
";
            TestGfmInGeneral(source, expected);
        }
    }
}
