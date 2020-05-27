// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Tests
{
    using MarkdigEngine.Extensions;

    using Markdig;
    using Markdig.Syntax;
    using Markdig.Syntax.Inlines;
    using Xunit;

    public class AggregatorTest
    {
        [Theory]
        [InlineData(
    @"# Test",
    @"<h1>Test</h1>
")]
        [InlineData(
    @"# Test
- - -",
    @"<h2>Test</h2>
")]
        [InlineData(
    @"# Test1
# Test2
- - -
# Test3
* * *
# Test4",
    @"<h1>Test1</h1>
<h2>Test2</h2>
<h2>Test3</h2>
<h1>Test4</h1>
")]
        [InlineData(
    @"# Test1
## Test2
- - -
# Test3
* * *
# Test4",
    @"<h1>Test1</h1>
<h2>Test2</h2>
<hr />
<h2>Test3</h2>
<h1>Test4</h1>
")]
        public void TestAggregateHead1_Hr_To_Head2(string content, string expected)
        {
            TestAggregator(content, expected, new Head1HrAggregateToHead2());
        }

        [Theory]
        [InlineData(
    @"P1

P2",
    @"<p>P1
P2</p>
")]
        [InlineData(
    @"P1

P2

P3",
    @"<p>P1
P2
P3</p>
")]
        [InlineData(
    @"P1

P2
***
P3

P4",
    @"<p>P1
P2</p>
<hr />
<p>P3
P4</p>
")]
        public void TestAggregatePara_Para_To_Para(string content, string expected)
        {
            TestAggregator(content, expected, new ParaParaAggregateToPara());
        }

        private sealed class Head1HrAggregateToHead2 : BlockAggregator<HeadingBlock>
        {
            protected override bool AggregateCore(HeadingBlock headBlock, BlockAggregateContext context)
            {
                if (headBlock.Level == 1)
                {
                    var next = context.LookAhead(1);
                    if (next is ThematicBreakBlock block)
                    {
                        var newBlock = headBlock;
                        newBlock.Level = 2;
                        context.AggregateTo(newBlock, 2);
                        return true;
                    }
                }

                return false;
            }
        }

        private sealed class ParaParaAggregateToPara : BlockAggregator<ParagraphBlock>
        {
            protected override bool AggregateCore(ParagraphBlock para, BlockAggregateContext context)
            {
                var next = context.LookAhead(1);
                if (next is ParagraphBlock nextPara)
                {
                    para.Inline.AppendChild(new LineBreakInline());
                    nextPara.Inline.MoveChildrenAfter(para.Inline.LastChild);
                    context.AggregateTo(para, 2);

                    return true;
                }

                return false;
            }
        }

        private void TestAggregator(string content, string expected, IBlockAggregator blockAggregator)
        {
            var visitor = new MarkdownDocumentAggregatorVisitor(blockAggregator);
            var pipelineBuilder = new MarkdownPipelineBuilder();
            pipelineBuilder.DocumentProcessed += document => visitor.Visit(document);

            var pipeline = pipelineBuilder.Build();
            var html = Markdown.ToHtml(content, pipeline);

            Assert.Equal(expected.Replace("\r\n", "\n"), html);
        }
    }
}
