// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite.Tests
{
    using System.Collections.Immutable;

    using Microsoft.DocAsCode.MarkdownLite;

    using Xunit;

    [Trait("Related", "Markdown")]
    public class TokenAggregatorTest
    {
        [Theory]
        [InlineData(
            @"# Test",
            @"<h1 id=""test"">Test</h1>
")]
        [InlineData(
            @"# Test
- - -",
            @"<h2 id=""test"">Test</h2>
")]
        [InlineData(
            @"# Test1
# Test2
- - -
# Test3
* * *
# Test4",
            @"<h1 id=""test1"">Test1</h1>
<h2 id=""test2"">Test2</h2>
<h2 id=""test3"">Test3</h2>
<h1 id=""test4"">Test4</h1>
")]
        [InlineData(
            @"# Test1
## Test2
- - -
# Test3
* * *
# Test4",
            @"<h1 id=""test1"">Test1</h1>
<h2 id=""test2"">Test2</h2>
<hr>
<h2 id=""test3"">Test3</h2>
<h1 id=""test4"">Test4</h1>
")]
        public void TestAggregateHead1_Hr_To_Head2(string source, string expected)
        {
            var builder = new GfmEngineBuilder(new Options())
            {
                TokenAggregators = ImmutableList.Create<IMarkdownTokenAggregator>(new Head1HrAggregateToHead2()),
            };
            var engine = builder.CreateEngine(new HtmlRenderer());
            var result = engine.Markup(source);
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Theory]
        [InlineData(
            @"P1

P2",
            @"<p>P1<br>P2</p>
")]
        [InlineData(
            @"P1

P2

P3",
            @"<p>P1<br>P2<br>P3</p>
")]
        [InlineData(
            @"P1

P2
***
P3

P4",
            @"<p>P1<br>P2</p>
<hr>
<p>P3<br>P4</p>
")]
        public void TestAggregatePara_Para_To_Para(string source, string expected)
        {
            var builder = new GfmEngineBuilder(new Options())
            {
                TokenAggregators = ImmutableList.Create<IMarkdownTokenAggregator>(new ParaParaAggregateToPara()),
            };
            var engine = builder.CreateEngine(new HtmlRenderer());
            var result = engine.Markup(source);
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        [Theory]
        [InlineData(
            @"# Test
- - -
P1

P2",
            @"<h2 id=""test"">Test</h2>
<p>P1<br>P2</p>
")]
        public void TestCompositeAggregate(string source, string expected)
        {
            var builder = new GfmEngineBuilder(new Options())
            {
                TokenAggregators = ImmutableList.Create<IMarkdownTokenAggregator>(
                    new ParaParaAggregateToPara(),
                    new Head1HrAggregateToHead2()),
            };
            var engine = builder.CreateEngine(new HtmlRenderer());
            var result = engine.Markup(source);
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        private sealed class Head1HrAggregateToHead2 : MarkdownTokenAggregator<MarkdownHeadingBlockToken>
        {
            protected override bool AggregateCore(MarkdownHeadingBlockToken headToken, IMarkdownTokenAggregateContext context)
            {
                if (headToken.Depth == 1)
                {
                    var next = context.LookAhead(1);
                    if (next is MarkdownHrBlockToken)
                    {
                        context.AggregateTo(
                            new MarkdownHeadingBlockToken(
                                headToken.Rule,
                                headToken.Context,
                                headToken.Content,
                                headToken.Id,
                                2,
                                headToken.SourceInfo), 2);
                        return true;
                    }
                }
                return false;
            }
        }

        private sealed class ParaParaAggregateToPara : MarkdownTokenAggregator<MarkdownParagraphBlockToken>
        {
            protected override bool AggregateCore(MarkdownParagraphBlockToken headToken, IMarkdownTokenAggregateContext context)
            {
                var tokenCount = 1;
                var next = context.LookAhead(tokenCount);
                while (next is MarkdownNewLineBlockToken)
                {
                    tokenCount++;
                    next = context.LookAhead(tokenCount);
                }

                if (next is MarkdownParagraphBlockToken nextPara)
                {
                    context.AggregateTo(
                        new MarkdownParagraphBlockToken(
                            headToken.Rule,
                            headToken.Context,
                            new InlineContent(
                                headToken.InlineTokens.Tokens
                                    .Add(new MarkdownBrInlineToken(headToken.Rule, headToken.InlineTokens.Tokens[0].Context, headToken.SourceInfo))
                                    .AddRange(nextPara.InlineTokens.Tokens)),
                            headToken.SourceInfo),
                        tokenCount + 1);
                    return true;
                }
                return false;
            }
        }
    }
}
