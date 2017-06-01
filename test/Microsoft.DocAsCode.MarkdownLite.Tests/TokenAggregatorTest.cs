// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite.Tests
{
    using Microsoft.DocAsCode.MarkdownLite;

    using Xunit;

    public class TokenAggregatorTest
    {
        [Theory]
        [Trait("Related", "Markdown")]
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
                TokenAggregator = new Head1HrAggregateToHead2(),
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
    }
}
