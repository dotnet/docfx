// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System;

    public sealed class TwoPhaseBlockToken : IMarkdownToken
    {
        private readonly Func<IMarkdownParser, TwoPhaseBlockToken, IMarkdownToken> _extractor;

        public TwoPhaseBlockToken(
            IMarkdownRule rule,
            IMarkdownContext context,
            string rawMarkdown,
            LineInfo lineInfo,
            Func<IMarkdownParser, TwoPhaseBlockToken, IMarkdownToken> extractor)
        {
            Rule = rule;
            Context = context;
            RawMarkdown = rawMarkdown;
            LineInfo = lineInfo;
            _extractor = extractor;
        }

        public IMarkdownRule Rule { get; }

        public IMarkdownContext Context { get; }

        public string RawMarkdown { get; }

        public LineInfo LineInfo { get; }

        public IMarkdownToken Extract(IMarkdownParser parser)
        {
            var c = parser.SwitchContext(Context);
            var result = _extractor(parser, this);
            parser.SwitchContext(c);
            return result;
        }
    }
}
