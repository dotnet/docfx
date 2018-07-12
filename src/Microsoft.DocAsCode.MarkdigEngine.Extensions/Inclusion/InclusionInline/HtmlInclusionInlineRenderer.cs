// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using Markdig;
    using Markdig.Renderers;
    using Markdig.Renderers.Html;

    public class HtmlInclusionInlineRenderer : HtmlObjectRenderer<InclusionInline>
    {
        private readonly MarkdownContext _context;
        private readonly MarkdownPipeline _inlinePipeline;

        public HtmlInclusionInlineRenderer(MarkdownContext context, MarkdownPipeline inlinePipeline)
        {
            _context = context;
            _inlinePipeline = inlinePipeline;
        }

        protected override void Write(HtmlRenderer renderer, InclusionInline inclusion)
        {
            var (content, includeFilePath) = _context.ReadFile(inclusion.IncludedFilePath, InclusionContext.File);

            if (content == null)
            {
                _context.LogWarning("include-not-found", $"Cannot resolve '{inclusion.IncludedFilePath}' relative to '{InclusionContext.File}'.");
                renderer.Write(inclusion.GetRawToken());
                return;
            }

            if (InclusionContext.IsCircularReference(includeFilePath, out var dependencyChain))
            {
                _context.LogWarning("circular-reference", $"Found circular reference: {string.Join(" --> ", dependencyChain)} --> {includeFilePath}");
                renderer.Write(inclusion.GetRawToken());
                return;
            }

            using (InclusionContext.PushFile(includeFilePath))
            {
                renderer.Write(Markdown.ToHtml(content, _inlinePipeline));
            }
        }
    }
}