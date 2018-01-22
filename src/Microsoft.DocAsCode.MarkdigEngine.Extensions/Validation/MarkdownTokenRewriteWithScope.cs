// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using Markdig.Syntax;
    using Microsoft.DocAsCode.Common;

    public class MarkdownTokenRewriteWithScope : IMarkdownObjectRewriter
    {
        public IMarkdownObjectRewriter Inner { get; }
        public string Scope { get; }

        public MarkdownTokenRewriteWithScope(IMarkdownObjectRewriter inner, string scope)
        {
            Inner = inner;
            Scope = scope;
        }

        public void PostProcess(IMarkdownObject markdownObject)
        {
            using (string.IsNullOrEmpty(Scope) ? null : new LoggerPhaseScope(Scope))
            {
                Inner.PostProcess(markdownObject);
            }
        }

        public void PreProcess(IMarkdownObject markdownObject)
        {
            using (string.IsNullOrEmpty(Scope) ? null : new LoggerPhaseScope(Scope))
            {
                Inner.PreProcess(markdownObject);
            }
        }

        public IMarkdownObject Rewrite(IMarkdownObject markdownObject)
        {
            using (string.IsNullOrEmpty(Scope) ? null : new LoggerPhaseScope(Scope))
            {
                return Inner.Rewrite(markdownObject);
            }
        }
    }
}