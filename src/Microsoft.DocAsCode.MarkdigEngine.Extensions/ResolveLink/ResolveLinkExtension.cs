// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using System.Linq;
    using System.Text;
    using Markdig;
    using Markdig.Renderers;
    using Markdig.Renderers.Html;
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
            switch (markdownObject)
            {
                case TabTitleBlock _:
                    break;

                case LinkInline linkInline:
                    linkInline.Url = linkInline.IsImage
                        ? _context.GetImageLink(linkInline.Url, linkInline, null)
                        : _context.GetLink(linkInline.Url, linkInline);
                    foreach (var subBlock in linkInline)
                    {
                        UpdateLinks(subBlock);
                    }
                    break;
                case ContainerBlock containerBlock:
                    foreach (var subBlock in containerBlock)
                    {
                        UpdateLinks(subBlock);
                    }
                    break;

                case LeafBlock leafBlock when leafBlock.Inline != null:
                    foreach (var subInline in leafBlock.Inline)
                    {
                        UpdateLinks(subInline);
                    }
                    break;

                case ContainerInline containerInline:
                    foreach (var subInline in containerInline)
                    {
                        UpdateLinks(subInline);
                    }
                    break;

                default:
                    break;
            }
        }
    }
}
