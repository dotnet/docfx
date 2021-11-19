// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Linq;
using Markdig;
using Markdig.Extensions.CustomContainers;
using Markdig.Parsers;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;

namespace Microsoft.Docs.MarkdigExtensions
{
    public class TripleColonExtension : IMarkdownExtension
    {
        private readonly MarkdownContext _context;
        private readonly IDictionary<string, ITripleColonExtensionInfo> _extensionsBlock;
        private readonly IDictionary<string, ITripleColonExtensionInfo> _extensionsInline;

        public TripleColonExtension(MarkdownContext context)
        {
            _context = context;
            _extensionsBlock = (new ITripleColonExtensionInfo[]
            {
                new ZoneExtension(),
                new ChromelessFormExtension(),
                new ImageExtension(context),
                new CodeExtension(context),
                new VideoExtension(),

                // todo: moniker range, row, etc...
            }).ToDictionary(x => x.Name);

            _extensionsInline = (new ITripleColonExtensionInfo[]
            {
                new ImageExtension(context),
                new VideoExtension(),
            }).ToDictionary(x => x.Name);
        }

        public TripleColonExtension(MarkdownContext context, ITripleColonExtensionInfo extension)
        {
            _context = context;
            _extensionsBlock = new Dictionary<string, ITripleColonExtensionInfo>();
            _extensionsInline = new Dictionary<string, ITripleColonExtensionInfo>();

            if (extension.IsBlock)
            {
                _extensionsBlock[extension.Name] = extension;
            }

            if (extension.IsInline)
            {
                _extensionsInline[extension.Name] = extension;
            }
        }

        public void Setup(MarkdownPipelineBuilder pipeline)
        {
            var parser = new TripleColonBlockParser(_context, _extensionsBlock);
            if (pipeline.BlockParsers.Contains<CustomContainerParser>())
            {
                pipeline.BlockParsers.InsertBefore<CustomContainerParser>(parser);
            }
            else
            {
                pipeline.BlockParsers.AddIfNotAlready(parser);
            }

            var inlineParser = new TripleColonInlineParser(_context, _extensionsInline);
            pipeline.InlineParsers.InsertBefore<InlineParser>(inlineParser);
        }

        public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
        {
            if (renderer is HtmlRenderer htmlRenderer && !htmlRenderer.ObjectRenderers.Contains<TripleColonBlockRenderer>())
            {
                htmlRenderer.ObjectRenderers.Insert(0, new TripleColonInlineRenderer(_context));
                htmlRenderer.ObjectRenderers.Insert(0, new TripleColonBlockRenderer(_context));
            }
        }
    }

    public interface ITripleColonExtensionInfo
    {
        string Name { get; }

        bool IsInline { get; }

        bool IsBlock { get; }

        bool SelfClosing { get; }

        bool TryProcessAttributes(IDictionary<string, string> attributes, out HtmlAttributes htmlAttributes, out IDictionary<string, string> renderProperties, Action<string> logError, Action<string> logWarning, MarkdownObject markdownObject);

        bool TryValidateAncestry(ContainerBlock container, Action<string> logError);

        bool Render(HtmlRenderer renderer, MarkdownObject markdownObject, Action<string> logWarning);
    }
}
