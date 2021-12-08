// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Markdig;
using Markdig.Renderers;
using Markdig.Renderers.Html;

namespace Microsoft.Docs.MarkdigExtensions;

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
        if (!string.IsNullOrEmpty(inclusion.IncludedFilePath) && !inclusion.IncludedFilePath.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
        {
            _context.LogWarning("include-invalid", $"Invalid include link extension: '{inclusion.IncludedFilePath}'.", inclusion);
            renderer.Write(inclusion.GetRawToken());
            return;
        }

        var (content, includeFilePath) = _context.ReadFile(inclusion.IncludedFilePath, inclusion);

        if (content == null)
        {
            _context.LogWarning("include-not-found", $"Cannot resolve '{inclusion.IncludedFilePath}' relative to '{InclusionContext.File}'.", inclusion);
            renderer.Write(inclusion.GetRawToken());
            return;
        }

        if (InclusionContext.IsCircularReference(includeFilePath, out var dependencyChain))
        {
            _context.LogWarning("circular-reference", $"Build has identified file(s) referencing each other: {string.Join(" --> ", dependencyChain.Select(file => $"'{file}'"))} --> '{includeFilePath}'", inclusion);
            renderer.Write(inclusion.GetRawToken());
            return;
        }

        using (InclusionContext.PushInclusion(includeFilePath))
        {
            renderer.Write(Markdown.ToHtml(content, _inlinePipeline));
        }
    }
}
