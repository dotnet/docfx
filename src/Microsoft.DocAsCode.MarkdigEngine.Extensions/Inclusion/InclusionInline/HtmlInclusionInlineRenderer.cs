// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using System.IO;
    using System.Text;
    using Markdig;
    using Markdig.Parsers;
    using Markdig.Renderers;
    using Markdig.Renderers.Html;
    using Microsoft.DocAsCode.Common;

    public class HtmlInclusionInlineRenderer : HtmlObjectRenderer<InclusionInline>
    {
        private readonly MarkdownContext _context;
        private readonly MarkdownPipeline _pipeline;
        private readonly MarkdownPipeline _inlinePipeline;

        public HtmlInclusionInlineRenderer(MarkdownContext context, MarkdownPipeline pipeline)
        {
            _context = context;
            _pipeline = pipeline;
            _inlinePipeline = CreateInlineOnlyPipeline(pipeline);
        }

        protected override void Write(HtmlRenderer renderer, InclusionInline inclusion)
        {
            var (content, includeFilePath) = _context.ReadFile(inclusion.IncludedFilePath, InclusionContext.File);

            if (content == null)
            {
                Logger.LogWarning($"Cannot resolve '{inclusion.IncludedFilePath}' relative to '{InclusionContext.File}'.");
                renderer.Write(inclusion.GetRawToken());
                return;
            }

            if (InclusionContext.IsCircularReference(includeFilePath, out var dependencyChain))
            {
                Logger.LogWarning($"Found circular reference: {string.Join(" -> ", dependencyChain)}\"");
                renderer.Write(inclusion.GetRawToken());
                return;
            }

            using (InclusionContext.PushFile(includeFilePath))
            {
                renderer.Write(RenderInline(content));
            }
        }

        private static MarkdownPipeline CreateInlineOnlyPipeline(MarkdownPipeline pipeline)
        {
            var builder = new MarkdownPipelineBuilder();

            foreach (var extension in pipeline.Extensions)
            {
                extension.Setup(builder);
            }

            // Force it into a single paragragh
            var paragraphBlockParser = builder.BlockParsers.FindExact<ParagraphBlockParser>() ?? new ParagraphBlockParser();
            builder.BlockParsers.Clear();
            builder.BlockParsers.Add(paragraphBlockParser);

            return builder.Build();
        }

        private string RenderInline(string content)
        {
            var document = Markdown.Parse(content, _inlinePipeline);
            var result = new StringBuilder();

            using (var writer = new StringWriter(result))
            {
                var renderer = new HtmlRenderer(writer);

                _pipeline.Setup(renderer);

                // Render with no <p></p>
                renderer.ImplicitParagraph = true;

                renderer.Render(document);
            }

            return result.ToString();
        }
    }
}