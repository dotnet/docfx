// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using System.Linq;
    using Markdig;
    using Markdig.Renderers;
    using Markdig.Renderers.Html;
    using Microsoft.DocAsCode.Common;

    public class HtmlInclusionInlineRenderer : HtmlObjectRenderer<InclusionInline>
    {
        private readonly MarkdownContext _context;
        private readonly MarkdownPipeline _pipeline;

        public HtmlInclusionInlineRenderer(MarkdownContext context, MarkdownPipeline pipeline)
        {
            _context = context;
            _pipeline = pipeline;
        }

        protected override void Write(HtmlRenderer renderer, InclusionInline inclusion)
        {
            var (content, includeFilePath) = _context.ReadFile(inclusion.Context.IncludedFilePath, _context.File);

            if (content == null)
            {
                Logger.LogWarning($"Cannot resolve '{inclusion.Context.IncludedFilePath}' relative to '{_context.File}'.");
                renderer.Write(inclusion.Context.GetRaw());
                return;
            }

            if (_context.RecursionDetector.Contains(includeFilePath))
            {
                Logger.LogWarning($"Found circular reference: {string.Join(" -> ", _context.RecursionDetector)} -> {includeFilePath}\"");
                renderer.Write(inclusion.Context.GetRaw());
                return;
            }

            _context.Dependencies.Add(includeFilePath);

            var context = new MarkdownContext(
                includeFilePath,
                true,
                _context.EnableSourceInfo,
                _context.Tokens,
                _context.Mvb,
                _context.ReadFile,
                _context.GetLink,
                _context.GetFilePath,
                _context.RecursionDetector,
                _context.Dependencies);

            var pipeline = new MarkdownPipelineBuilder()
                .UseDocfxExtensions(context)
                .Build();

            // Do not need to check if content is a single paragragh
            // context.IsInline = true will force it into a single paragragh and render with no <p></p>
            renderer.Write(Markdown.ToHtml(content, pipeline));
        }
    }
}