// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Renderers.Html;
using Markdig.Syntax;

namespace Docfx.MarkdigEngine.Extensions;

public class TripleColonInlineParser : InlineParser
{
    private readonly MarkdownContext _context;
    private readonly IDictionary<string, ITripleColonExtensionInfo> _extensions;

    public TripleColonInlineParser(MarkdownContext context, IDictionary<string, ITripleColonExtensionInfo> extensions)
    {
        OpeningCharacters = [':'];
        _context = context;
        _extensions = extensions;
    }

    public override bool Match(InlineProcessor processor, ref StringSlice slice)
    {
        var startPosition = processor.GetSourcePosition(slice.Start, out var line, out var column);

        if (!ExtensionsHelper.MatchStart(ref slice, ":::"))
        {
            return false;
        }

        if (!TripleColonBlockParser.TryMatchIdentifier(ref slice, out var extensionName)
            || !_extensions.TryGetValue(extensionName, out var extension))
        {
            return false;
        }

        var inline = new TripleColonInline()
        {
            IsClosed = true,
            Line = line,
            Column = column,
        };

        var logError = new Action<string>(message => _context.LogError($"invalid-{extensionName}", message, inline));
        var logWarning = new Action<string>(message => _context.LogWarning($"invalid-{extensionName}", message, inline));

        if (!TripleColonBlockParser.TryMatchAttributes(ref slice, out var attributes, extension.SelfClosing, logError) ||
            !extension.TryProcessAttributes(attributes, out var htmlAttributes, logError, logWarning, inline))
        {
            return false;
        }

        inline.Extension = extension;
        inline.Attributes = attributes;
        inline.Span = new SourceSpan(startPosition, processor.GetSourcePosition(slice.Start - 1));

        if (htmlAttributes != null)
        {
            inline.SetData(typeof(HtmlAttributes), htmlAttributes);
        }

        processor.Inline = inline;

        return true;
    }
}
