// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite.Tests
{
    using System;
    using System.Collections.Immutable;

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
                    (IMarkdownRewriteEngine e, MarkdownHeadingBlockToken t) => new MarkdownIgnoreToken(t.Rule, t.Context, t.RawMarkdown, t.LineInfo) // ignore all heading
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
                            t.Depth <= 2 ? new MarkdownHeadingBlockToken(t.Rule, t.Context, t.Content, t.Id, t.Depth + 1, t.RawMarkdown, t.LineInfo) : null),
                    MarkdownTokenRewriterFactory.FromLambda(
                        (IMarkdownRewriteEngine e, MarkdownHeadingBlockToken t) =>
                            t.Depth == 3 ? new MarkdownHeadingBlockToken(t.Rule, t.Context, t.Content, t.Id, t.Depth + 1, t.RawMarkdown, t.LineInfo) : null)
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
                            (IMarkdownRewriteEngine e, MarkdownHeadingBlockToken t) => new MarkdownTextToken(t.Rule, t.Context, t.RawMarkdown, t.RawMarkdown, t.LineInfo)
                        ),
                        MarkdownTokenRewriterFactory.FromLambda(
                            (IMarkdownRewriteEngine e, MarkdownTextToken t) => new MarkdownHeadingBlockToken(t.Rule, t.Context, new InlineContent(ImmutableArray<IMarkdownToken>.Empty), "aaaa", 1, t.RawMarkdown, t.LineInfo)
                        )
                    ),
                10);
            var engine = builder.CreateEngine(new HtmlRenderer());
            Assert.Throws<InvalidOperationException>(() => engine.Markup(source));
        }
    }
}
