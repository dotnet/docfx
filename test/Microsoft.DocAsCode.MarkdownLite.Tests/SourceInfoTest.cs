// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite.Tests
{
    using System.Linq;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    using Microsoft.DocAsCode.MarkdownLite;

    using Xunit;

    public class SourceInfoTest
    {
        [Fact]
        [Trait("Related", "Markdown")]
        [Trait("Related", "Markdown")]
        public void TestSourceInfo_Basic()
        {
            const string File = "test.md";
            var gfm = new GfmEngineBuilder(new Options()).CreateEngine(new HtmlRenderer());
            var tokens = gfm.Parser.Tokenize(
                SourceInfo.Create(@"

# HEAD1
First line.  
More line.
## HEAD2
Yeah!".Replace("\r\n", "\n"), File)).ToArray();
            var rewriter = MarkdownTokenRewriterFactory.FromLambda<IMarkdownRewriteEngine, TwoPhaseBlockToken>(
                (e, t) => t.Extract(gfm.Parser));
            for (int i = 0; i < tokens.Length; i++)
            {
                tokens[i] = rewriter.Rewrite(gfm.RewriteEngine, tokens[i]) ?? tokens[i];
            }

            Assert.Equal(5, tokens.Length);
            Assert.IsType<MarkdownNewLineBlockToken>(tokens[0]);
            Assert.IsType<MarkdownHeadingBlockToken>(tokens[1]);
            Assert.IsType<MarkdownParagraphBlockToken>(tokens[2]);
            Assert.IsType<MarkdownHeadingBlockToken>(tokens[3]);
            Assert.IsType<MarkdownParagraphBlockToken>(tokens[4]);
            var para = (MarkdownParagraphBlockToken)tokens[2];
            Assert.Equal(3, para.InlineTokens.Tokens.Length);
            Assert.IsType<MarkdownTextToken>(para.InlineTokens.Tokens[0]);
            Assert.IsType<MarkdownBrInlineToken>(para.InlineTokens.Tokens[1]);
            Assert.IsType<MarkdownTextToken>(para.InlineTokens.Tokens[2]);

            Assert.Equal(1, tokens[0].SourceInfo.LineNumber);
            Assert.Equal(File, tokens[0].SourceInfo.File);
            Assert.Equal("\n\n", tokens[0].SourceInfo.Markdown);

            Assert.Equal(3, tokens[1].SourceInfo.LineNumber);
            Assert.Equal(File, tokens[1].SourceInfo.File);
            Assert.Equal("# HEAD1\n", tokens[1].SourceInfo.Markdown);

            Assert.Equal(4, tokens[2].SourceInfo.LineNumber);
            Assert.Equal(File, tokens[2].SourceInfo.File);
            Assert.Equal("First line.  \nMore line.\n", tokens[2].SourceInfo.Markdown);

            Assert.Equal(4, para.InlineTokens.Tokens[0].SourceInfo.LineNumber);
            Assert.Equal(File, para.InlineTokens.Tokens[0].SourceInfo.File);
            Assert.Equal("First line.", para.InlineTokens.Tokens[0].SourceInfo.Markdown);

            Assert.Equal(4, para.InlineTokens.Tokens[1].SourceInfo.LineNumber);
            Assert.Equal(File, para.InlineTokens.Tokens[1].SourceInfo.File);
            Assert.Equal("  \n", para.InlineTokens.Tokens[1].SourceInfo.Markdown);

            Assert.Equal(5, para.InlineTokens.Tokens[2].SourceInfo.LineNumber);
            Assert.Equal(File, para.InlineTokens.Tokens[2].SourceInfo.File);
            Assert.Equal("More line.", para.InlineTokens.Tokens[2].SourceInfo.Markdown);

            Assert.Equal(6, tokens[3].SourceInfo.LineNumber);
            Assert.Equal(File, tokens[3].SourceInfo.File);
            Assert.Equal("## HEAD2\n", tokens[3].SourceInfo.Markdown);

            Assert.Equal(7, tokens[4].SourceInfo.LineNumber);
            Assert.Equal(File, tokens[4].SourceInfo.File);
            Assert.Equal("Yeah!", tokens[4].SourceInfo.Markdown);
        }
    }
}
