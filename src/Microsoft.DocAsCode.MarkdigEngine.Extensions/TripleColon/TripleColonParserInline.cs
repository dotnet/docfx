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
        private readonly IDictionary<string, ITripleColonExtensionInlineInfo> _extensions;

        private readonly string[] StartStrings = { ":::image", ":::video", ":::code" };
        private const string EndString = "\":::";

        public TripleColonParserInline(MarkdownContext context, IDictionary<string, ITripleColonExtensionInlineInfo> extensions)
        {
            OpeningCharacters = new[] { ':' };
            _context = context;
            _extensions = extensions;
        }

        public override bool Match(InlineProcessor processor, ref StringSlice slice)
        {
            //if (!ExtensionsHelper.MatchStartMultiple(ref slice, StartStrings, true))
            //{
            //    return false;
            //}

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


            if (!TryMatchIdentifier(ref slice, out extensionName)
                || !_extensions.TryGetValue(extensionName, out var extension)
                || !TryMatchAttributes(ref slice, out var attributes, extensionName, extension.SelfClosing, logError)
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

        
        private bool TryMatchIdentifier(ref StringSlice slice, out string name)
        {
            name = string.Empty;
            var c = slice.CurrentChar;
            if (c.IsAlpha())
            {
                var b = StringBuilderCache.Local();
                do
                {
                    b.Append(c);
                    c = slice.NextChar();
                } while (c.IsAlphaNumeric() || c == '-');
                name = b.ToString().ToLower();
                return true;
            }
            return false;
        }

        private bool TryMatchAttributeValue(ref StringSlice slice, out string value, string extensionName, string attributeName, Action<string> logError)
        {
            value = string.Empty;
            var c = slice.CurrentChar;
            if (c != '"')
            {
                logError($"Invalid attribute \"{attributeName}\". Values must be enclosed in double quotes.");
                return false;
            }
            var b = StringBuilderCache.Local();
            c = slice.NextChar();
            while (c != '"')
            {
                if (c.IsZero())
                {
                    logError($"Invalid attribute \"{attributeName}\". Values must be terminated with a double quote.");
                    return false;
                }
                b.Append(c);
                c = slice.NextChar();
            }
            slice.NextChar();
            value = b.ToString();
            return true;
        }

        private bool TryMatchAttributes(ref StringSlice slice, out IDictionary<string, string> attributes, string extensionName, bool selfClosing, Action<string> logError)
        {
            attributes = EmptyAttributes;

            while (true)
            {
                ExtensionsHelper.SkipSpaces(ref slice);
                if (slice.CurrentChar.IsZero() || (selfClosing && ExtensionsHelper.MatchStart(ref slice, ":::")))
                {
                    return true;
                }

                if (!TryMatchIdentifier(ref slice, out var attributeName))
                {
                    logError("Invalid attribute.");
                    return false;
                }
                if (attributes.ContainsKey(attributeName))
                {
                    logError($"Attribute \"{attributeName}\" specified multiple times.");
                    return false;
                }

                var value = string.Empty;

                ExtensionsHelper.SkipSpaces(ref slice);
                if (slice.CurrentChar == '=')
                {
                    slice.NextChar();
                    ExtensionsHelper.SkipSpaces(ref slice);
                    if (!TryMatchAttributeValue(ref slice, out value, extensionName, attributeName, logError))
                    {
                        return false;
                    }
                }

                if (attributes == EmptyAttributes)
                {
                    attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                }
                attributes.Add(attributeName, value);
            }
        }
    }
}
