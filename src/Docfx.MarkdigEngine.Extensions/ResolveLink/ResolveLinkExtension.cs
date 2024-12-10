// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Markdig;
using Markdig.Renderers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Docfx.MarkdigEngine.Extensions;

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
            case TabTitleBlock:
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

            case LeafBlock { Inline: not null } leafBlock:
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
