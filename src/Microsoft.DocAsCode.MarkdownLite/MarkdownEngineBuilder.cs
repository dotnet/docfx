// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System;
    using System.Collections.Immutable;

    /// <summary>
    /// A builder for create an instance of <see cref="IMarkdownEngine"/>
    /// </summary>
    public class MarkdownEngineBuilder
    {
        public MarkdownEngineBuilder(Options options)
        {
            Options = options;
        }

        /// <summary>
        /// The options.
        /// </summary>
        public Options Options { get; }

        /// <summary>
        /// The block rules.
        /// </summary>
        public ImmutableList<IMarkdownRule> BlockRules { get; set; } = ImmutableList<IMarkdownRule>.Empty;

        /// <summary>
        /// The inline rules.
        /// </summary>
        public ImmutableList<IMarkdownRule> InlineRules { get; set; } = ImmutableList<IMarkdownRule>.Empty;

        /// <summary>
        /// The markdown token rewriter.
        /// </summary>
        public IMarkdownTokenRewriter Rewriter { get; set; }

        /// <summary>
        /// The markdown token tree validator.
        /// </summary>
        public IMarkdownTokenTreeValidator TokenTreeValidator { get; set; }

        public ImmutableList<IMarkdownTokenAggregator> TokenAggregators { get; set; } = ImmutableList<IMarkdownTokenAggregator>.Empty;

        /// <summary>
        /// Create markdown paring context.
        /// </summary>
        /// <returns>a instance of <see cref="IMarkdownContext"/></returns>
        public virtual IMarkdownContext CreateParseContext()
        {
            return new MarkdownBlockContext(BlockRules, new MarkdownInlineContext(InlineRules));
        }

        /// <summary>
        /// Create an instance of <see cref="IMarkdownEngine"/>
        /// </summary>
        /// <param name="renderer">the renderer.</param>
        /// <returns>an instance of <see cref="IMarkdownEngine"/></returns>
        public virtual IMarkdownEngine CreateEngine(object renderer)
        {
            return new MarkdownEngine(CreateParseContext(), Rewriter, renderer, Options)
            {
                TokenTreeValidator = TokenTreeValidator,
                TokenAggregators = TokenAggregators,
            };
        }
    }
}
