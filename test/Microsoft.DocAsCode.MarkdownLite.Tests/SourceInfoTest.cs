// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite.Tests
{
    using System.Collections.Immutable;

    using Microsoft.DocAsCode.MarkdownLite;

    using Xunit;

    public class SourceInfoTest
    {
        [Fact]
        [Trait("Related", "Markdown")]
        public void TestSourceInfo_Basic()
        {
            const string File = "test.md";
            var tokens = Tokenize(@"

# HEAD1
First line.  
More line.
## HEAD2
Yeah!", File);

            Assert.Equal(5, tokens.Length);
            Assert.IsType<MarkdownHeadingBlockToken>(tokens[1]);
            Assert.IsType<MarkdownParagraphBlockToken>(tokens[2]);
            Assert.IsType<MarkdownHeadingBlockToken>(tokens[3]);
            Assert.IsType<MarkdownParagraphBlockToken>(tokens[4]);
            var para = (MarkdownParagraphBlockToken)tokens[2];
            Assert.Equal(3, para.InlineTokens.Tokens.Length);
            Assert.IsType<MarkdownTextToken>(para.InlineTokens.Tokens[0]);
            Assert.IsType<MarkdownBrInlineToken>(para.InlineTokens.Tokens[1]);
            Assert.IsType<MarkdownTextToken>(para.InlineTokens.Tokens[2]);

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

        private static ImmutableArray<IMarkdownToken> Tokenize(string markdown, string file)
        {
            var gfm = new GfmEngineBuilder(new Options()).CreateEngine(new HtmlRenderer());
            var sourceInfo = SourceInfo.Create(markdown.Replace("\r\n", "\n"), file);

            var tokens = gfm.Parser.Tokenize(sourceInfo);

            tokens = TokenHelper.CreateParagraghs(
                gfm.Parser,
                MarkdownParagraphBlockRule.Instance,
                tokens,
                true,
                sourceInfo);

            var rewriter =
                new MarkdownRewriteEngine(
                    gfm,
                    MarkdownTokenRewriterFactory.FromLambda<IMarkdownRewriteEngine, TwoPhaseBlockToken>(
                        (e, t) => t.Extract(gfm.Parser)));
            tokens = rewriter.Rewrite(tokens);
            return tokens;
        }

        [Fact]
        [Trait("Related", "Markdown")]
        public void TestSourceInfo_BlockquoteAndList()
        {
            const string File = "test.md";
            var tokens = Tokenize(@"> blockquote
> [link text](sometarget)
> 
> - list item 1
>   - list item 1-1
>   - list item 1-2
> - list item 2
>
> more para.
", File);

            Assert.Equal(1, tokens.Length);
            Assert.IsType<MarkdownBlockquoteBlockToken>(tokens[0]);
            var blockquote = (MarkdownBlockquoteBlockToken)tokens[0];
            Assert.Equal(5, blockquote.Tokens.Length);

            Assert.Equal(1, blockquote.SourceInfo.LineNumber);
            Assert.Equal(File, blockquote.SourceInfo.File);

            Assert.IsType<MarkdownParagraphBlockToken>(blockquote.Tokens[0]);
            {
                var para = (MarkdownParagraphBlockToken)blockquote.Tokens[0];

                Assert.Equal(1, para.SourceInfo.LineNumber);
                Assert.Equal(File, para.SourceInfo.File);

                Assert.Equal(2, para.InlineTokens.Tokens.Length);

                Assert.IsType<MarkdownTextToken>(para.InlineTokens.Tokens[0]);
                var text = (MarkdownTextToken)para.InlineTokens.Tokens[0];
                Assert.Equal(1, text.SourceInfo.LineNumber);
                Assert.Equal(File, text.SourceInfo.File);

                Assert.IsType<MarkdownLinkInlineToken>(para.InlineTokens.Tokens[1]);
                var link = (MarkdownLinkInlineToken)para.InlineTokens.Tokens[1];
                Assert.Equal(2, link.SourceInfo.LineNumber);
                Assert.Equal(File, link.SourceInfo.File);
            }
            Assert.IsType<MarkdownListBlockToken>(blockquote.Tokens[2]);
            {
                var list = (MarkdownListBlockToken)blockquote.Tokens[2];

                Assert.Equal(4, list.SourceInfo.LineNumber);
                Assert.Equal(File, list.SourceInfo.File);
                Assert.Equal(2, list.Tokens.Length);
                Assert.IsType<MarkdownListItemBlockToken>(list.Tokens[0]);
                {
                    var listItem = (MarkdownListItemBlockToken)list.Tokens[0];
                    Assert.Equal(4, listItem.SourceInfo.LineNumber);
                    Assert.Equal(File, listItem.SourceInfo.File);

                    Assert.IsType<MarkdownNonParagraphBlockToken>(listItem.Tokens[0]);
                    var np = (MarkdownNonParagraphBlockToken)listItem.Tokens[0];
                    Assert.Equal(1, np.Content.Tokens.Length);
                    Assert.Equal(4, np.SourceInfo.LineNumber);
                    Assert.Equal(File, np.SourceInfo.File);

                    Assert.IsType<MarkdownListBlockToken>(listItem.Tokens[1]);
                    var subList = (MarkdownListBlockToken)listItem.Tokens[1];
                    Assert.Equal(2, subList.Tokens.Length);
                    Assert.IsType<MarkdownListItemBlockToken>(subList.Tokens[0]);
                    {
                        var subListItem = (MarkdownListItemBlockToken)subList.Tokens[0];
                        Assert.Equal(5, subListItem.SourceInfo.LineNumber);
                        Assert.Equal(File, subListItem.SourceInfo.File);
                    }
                    Assert.IsType<MarkdownListItemBlockToken>(subList.Tokens[1]);
                    {
                        var subListItem = (MarkdownListItemBlockToken)subList.Tokens[1];
                        Assert.Equal(6, subListItem.SourceInfo.LineNumber);
                        Assert.Equal(File, subListItem.SourceInfo.File);
                    }
                }

                Assert.IsType<MarkdownListItemBlockToken>(list.Tokens[1]);
                {
                    var listItem = (MarkdownListItemBlockToken)list.Tokens[1];
                    Assert.Equal(7, listItem.SourceInfo.LineNumber);
                    Assert.Equal(File, listItem.SourceInfo.File);
                }
            }

            Assert.IsType<MarkdownParagraphBlockToken>(blockquote.Tokens[4]);
            {
                var para = (MarkdownParagraphBlockToken)blockquote.Tokens[4];
                Assert.Equal(9, para.SourceInfo.LineNumber);
                Assert.Equal(File, para.SourceInfo.File);
            }
        }

        [Fact]
        [Trait("Related", "Markdown")]
        public void TestSourceInfo_Table()
        {
            const string File = "test.md";
            var tokens = Tokenize(@"
| H1 | H2 |
|----|----|
|R1C1|R1C2|
|R2C1|R2C2|
", File);

            Assert.Equal(2, tokens.Length);
            Assert.IsType<MarkdownTableBlockToken>(tokens[1]);
            var table = (MarkdownTableBlockToken)tokens[1];
            Assert.Equal(2, table.Header.Length);
            Assert.Equal(2, table.Cells.Length);

            Assert.Equal(2, table.SourceInfo.LineNumber);
            Assert.Equal(File, table.SourceInfo.File);

            Assert.Equal(2, table.Header[0].SourceInfo.LineNumber);
            Assert.Equal(2, table.Header[1].SourceInfo.LineNumber);

            Assert.Equal(4, table.Cells[0][0].SourceInfo.LineNumber);
            Assert.Equal(4, table.Cells[0][1].SourceInfo.LineNumber);
            Assert.Equal(5, table.Cells[1][0].SourceInfo.LineNumber);
            Assert.Equal(5, table.Cells[1][1].SourceInfo.LineNumber);
        }
    }
}
