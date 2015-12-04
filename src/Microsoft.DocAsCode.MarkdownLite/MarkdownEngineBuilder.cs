// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Collections.Immutable;

    public class MarkdownEngineBuilder
    {
        public MarkdownEngineBuilder(Options options)
        {
            Options = options;
        }

        public Options Options { get; }

        public ImmutableList<IMarkdownRule> BlockRules { get; set; } = ImmutableList<IMarkdownRule>.Empty;

        public ImmutableList<IMarkdownRule> InlineRules { get; set; } = ImmutableList<IMarkdownRule>.Empty;

        public IMarkdownRewriter Rewriter { get; set; }

        protected virtual IMarkdownContext CreateParseContext()
        {
            return new MarkdownBlockContext(BlockRules, new MarkdownInlineContext(InlineRules));
        }

        public virtual MarkdownEngine CreateEngine(object renderer)
        {
            return new MarkdownEngine(CreateParseContext(), Rewriter, renderer, Options);
        }
    }
}
