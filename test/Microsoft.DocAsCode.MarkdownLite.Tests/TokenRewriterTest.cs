// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite.Tests
{
    using System;
    using System.Collections.Immutable;
    using System.Linq;

    using Microsoft.DocAsCode.MarkdownLite;

    using Xunit;

    public class TokenRewriterTest
    {
        [Fact]
        [Trait("Related", "Markdown")]
        public void TestGfmWithValidator()
        {
            const string source = "#Hello World";
            const string expected = "<h1 id=\"hello-world\">Hello World</h1>\n";
            const string expectedMessage = "a space is expected after '#'";
            string message = null;
            var builder = new GfmEngineBuilder(new Options
            {
                LegacyMode = true,
            });
            builder.Rewriter =
                MarkdownTokenRewriterFactory.FromValidators(
                    MarkdownTokenValidatorFactory.FromLambda(
                        (MarkdownHeadingBlockToken token) =>
                        {
                            if (!token.SourceInfo.Markdown.StartsWith("# "))
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
        public void TestGfmWithValidatorWithContext()
        {
            const string source = @"# Title-1
# Title-2";
            const string expected = @"<h1 id=""title-1"">Title-1</h1>
<h1 id=""title-2"">Title-2</h1>
";
            const string expectedMessage = "expected one title in one document.";
            string message = null;
            var builder = new GfmEngineBuilder(new Options());
            builder.Rewriter =
                MarkdownTokenRewriterFactory.FromValidators(
                    MarkdownTokenValidatorFactory.FromLambda(
                        (MarkdownHeadingBlockToken token) =>
                        {
                            var re = MarkdownTokenValidatorContext.CurrentRewriteEngine;
                            if (token.Depth == 1)
                            {
                                re.SetVariable("count", (int)re.GetVariable("count") + 1);
                            }
                        },
                        re =>
                        {
                            re.SetVariable("count", 0);
                            re.SetPostProcess("h1 count", re1 =>
                            {
                                if ((int)re.GetVariable("count") != 1)
                                {
                                    message = expectedMessage;
                                }
                            });
                        }));
            var engine = builder.CreateEngine(new HtmlRenderer());
            var result = engine.Markup(source);
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
            Assert.Equal(expectedMessage, message);
        }

        [Fact]
        [Trait("Related", "Markdown")]
        public void TestGfmWithValidatorWithQuery()
        {
            const string source = @"abc (not match)

- abc (match)
- a*b*c (match)
- xyz
- x

> a**b**c (not match)";
            const string expected = @"<p>abc (not match)</p>
<ul>
<li>abc (match)</li>
<li>a<em>b</em>c (match)</li>
<li>xyz</li>
<li>x</li>
</ul>
<blockquote>
<p>a<strong>b</strong>c (not match)</p>
</blockquote>
";
            int matchCount = 0;
            var builder = new GfmEngineBuilder(new Options());
            builder.Rewriter =
                MarkdownTokenRewriterFactory.FromValidators(
                    MarkdownTokenValidatorFactory.FromLambda(
                        (MarkdownListItemBlockToken token) =>
                        {
                            var text = string.Concat(from t in token.Descendants<MarkdownTextToken>() select t.Content);
                            if (text.Contains("abc"))
                            {
                                matchCount++;
                            }
                        }));
            var engine = builder.CreateEngine(new HtmlRenderer());
            var result = engine.Markup(source);
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
            Assert.Equal(2, matchCount);
        }

        [Fact]
        [Trait("Related", "Markdown")]
        public void TestGfmWithValidatorWithParents()
        {
            const string source = @"# abc
> *abc*

- abc

abc
";
            const string expected = @"<h1 id=""abc"">abc</h1>
<blockquote>
<p><em>abc</em></p>
</blockquote>
<ul>
<li>abc</li>
</ul>
<p>abc</p>
";
            int headingTextCount = 0;
            int blockquoteTextCount = 0;
            int listTextCount = 0;
            int paraTextCount = 0;
            var builder = new GfmEngineBuilder(new Options());
            builder.Rewriter =
                MarkdownTokenRewriterFactory.FromValidators(
                    MarkdownTokenValidatorFactory.FromLambda(
                        (MarkdownTextToken token) =>
                        {
                            if (token.Content == "abc")
                            {
                                var re = MarkdownTokenValidatorContext.CurrentRewriteEngine;
                                foreach (var parent in re.GetParents())
                                {
                                    if (parent is MarkdownHeadingBlockToken)
                                    {
                                        headingTextCount++;
                                    }
                                    else if (parent is MarkdownBlockquoteBlockToken)
                                    {
                                        blockquoteTextCount++;
                                    }
                                    else if (parent is MarkdownListItemBlockToken)
                                    {
                                        listTextCount++;
                                    }
                                    else if (parent is MarkdownParagraphBlockToken)
                                    {
                                        paraTextCount++;
                                    }
                                }
                            }
                        }));
            var engine = builder.CreateEngine(new HtmlRenderer());
            var result = engine.Markup(source);
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
            Assert.Equal(1, headingTextCount);
            Assert.Equal(1, blockquoteTextCount);
            Assert.Equal(1, listTextCount);
            Assert.Equal(2, paraTextCount);
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
";

            var builder = new GfmEngineBuilder(new Options());
            builder.Rewriter =
                MarkdownTokenRewriterFactory.FromLambda(
                    (IMarkdownRewriteEngine e, MarkdownHeadingBlockToken t) => new MarkdownIgnoreToken(t.Rule, t.Context, t.SourceInfo) // ignore all heading
                );
            var engine = builder.CreateEngine(new HtmlRenderer());
            var result = engine.Markup(source);
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Fact]
        [Trait("Related", "Markdown")]
        public void TestGfmWithSequenceRewrite()
        {
            const string source = @"
# A
## B
### C";
            const string expected = @"<h2 id=""a"">A</h2>
<h4 id=""b"">B</h4>
<h4 id=""c"">C</h4>
";

            var builder = new GfmEngineBuilder(new Options());
            builder.Rewriter =
                MarkdownTokenRewriterFactory.Sequence(
                    MarkdownTokenRewriterFactory.FromLambda(
                        (IMarkdownRewriteEngine e, MarkdownHeadingBlockToken t) =>
                            t.Depth <= 2 ? new MarkdownHeadingBlockToken(t.Rule, t.Context, t.Content, t.Id, t.Depth + 1, t.SourceInfo) : null),
                    MarkdownTokenRewriterFactory.FromLambda(
                        (IMarkdownRewriteEngine e, MarkdownHeadingBlockToken t) =>
                            t.Depth == 3 ? new MarkdownHeadingBlockToken(t.Rule, t.Context, t.Content, t.Id, t.Depth + 1, t.SourceInfo) : null)
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
                            (IMarkdownRewriteEngine e, MarkdownHeadingBlockToken t) => new MarkdownTextToken(t.Rule, t.Context, t.SourceInfo.Markdown, t.SourceInfo)
                        ),
                        MarkdownTokenRewriterFactory.FromLambda(
                            (IMarkdownRewriteEngine e, MarkdownTextToken t) => new MarkdownHeadingBlockToken(t.Rule, t.Context, new InlineContent(ImmutableArray<IMarkdownToken>.Empty), "aaaa", 1, t.SourceInfo)
                        )
                    ),
                10);
            var engine = builder.CreateEngine(new HtmlRenderer());
            Assert.Throws<InvalidOperationException>(() => engine.Markup(source));
        }
    }
}
