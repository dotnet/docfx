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
<th>Header1</th>
<th>Header2</th>
</tr>
</thead>
<tbody>
<tr>
<td>Single backticks</td>
<td><code>&#39;Isn&#39;t this fun?&#39;</code></td>
<td>&#39;Isn&#39;t this fun?&#39;</td>
</tr>
<tr>
<td>Quotes</td>
<td><code>&quot;Isn&#39;t this fun?&quot;</code></td>
<td>&quot;Isn&#39;t this fun?&quot;</td>
</tr>
<tr>
<td>Dashes</td>
<td><code>-- is en-dash, --- is em-dash</code></td>
<td>-- is en-dash, --- is em-dash</td>
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
<p>A <a href=""http://example.com"" data-raw-source=""[link](http://example.com)"">link</a>.</p>
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
<li><a href=""link1"" data-raw-source=""[A](link1)"">A</a></li>
<li><a href=""link2"" data-raw-source=""[B](link2)"">B</a><ul>
<li><a href=""link3"" data-raw-source=""[B
1](link3)"">B
1</a></li>
<li><a href=""link4"" data-raw-source=""[B&#39;2](link4)"">B&#39;2</a></li>
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
<ul>
<li>j</li>
<li><p>j</p>
<p>  <img src=""a"" alt=""""></p>
</li>
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
</blockquote>
<blockquote>
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
<li><p>First</p>
<table>
<thead>
<tr>
<th></th>
<th>Header1</th>
<th>Header2</th>
</tr>
</thead>
<tbody>
<tr>
<td>Row1</td>
<td>Cell11</td>
<td>Cell12</td>
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
<th>Header1</th>
<th>Header2</th>
</tr>
</thead>
<tbody>
<tr>
<td>Row1</td>
<td>Cell11</td>
<td>Cell12</td>
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
        [InlineData("[A] (link1)", @"<p><a href=""link1"" data-raw-source=""[A] (link1)"">A</a></p>
")]
        [InlineData(@"# Test Ref
For more information about user navigation properties, see the documentation for [User].

[User]: ./entity-and-complex-type-reference.md#UserEntity",
            @"<h1 id=""test-ref"">Test Ref</h1>
<p>For more information about user navigation properties, see the documentation for <a href=""./entity-and-complex-type-reference.md#UserEntity"" data-raw-source=""[User]"">User</a>.</p>
")]
        [InlineData(@"> [abc][1]
> > [1]


[1]: 11111",
            @"<blockquote>
<p><a href=""11111"" data-raw-source=""[abc][1]"">abc</a></p>
<blockquote>
<p><a href=""11111"" data-raw-source=""[1]"">1</a></p>
</blockquote>
</blockquote>
")]
        [InlineData(@"[a](a(b).c)",
            @"<p><a href=""a(b).c"" data-raw-source=""[a](a(b).c)"">a</a></p>
")]
        [InlineData(@"[a](a(b(c)).d 'text')",
            @"<p><a href=""a(b(c)).d"" title=""text"" data-raw-source=""[a](a(b(c)).d &#39;text&#39;)"">a</a></p>
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
<li><p>some text</p>
<pre><code>--
</code></pre></li>
</ol>
")]
        [InlineData(@"1. some text
 ```
 --
 ```",
            @"<ol>
<li>some text</li>
</ol>
<pre><code>--
</code></pre>")]
        [InlineData(@"1. some text
 > 1",
            @"<ol>
<li>some text</li>
</ol>
<blockquote>
<p>1</p>
</blockquote>
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
            @"<p><a href=""b"" data-raw-source=""[a\\](b)"">a\</a></p>
")]
        [InlineData(
            @"[a](b\\)",
            @"<p><a href=""b\"" data-raw-source=""[a](b\\)"">a</a></p>
")]
        [InlineData(
            @"<!--a-->[b](c)<!--d-->e
<!--f-->",
            @"<!--a-->[b](c)<!--d-->e
<!--f-->")]
        [InlineData(
            @"aabbcc:smile:ddee",
            @"<p>aabbcc<span class=""emoji"" shortCode=""smile"">😄</span>ddee</p>
")]
        [InlineData(
            @"aabbcc:not_emoji:ddee",
            @"<p>aabbcc:not_emoji:ddee</p>
")]
        [InlineData(
            @"# Ice cube",
            @"<h1 id=""ice-cube"">Ice cube</h1>
")]
        [InlineData(
            @"# Eazy-E",
            @"<h1 id=""eazy-e"">Eazy-E</h1>
")]
        [InlineData(
            @"# Straight Outta Compton
# Dopeman
# Express Yourself
# Dopeman",
            @"<h1 id=""straight-outta-compton"">Straight Outta Compton</h1>
<h1 id=""dopeman"">Dopeman</h1>
<h1 id=""express-yourself"">Express Yourself</h1>
<h1 id=""dopeman-1"">Dopeman</h1>
")]
        [InlineData(
            @"# ""Funky President"" by James Brown",
            @"<h1 id=""funky-president-by-james-brown"">&quot;Funky President&quot; by James Brown</h1>
")]
        [InlineData(
            @"# 中文",
            @"<h1 id=""中文"">中文</h1>
")]
        [InlineData(
            @"# sān　空格　 sān",
            @"<h1 id=""sān空格-sān"">sān　空格　 sān</h1>
")]
        [InlineData(
            @"# a-1
# a
# a",
            @"<h1 id=""a-1"">a-1</h1>
<h1 id=""a"">a</h1>
<h1 id=""a-1-1"">a</h1>
")]
        [InlineData(
            @"# 测试。用例
# 测试。用例",
            @"<h1 id=""测试用例"">测试。用例</h1>
<h1 id=""测试用例-1"">测试。用例</h1>
")]
        [InlineData(
            @"**this is bold and *italic** *",
            @"<p><strong>this is bold and *italic</strong> *</p>
")]
        [InlineData(
            @"**aaa*aa **aaa a *a aaa **",
            @"<p><em>*aaa</em>aa **aaa a *a aaa **</p>
")]
        [InlineData(
            @"__aaa_aa __aaa a _a aaa __a",
            @"<p>__aaa_aa __aaa a _a aaa __a</p>
")]
        [InlineData(
            @"***A*B*C*D**",
            @"<p><strong><em>A</em>B<em>C</em>D</strong></p>
")]
        [InlineData(
            @"* [a]: a

* [b]: b

  [c]: c",
            @"<ul>
<li></li>
<li></li>
</ul>
")]
        [InlineData(
            @"* [a][a] (b)

[a]: http://a.b/c",
            @"<ul>
<li><a href=""http://a.b/c"" data-raw-source=""[a][a]"">a</a> (b)</li>
</ul>
")]
        [InlineData(
            @"[https://github.com/dotnet/docfx/](https://github.com/dotnet/docfx/)",
            @"<p><a href=""https://github.com/dotnet/docfx/"" data-raw-source=""[https://github.com/dotnet/docfx/](https://github.com/dotnet/docfx/)"">https://github.com/dotnet/docfx/</a></p>
")]
        [InlineData(
            @"[![](myimage.png)Some description](targetfile.md)",
            @"<p><a href=""targetfile.md"" data-raw-source=""[![](myimage.png)Some description](targetfile.md)""><img src=""myimage.png"" alt="""">Some description</a></p>
")]
        [InlineData(
            @"[![](myimage.png)Some description][a]

[a]: targetfile.md",
            @"<p><a href=""targetfile.md"" data-raw-source=""[![](myimage.png)Some description][a]""><img src=""myimage.png"" alt="""">Some description</a></p>
")]
        [InlineData(
            @"[![b]Some description][a]

[a]: targetfile.md
[b]: myimage.png",
            @"<p><a href=""targetfile.md"" data-raw-source=""[![b]Some description][a]""><img src=""myimage.png"" alt=""b"">Some description</a></p>
")]
        [InlineData(
            @"[![image][b]Some description][a]

[a]: targetfile.md
[b]: myimage.png",
            @"<p><a href=""targetfile.md"" data-raw-source=""[![image][b]Some description][a]""><img src=""myimage.png"" alt=""image"">Some description</a></p>
")]
        [InlineData(
            @"> a
>
> b

>
c

>
> d
>
e",
            @"<blockquote>
<p>a</p>
<p>b</p>
</blockquote>
<blockquote>
</blockquote>
<p>c</p>
<blockquote>
<p>d</p>
</blockquote>
<p>e</p>
")]
        [InlineData(
            @"* Unordered list item 1
* Unordered list item 2
  ## This Is Heading (in list).
",
            @"<ul>
<li>Unordered list item 1</li>
<li>Unordered list item 2<h2 id=""this-is-heading-in-list"">This Is Heading (in list).</h2>
</li>
</ul>
")]
        [InlineData(@"+ a
+ b",
            @"<ul>
<li>a</li>
<li>b</li>
</ul>
")]
        [InlineData(
            @"```md
in code a.
    ```
in code b.
   ````
not in code c.
",
            @"<pre><code class=""lang-md"">in code a.
    ```
in code b.
</code></pre><p>not in code c.</p>
")]
        [InlineData(@"<div>a</div>", @"<div>a</div>")]
        [InlineData(
            @"<div>a</div>
",
            @"<div>a</div>
")]
        [InlineData(
            @"<div>a</div>

b",
            @"<div>a</div>

<p>b</p>
")]
        [InlineData(@"<input class=""a>"">", @"<input class=""a>"">")]
        [InlineData(@"<input class=""a>""
>", @"<input class=""a>""
>")]
        [InlineData(@"6. a
1. b", @"<ol start=""6"">
<li>a</li>
<li>b</li>
</ol>
")]
        [InlineData(@"[a]()", @"<p><a href="""" data-raw-source=""[a]()"">a</a></p>
")]
        [InlineData(@"<p>[a](b)</p>", @"<p>[a](b)</p>")]
        [InlineData(@"A\
", @"<p>A\</p>
")]
        [InlineData(@"A\
B", @"<p>A<br>B</p>
")]
        [InlineData(@"A  
", @"<p>A  </p>
")]
        [InlineData(@"A  
B", @"<p>A<br>B</p>
")]
        [InlineData(@"a*b\*c", @"<p>a*b*c</p>
")]
        [InlineData(@"a*b\*c*d", @"<p>a<em>b*c</em>d</p>
")]
        [InlineData(@"[a][ b] (c)
[b]: x", @"<p><a href=""x"" data-raw-source=""[a][ b]"">a</a> (c)</p>
")]
        [InlineData(@"<a />[a](#b)", @"<p><a /><a href=""#b"" data-raw-source=""[a](#b)"">a</a></p>
")]
        #endregion
        public void TestGfmInGeneral(string source, string expected)
        {
            var builder = new GfmEngineBuilder(new Options());
            var engine = builder.CreateEngine(new HtmlRenderer());
            var result = engine.Markup(source);
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        public void TestLegacyGfmInGeneral(string source, string expected)
        {
            var builder = new GfmEngineBuilder(new Options { LegacyMode = true });
            var engine = builder.CreateEngine(new HtmlRenderer());
            var result = engine.Markup(source);
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Theory]
        [Trait("Related", "Markdown")]
        #region Inline Data
        [InlineData(
            @"<pre>a</pre>
abc",
            @"<pre>a</pre>
<p>abc</p>
")]
        [InlineData(
            @"<pre id=""x"">a

b</pre>
abc",
            @"<pre id=""x"">a

b</pre>
<p>abc</p>
")]
        [InlineData(
            @"a
<pre>b

c</pre>
d",
            @"<p>a</p>
<pre>b

c</pre>
<p>d</p>
")]
        [InlineData(
            @"<pre

<a>
b</pre>
c",
            @"<pre

<a>
b</pre>
<p>c</p>
")]
        [InlineData(
            @"<pre:
a</pre>
b",
            @"<p>&lt;pre:
a</pre>
b</p>
")]
        [InlineData(
            @"<pre>
a</pre
>
b",
            @"<pre>
a</pre
>
<p>b</p>
")]
        #endregion
        public void TestPreElement(string source, string expected)
        {
            TestGfmInGeneral(source, expected);
            TestLegacyGfmInGeneral(source, expected);
        }

        [Fact]
        [Trait("Related", "Markdown")]
        public void TestListWithTab()
        {
            // 1. Prepare data
            var source = @"
0.	a

0.	b

	c

	d

0.	e
";
            var expected = @"<ol start=""0"">
<li><p>a</p>
</li>
<li><p>b</p>
<p>c</p>
<p>d</p>
</li>
<li><p>e</p>
</li>
</ol>
";
            TestGfmInGeneral(source, expected);
        }

        [Fact]
        [Trait("Related", "Markdown")]
        public void TestNoLinkWhenContainWhiteSpace()
        {
            // 1. Prepare data
            var source = @"[macro](Outlook Macro.md)";
            var expected = @"<p>[macro](Outlook Macro.md)</p>
";
            TestGfmInGeneral(source, expected);
            TestLegacyGfmInGeneral(source, expected);
        }

        [Fact]
        [Trait("Related", "Markdown")]
        public void TestEscape()
        {
            // 1. Prepare data
            var source = @"\@";
            var expected = @"<p>@</p>
";
            TestGfmInGeneral(source, expected);
            TestLegacyGfmInGeneral(source, expected);
        }

        [Fact]
        [Trait("Related", "Markdown")]
        public void TestHeadingDifference()
        {
            // 1. Prepare data
            var source = @"#aaaa";
            TestGfmInGeneral(source, @"<p>#aaaa</p>
");
            TestLegacyGfmInGeneral(source, @"<h1 id=""aaaa"">aaaa</h1>
");
        }

        [Theory]
        [Trait("Related", "Markdown")]
        [InlineData(@"<div a:b=""c"">")]
        [InlineData(@"<div a:b=""c"">
</div>")]
        public void TestTag(string html)
        {
            TestGfmInGeneral(html, html);
            TestLegacyGfmInGeneral(html, html);
        }

        [Theory]
        [Trait("Related", "Markdown")]
        #region InlineData
        [InlineData(@"a<x>", @"<p>a<x></p>
")]
        [InlineData(@"a</x>", @"<p>a</x></p>
")]
        [InlineData(@"a<x/>", @"<p>a<x/></p>
")]
        [InlineData(@"a<-x>", @"<p>a&lt;-x&gt;</p>
")]
        [InlineData(@"a<x-x>", @"<p>a<x-x></p>
")]
        [InlineData(@"a<_x>", @"<p>a&lt;_x&gt;</p>
")]
        [InlineData(@"a<x_x>", @"<p>a&lt;x_x&gt;</p>
")]
        [InlineData(@"a<x:y>", @"<p>a&lt;x:y&gt;</p>
")]
        [InlineData(@"a<x y>", @"<p>a<x y></p>
")]
        [InlineData(@"a<中文>", @"<p>a&lt;中文&gt;</p>
")]
        [InlineData(@"a<x中文>", @"<p>a&lt;x中文&gt;</p>
")]
        [InlineData(@"a<x 中文>", @"<p>a&lt;x 中文&gt;</p>
")]
        [InlineData(@"a<x y='中文'>", @"<p>a<x y='中文'></p>
")]
        [InlineData(@"a<x y:z='中文'>", @"<p>a<x y:z='中文'></p>
")]
        [InlineData(@"a<x-x y-y:z-z='中文'>", @"<p>a<x-x y-y:z-z='中文'></p>
")]
        [InlineData(@"a<x-x y_y:z_z='中文'>", @"<p>a<x-x y_y:z_z='中文'></p>
")]
        [InlineData(@"a<x _:_='中文'>", @"<p>a<x _:_='中文'></p>
")]
        [InlineData(@"a<x y='中文' z='中文'>", @"<p>a<x y='中文' z='中文'></p>
")]
        [InlineData(@"a<x
y='中文'
z='中文'>", @"<p>a<x
y='中文'
z='中文'></p>
")]
        #endregion
        public void TestTag_MoreCase(string markdown, string html)
        {
            TestGfmInGeneral(markdown, html);
            TestLegacyGfmInGeneral(markdown, html);
        }

        [Theory]
        [Trait("Related", "Markdown")]
        [InlineData("\ta")]
        [InlineData(" \ta")]
        [InlineData("  \ta")]
        [InlineData("   \ta")]
        [InlineData("    a")]
        public void TestTab(string md)
        {
            TestGfmInGeneral(md, @"<pre><code>a
</code></pre>");
        }

        [Fact]
        [Trait("Related", "Markdown")]
        public void TestNotHtml()
        {
            TestGfmInGeneral(@"<x:a>", @"<p>&lt;x:a&gt;</p>
".Replace("\r\n", "\n"));
            TestLegacyGfmInGeneral(@"<x:a>", @"<p>&lt;x:a&gt;</p>
".Replace("\r\n", "\n"));
        }

        [Fact]
        [Trait("Related", "Markdown")]
        public void TestTable_PipesInTableCell()
        {
            var source = @"
| column 1 | column 2|
| ---- | ---- |
| test 1 - fenced pipe | `dotnet test --filter ""FullyQualifiedName~TestClass1|Category=Nightly""`|
| test 2 - non-fenced, escaped pipe | dotnet test --filter ""FullyQualifiedName~TestClass1\|Category=Nightly"" |
| test 3 - non-fenced, HTML coded pipe | dotnet test --filter ""FullyQualifiedName~TestClass1&#124;Category=Nightly"" |
| test 4 - ""fenced"" with HTML `<code>` tag (the **workaround**) | <code>dotnet test --filter ""FullyQualifiedName~TestClass1&#124;Category=Nightly""</code> |
";
            var expected = @"<table>
<thead>
<tr>
<th>column 1</th>
<th>column 2</th>
</tr>
</thead>
<tbody>
<tr>
<td>test 1 - fenced pipe</td>
<td>`dotnet test --filter &quot;FullyQualifiedName~TestClass1</td>
</tr>
<tr>
<td>test 2 - non-fenced, escaped pipe</td>
<td>dotnet test --filter &quot;FullyQualifiedName~TestClass1|Category=Nightly&quot;</td>
</tr>
<tr>
<td>test 3 - non-fenced, HTML coded pipe</td>
<td>dotnet test --filter &quot;FullyQualifiedName~TestClass1&#124;Category=Nightly&quot;</td>
</tr>
<tr>
<td>test 4 - &quot;fenced&quot; with HTML <code>&lt;code&gt;</code> tag (the <strong>workaround</strong>)</td>
<td><code>dotnet test --filter &quot;FullyQualifiedName~TestClass1&#124;Category=Nightly&quot;</code></td>
</tr>
</tbody>
</table>
";
            TestGfmInGeneral(source, expected);
        }

        [Fact]
        [Trait("Related", "Markdown")]
        public void TestHtml()
        {
            // 1. Prepare data
            var source = @"
<p>Here's example of how to create an instance of **Cat** class. As T is limited with <code>class</code> and K is limited with <code>struct</code>.</p>
<pre><code class=""c#"">    var a = new Cat(object, int)();
    int catNumber = new int();
    unsafe
    {
        a.GetFeetLength(catNumber);
    }</code></pre>
<p>As you see, here we bring in <strong>pointer</strong> so we need to add <span class=""languagekeyword"">unsafe</span> keyword.</p>
";
            var expected = @"<p>Here&#39;s example of how to create an instance of <strong>Cat</strong> class. As T is limited with <code>class</code> and K is limited with <code>struct</code>.</p>
<pre><code class=""c#"">    var a = new Cat(object, int)();
    int catNumber = new int();
    unsafe
    {
        a.GetFeetLength(catNumber);
    }</code></pre>
<p>As you see, here we bring in <strong>pointer</strong> so we need to add <span class=""languagekeyword"">unsafe</span> keyword.</p>
";
            TestLegacyGfmInGeneral(source, expected);
        }

        [Fact]
        [Trait("Related", "Markdown")]
        public void TestHtml_Div()
        {
            // 1. Prepare data
            var source = @"
<div>
    <div>
        <div>aaa</div>
    </div>
</div>
";
            var expected = @"<div>
    <div>
        <div>aaa</div>
    </div>
</div>
";
            TestGfmInGeneral(source, expected);
        }

        [Fact]
        [Trait("Related", "Markdown")]
        public void TestInlineHtml_Span()
        {
            // 1. Prepare data
            var source = @"
<span>a</span>

<span id=""a"" class=""x"">a</span>
<span>b</span>
";
            var expected = @"<p><span>a</span></p>
<p><span id=""a"" class=""x"">a</span>
<span>b</span></p>
";
            TestGfmInGeneral(source, expected);
        }

        [Fact]
        [Trait("Related", "Markdown")]
        public void TestTable_WithEmptyColumn()
        {
            // 1. Prepare data
            var source = @"|   | Empty column  | Right Aligned |
|:------------ |:---------------:|-----:|
|       | some wordy text | $1600 |
|       | centered        |   $12 |
|   | are neat        |    $1 |";

            var expected = @"<table>
<thead>
<tr>
<th style=""text-align:left""></th>
<th style=""text-align:center"">Empty column</th>
<th style=""text-align:right"">Right Aligned</th>
</tr>
</thead>
<tbody>
<tr>
<td style=""text-align:left""></td>
<td style=""text-align:center"">some wordy text</td>
<td style=""text-align:right"">$1600</td>
</tr>
<tr>
<td style=""text-align:left""></td>
<td style=""text-align:center"">centered</td>
<td style=""text-align:right"">$12</td>
</tr>
<tr>
<td style=""text-align:left""></td>
<td style=""text-align:center"">are neat</td>
<td style=""text-align:right"">$1</td>
</tr>
</tbody>
</table>
";
            TestGfmInGeneral(source, expected);
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
        public void TestTable_WithEmptyCell2()
        {
            // 1. Prepare data
            var source = @"  A |  B | C 
|:-------|-------|-------| 
| A1 | B1
| A2 | |  C2 | D2 | E2
| A3 | B3 |
";

            var expected = @"<table>
<thead>
<tr>
<th style=""text-align:left"">A</th>
<th>B</th>
<th>C</th>
</tr>
</thead>
<tbody>
<tr>
<td style=""text-align:left"">A1</td>
<td>B1</td>
<td></td>
</tr>
<tr>
<td style=""text-align:left"">A2</td>
<td></td>
<td>C2</td>
</tr>
<tr>
<td style=""text-align:left"">A3</td>
<td>B3</td>
<td></td>
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
<td style=""text-align:center""><a href=""./entity-and-complex-type-reference.md#UserEntity"" data-raw-source=""[User]"">User</a></td>
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

            var expected = @"<p><a href=""girl.png"" title=""title is &quot;hello&quot; world."" data-raw-source=""[This is link text with quotation &#39; and double quotation &quot;hello&quot; world](girl.png &quot;title is &quot;hello&quot; world.&quot;)"">This is link text with quotation &#39; and double quotation &quot;hello&quot; world</a></p>
";
            TestGfmInGeneral(source, expected);
        }

        [Fact]
        [Trait("Related", "Markdown")]
        public void TestGfmHeading_WithSharpAtTheEndInTitle()
        {
            var source = @"# Language C#
# Language C# #";

            var expected = @"<h1 id=""language-c"">Language C#</h1>
<h1 id=""language-c-1"">Language C#</h1>
";

            TestLegacyGfmInGeneral(source, expected);
            TestGfmInGeneral(source, expected);
        }

        [Fact]
        [Trait("Related", "Markdown")]
        public void TestGfmFences_WithEmptyFence()
        {
            var source = @"```
```
test";

            var expected = @"<pre><code>
</code></pre><p>test</p>
";

            TestGfmInGeneral(source, expected);
            TestLegacyGfmInGeneral(source, expected);
        }
    }
}
