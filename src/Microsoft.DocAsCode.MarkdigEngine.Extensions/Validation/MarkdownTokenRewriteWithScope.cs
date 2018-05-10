// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using Markdig.Syntax;

    public class MarkdownTokenRewriteWithScope : IMarkdownObjectRewriter
    {
        public IMarkdownObjectRewriter Inner { get; }
        public string Scope { get; }

        private MarkdownContext _context;

        public MarkdownTokenRewriteWithScope(IMarkdownObjectRewriter inner, string scope, MarkdownContext context)
        {
            Inner = inner;
            Scope = scope;

            _context = context;
        }

        public void PostProcess(IMarkdownObject markdownObject)
        {
            using (string.IsNullOrEmpty(Scope) ? null : _context.SetLoggerScope(Scope))
            {
                Inner.PostProcess(markdownObject);
            }
        }

        public void PreProcess(IMarkdownObject markdownObject)
        {
            using (string.IsNullOrEmpty(Scope) ? null : _context.SetLoggerScope(Scope))
            {
                Inner.PreProcess(markdownObject);
            }
        }

        public IMarkdownObject Rewrite(IMarkdownObject markdownObject)
        {
            using (string.IsNullOrEmpty(Scope) ? null : _context.SetLoggerScope(Scope))
            {
                return Inner.Rewrite(markdownObject);
            }
        }
    }
}