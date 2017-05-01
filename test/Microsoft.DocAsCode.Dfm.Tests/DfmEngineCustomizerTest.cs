// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm.Tests
{
    using System;
    using System.Collections.Generic;

    using Xunit;

    using Microsoft.DocAsCode.Build.Engine;
    using Microsoft.DocAsCode.MarkdownLite;
    using Microsoft.DocAsCode.MarkdownLite.Matchers;
    using Microsoft.DocAsCode.Plugins;

    public class DfmEngineCustomizerTest
    {
        [Fact]
        [Trait("Related", "dfm")]
        public void TestDfmEngineCustomizer()
        {
            var p = new DfmServiceProvider();
            p.DfmEngineCustomizers = new[] { new MyDfmEngineCustomizer() };
            var ms = p.CreateMarkdownService(new MarkdownServiceParameters());
            var result = ms.Markup(@"# a
# <b>c", "a.md");
            Assert.Equal(@"<h1 id=""a"" sourceFile=""a.md"" sourceStartLineNumber=""1"" sourceEndLineNumber=""1"">a</h1>
<h1 id=""b"" sourceFile=""a.md"" sourceStartLineNumber=""2"" sourceEndLineNumber=""2"">c</h1>
".Replace("\r\n", "\n"), result.Html);
            (ms as IDisposable).Dispose();
        }

        private sealed class MyDfmEngineCustomizer : IDfmEngineCustomizer
        {
            public void Customize(DfmEngineBuilder builder, IReadOnlyDictionary<string, object> parameters)
            {
                var index = builder.BlockRules.FindIndex(r => r is MarkdownHeadingBlockRule);
                builder.BlockRules = builder.BlockRules.Insert(index, new MyHeadingRule());
            }

            private sealed class MyHeadingRule : IMarkdownRule
            {
                private static readonly Matcher _HeadingMatcher =
                    Matcher.WhiteSpacesOrEmpty +
                    Matcher.Char('#').Repeat(1, 6).ToGroup("level") +
                    Matcher.WhiteSpaces +
                    '<' +
                    Matcher.AnyCharNotIn('\n', '>').RepeatAtLeast(0).ToGroup("id") +
                    '>' +
                    Matcher.AnyCharNot('\n').RepeatAtLeast(1).ToGroup("text") +
                    (Matcher.NewLine.RepeatAtLeast(1) | Matcher.EndOfString);

                public string Name => "Heading with id";

                public Matcher HeadingMatcher => _HeadingMatcher;

                public IMarkdownToken TryMatch(IMarkdownParser parser, IMarkdownParsingContext context)
                {
                    var match = context.Match(HeadingMatcher);
                    if (match?.Length > 0)
                    {
                        var sourceInfo = context.Consume(match.Length);
                        return new TwoPhaseBlockToken(
                            this,
                            parser.Context,
                            sourceInfo,
                            (p, t) => new MarkdownHeadingBlockToken(
                                t.Rule,
                                t.Context,
                                p.TokenizeInline(t.SourceInfo.Copy(match["text"].GetValue())),
                                match["id"].GetValue(),
                                match["level"].Count,
                                t.SourceInfo));
                    }
                    return null;
                }

            }
        }
    }
}
