// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using Markdig;
    using Markdig.Renderers;
    using Markdig.Syntax;
    using Markdig.Syntax.Inlines;

    public class ResolveLinkExtension : IMarkdownExtension
    {
        private readonly MarkdownContext _context;

        public ResolveLinkExtension(MarkdownContext context)
        {
            _context = context;
        }

        public void Setup(MarkdownPipelineBuilder pipeline)
        {
            pipeline.DocumentProcessed += UpdateLinks;
        }

        public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
        {
        }

        private void UpdateLinks(MarkdownObject markdownObject)
        {
            if (markdownObject == null) return;

            if (markdownObject is LinkInline linkInline && !linkInline.IsAutoLink)
            {
                linkInline.Url = _context.GetLink(linkInline.Url, InclusionContext.File, InclusionContext.RootFile, linkInline);
            }

            if (markdownObject is ContainerBlock containerBlock)
            {
                foreach (var subBlock in containerBlock)
                {
                    UpdateLinks(subBlock);
                }
            }
            else if (markdownObject is LeafBlock leafBlock)
            {
                if (leafBlock.Inline != null)
                {
                    foreach (var subInline in leafBlock.Inline)
                    {
                        UpdateLinks(subInline);
                    }
                }
            }
            else if (markdownObject is ContainerInline containerInline)
            {
                foreach (var subInline in containerInline)
                {
                    UpdateLinks(subInline);
                }
            }
        }
    }
}
