// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Markdig;
using Markdig.Renderers;
using Markdig.Renderers.Html;

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions;

public class HtmlInclusionBlockRenderer : HtmlObjectRenderer<InclusionBlock>
{
    private readonly MarkdownContext _context;
    private MarkdownPipeline _pipeline;

    public HtmlInclusionBlockRenderer(MarkdownContext context, MarkdownPipeline pipeline)
    {
        _context = context;
        _pipeline = pipeline;
    }

    protected override void Write(HtmlRenderer renderer, InclusionBlock inclusion)
    {
        var (content, includeFilePath) = _context.ReadFile(inclusion.IncludedFilePath, inclusion);

        if (content == null)
        {
            _context.LogWarning("include-not-found", $"Invalid include link: '{inclusion.IncludedFilePath}'.", inclusion);
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
            renderer.Write(Markdown.ToHtml(content, _pipeline));
        }
    }
}