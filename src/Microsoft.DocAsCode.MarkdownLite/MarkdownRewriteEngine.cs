// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Collections.Immutable;

    public class MarkdownRewriteEngine : IMarkdownRewriteEngine
    {
        private readonly IMarkdownRewriter _rewriter;

        public MarkdownRewriteEngine(IMarkdownEngine engine, IMarkdownRewriter rewriter)
        {
            Engine = engine;
            _rewriter = rewriter;
        }

        public IMarkdownEngine Engine { get; }

        public virtual ImmutableArray<IMarkdownToken> Rewrite(ImmutableArray<IMarkdownToken> tokens)
        {
            if (_rewriter == MarkdownRewriterFactory.Null)
            {
                return tokens;
            }
            var result = tokens;
            for (int i = 0; i < tokens.Length; i++)
            {
                var token = tokens[i];
                var rewrittenToken = _rewriter.Rewrite(this, token);
                if (rewrittenToken != null &&
                    !object.ReferenceEquals(rewrittenToken, token))
                {
                    result = result.SetItem(i, rewrittenToken);
                    token = rewrittenToken;
                }
                var rewritable = token as IMarkdownRewritable<IMarkdownToken>;
                if (rewritable != null)
                {
                    rewrittenToken = rewritable.Rewrite(this);
                    if (rewrittenToken != null &&
                        !object.ReferenceEquals(rewrittenToken, token))
                    {
                        result = result.SetItem(i, rewrittenToken);
                    }
                }
            }
            return result;
        }
    }
}
