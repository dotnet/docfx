// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Collections.Immutable;

    internal sealed class MarkdownCompositeRewriter : IMarkdownRewriter
    {
        public MarkdownCompositeRewriter(ImmutableList<IMarkdownRewriter> rewriters)
        {
            Rewriters = rewriters;
        }

        public ImmutableList<IMarkdownRewriter> Rewriters { get; }

        public IMarkdownToken Rewrite(IMarkdownRewriteEngine engine, IMarkdownToken token)
        {
            if (Rewriters.Count > 0)
            {
                foreach (var rewriter in Rewriters)
                {
                    var newToken = rewriter.Rewrite(engine, token);
                    if (newToken != null)
                    {
                        return newToken;
                    }
                }
            }
            return null;
        }
    }
}
