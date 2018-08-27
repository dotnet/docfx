// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using Markdig;
    using Markdig.Extensions.CustomContainers;
    using Markdig.Renderers;
    using Markdig.Renderers.Html;
    using Markdig.Syntax;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class TripleColonExtension : IMarkdownExtension
    {
        private readonly MarkdownContext _context;
        private readonly IDictionary<string, ITripleColonExtensionInfo> _extensions;

        public TripleColonExtension(MarkdownContext context)
        {
            _context = context;
            _extensions = (new ITripleColonExtensionInfo[]
            {
                new ZoneExtension(),
                new ChromelessFormExtension()
                // todo: moniker range, row, etc...
            }).ToDictionary(x => x.Name);
        }

        public void Setup(MarkdownPipelineBuilder pipeline)
        {
            var parser = new TripleColonParser(_context, _extensions);
            if (pipeline.BlockParsers.Contains<CustomContainerParser>())
            {
                pipeline.BlockParsers.InsertBefore<CustomContainerParser>(parser);
            }
            else
            {
                pipeline.BlockParsers.AddIfNotAlready(parser);
            }
        }

        public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
        {
            var htmlRenderer = renderer as HtmlRenderer;
            if (htmlRenderer != null && !htmlRenderer.ObjectRenderers.Contains<TripleColonRenderer>())
            {
                htmlRenderer.ObjectRenderers.Insert(0, new TripleColonRenderer());
            }
        }
    }

    public interface ITripleColonExtensionInfo
    {
        string Name { get; }
        bool SelfClosing { get; }
        bool TryProcessAttributes(IDictionary<string, string> attributes, out HtmlAttributes htmlAttributes, out IDictionary<string, string> renderProperties, Action<string> logError);
        bool TryValidateAncestry(ContainerBlock container, Action<string> logError);
        bool Render(HtmlRenderer renderer, TripleColonBlock block);
    }
}
