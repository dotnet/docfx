// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Renderers.Html;
using Markdig.Syntax;

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions;

public class TripleColonInlineParser : InlineParser
{
    private readonly MarkdownContext _context;
    private readonly IDictionary<string, ITripleColonExtensionInfo> _extensions;

    public TripleColonInlineParser(MarkdownContext context, IDictionary<string, ITripleColonExtensionInfo> extensions)
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

        if (!TripleColonBlockParser.TryMatchIdentifier(ref slice, out var extensionName)
            || !_extensions.TryGetValue(extensionName, out var extension))
        {
            return false;
        }

        var inline = new TripleColonInline(this)
        {
            Closed = false,
            Column = 0,
            Line = processor.LineIndex,
            Span = new SourceSpan(processor.LineIndex, slice.End),
        };

        var logError = new Action<string>(message => _context.LogError($"invalid-{extensionName}", message, inline));
        var logWarning = new Action<string>(message => _context.LogWarning($"invalid-{extensionName}", message, inline));

        if (!TripleColonBlockParser.TryMatchAttributes(ref slice, out var attributes, extension.SelfClosing, logError) ||
            !extension.TryProcessAttributes(attributes, out var htmlAttributes, out var renderProperties, logError, logWarning, inline))
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
