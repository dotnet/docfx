// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using Markdig;
    using Markdig.Extensions.Yaml;
    using Markdig.Parsers;
    using Markdig.Renderers;

    public class InlineOnlyExtentsion : IMarkdownExtension
    {
        public void Setup(MarkdownPipelineBuilder pipeline)
        {
            var paragraphBlockParser = pipeline.BlockParsers.FindExact<ParagraphBlockParser>() ?? new ParagraphBlockParser();
            pipeline.BlockParsers.Clear();
            pipeline.BlockParsers.Add(paragraphBlockParser);
            pipeline.BlockParsers.Add(new YamlFrontMatterParser());
        }

        public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
        {
            if (renderer is HtmlRenderer htmlRenderer)
            {
                htmlRenderer.ImplicitParagraph = true;
            }
        }
    }
}
