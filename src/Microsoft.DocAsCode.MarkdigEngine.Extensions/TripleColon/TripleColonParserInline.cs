// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using Markdig.Helpers;
    using Markdig.Parsers;
    using Markdig.Renderers.Html;
    using Markdig.Syntax;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    public class TripleColonParserInline : InlineParser
    {
        private static readonly IDictionary<string, string> EmptyAttributes = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>());
        private readonly MarkdownContext _context;
        private readonly IDictionary<string, ITripleColonExtensionInfo> _extensions;

        private readonly string[] StartStrings = { ":::image", ":::video", ":::code" };
        private const string EndString = "\":::";

        public TripleColonParserInline(MarkdownContext context, IDictionary<string, ITripleColonExtensionInfo> extensions)
        {
            OpeningCharacters = new[] { ':' };
            _context = context;
            _extensions = extensions;
        }

        public override bool Match(InlineProcessor processor, ref StringSlice slice)
        {
            if (!ExtensionsHelper.MatchStart(ref slice, ":::"))
            {
                return false;
            }

            var extensionName = "triple-colon";
            var sourcePosition = processor.LineIndex;
            Action<string> logError = (string message) => _context.LogError(
                $"invalid-{extensionName}",
                $"{message}",
                null,
                line: processor.LineIndex);
            Action<string> logWarning = (string message) => _context.LogWarning(
                $"invalid-{extensionName}",
                $"{message}",
                null,
                line: processor.LineIndex);

            var inline = new TripleColonInline(this)
            {
                Closed = false,
                Column = 0,
                Line = processor.LineIndex,
                Span = new SourceSpan(sourcePosition, slice.End),
            };


            if (!TripleColonParser.TryMatchIdentifier(ref slice, out extensionName)
                || !_extensions.TryGetValue(extensionName, out var extension)
                || !TripleColonParser.TryMatchAttributes(ref slice, out var attributes, extensionName, extension.SelfClosing, logError)
                || !extension.TryProcessAttributes(attributes, out var htmlAttributes, out var renderProperties, logError, logWarning, inline))
            {
                return false;
            }

            inline.Extension = extension;
            inline.Attributes = attributes;
            inline.RenderProperties = renderProperties;

            if (htmlAttributes != null)
            {
                inline.SetData(typeof(HtmlAttributes), htmlAttributes);
            }

            processor.Inline = inline;

            return true;
        }
    }
}
