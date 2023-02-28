// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using Markdig;
    using Markdig.Extensions.Yaml;
    using Markdig.Parsers;
    using Markdig.Renderers;
    using Markdig.Renderers.Html;

    public class YamlHeaderExtension : IMarkdownExtension
    {
        private readonly MarkdownContext _context;

        public bool AllowInMiddleOfDocument { get; init; }

        public YamlHeaderExtension(MarkdownContext context)
        {
            _context = context;
        }

        public void Setup(MarkdownPipelineBuilder pipeline)
        {
            if (!pipeline.BlockParsers.Contains<YamlFrontMatterParser>())
            {
                // Insert the YAML parser before the thematic break parser, as it is also triggered on a --- dash
                pipeline.BlockParsers.InsertBefore<ThematicBreakParser>(new YamlFrontMatterParser { AllowInMiddleOfDocument = AllowInMiddleOfDocument });
            }
        }

        public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
        {
            if (!renderer.ObjectRenderers.Contains<YamlHeaderRenderer>())
            {
                renderer.ObjectRenderers.InsertBefore<CodeBlockRenderer>(new YamlHeaderRenderer(_context));
            }
        }
    }
}
