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
    using System.Linq;
    using System.Text;

    public class ImageParser : BlockParser
    {
        private string ExtensionName = "image";
        private const string EndString = "image-end:::";
        private const char Colon = ':';
        private static readonly IDictionary<string, string> EmptyAttributes = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>());

        private readonly MarkdownContext _context;

        public ImageParser(MarkdownContext context)
        {
            OpeningCharacters = new[] { ':' };
            _context = context;
        }

        public override BlockState TryOpen(BlockProcessor processor)
        {
            if (processor.IsBlankLine)
            {
                return BlockState.Continue;
            }
            
            IDictionary<string, string> attributes;
            IDictionary<string, string> renderProperties;
            HtmlAttributes htmlAttributes;
            var slice = processor.Line;
            var column = processor.Column;
            var sourcePosition = processor.Start;
            var colonCount = 0;
            Action<string> logWarning = (string message) => _context.LogWarning(
            $"invalid-{ExtensionName}",
            $"Invalid {ExtensionName} on line {processor.LineIndex}. \"{slice.Text}\" is invalid. {message}",
            null,
            line: processor.LineIndex);

            ExtensionsHelper.SkipSpaces(ref slice);

            var c = slice.CurrentChar;

            while (c == Colon)
            {
                c = slice.NextChar();
                colonCount++;
            }

            if (colonCount < 3) return BlockState.None;

            ExtensionsHelper.SkipSpaces(ref slice);

            if (!ExtensionsHelper.MatchStart(ref slice, ExtensionName, false))
            {
                return BlockState.None;
            }

            if (!TryMatchAttributes(ref slice, out attributes, ExtensionName, logWarning)
                || !TryProcessAttributes(attributes, out htmlAttributes, out renderProperties, logWarning))
            {
                return BlockState.None;
            }

            var id = htmlAttributes.Properties.FirstOrDefault(x => x.Key == "aria-describedby").Value;
            var src = htmlAttributes.Properties.FirstOrDefault(x => x.Key == "src").Value;
            var alt = htmlAttributes.Properties.FirstOrDefault(x => x.Key == "alt").Value;
            
            processor.NewBlocks.Push(new ImageBlock(this)
            {
                Line = processor.LineIndex,
                Src = src,
                Alt = alt,
                Id = id,
                ColonCount = colonCount,
                Column = column,
                Span = new SourceSpan(sourcePosition, slice.End),
            });

            return BlockState.ContinueDiscard;
        }

        public override BlockState TryContinue(BlockProcessor processor, Block block)
        {
            if (processor.IsBlankLine)
            {
                return BlockState.Continue;
            }

            var slice = processor.Line;
            var ImageBlock = (ImageBlock)block;

            ExtensionsHelper.SkipSpaces(ref slice);

            if (!ExtensionsHelper.MatchStart(ref slice, new string(':', ImageBlock.ColonCount)))
            {
                return BlockState.Continue;
            }

            ExtensionsHelper.SkipSpaces(ref slice);

            if (!ExtensionsHelper.MatchStart(ref slice, EndString, false))
            {
                return BlockState.Continue;
            }

            var c = ExtensionsHelper.SkipSpaces(ref slice);

            if (!c.IsZero())
            {
                _context.LogWarning(
                    $"invalid-{ExtensionName}",
                    $"Invalid {ExtensionName} on line {block.Line}. \"{slice.Text}\" is invalid. Invalid character after \"::: {ExtensionName}-end\": \"{c}\"",
                    block);
            }

            block.UpdateSpanEnd(slice.End);
            block.IsOpen = false;
            (block as ImageBlock).Closed = true;

            return BlockState.BreakDiscard;
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

        private bool TryMatchAttributes(ref StringSlice slice, out IDictionary<string, string> attributes, string extensionName, Action<string> logError)
        {
            attributes = EmptyAttributes;
            while (true)
            {
                ExtensionsHelper.SkipSpaces(ref slice);
                if (ExtensionsHelper.MatchStart(ref slice, ":::"))
                {
                    return true;
                }
                string attributeName;
                if (!TryMatchIdentifier(ref slice, out attributeName))
                {
                    logError($"Invalid attribute.");
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

        public bool TryProcessAttributes(IDictionary<string, string> attributes, out HtmlAttributes htmlAttributes, out IDictionary<string, string> renderProperties, Action<string> logWarning)
        {
            htmlAttributes = null;
            renderProperties = new Dictionary<string, string>();
            var src = string.Empty;
            var alt = string.Empty;
            var id = string.Empty;
            foreach (var attribute in attributes)
            {
                var name = attribute.Key;
                var value = attribute.Value;
                switch (name)
                {
                    case "alt-text":
                        alt = value;
                        break;
                    case "source":
                        src = value;
                        break;
                    case "id":
                        id = value;
                        break;
                    default:
                        logWarning($"Unexpected attribute \"{name}\".");
                        return false;
                }
            }

            if (alt == string.Empty)
            {
                logWarning($"alt-text must be specified.");
                return false;
            }
            if (src == string.Empty)
            {
                logWarning($"source must be specified.");
                return false;
            }
            var Id = id;
            if (string.IsNullOrEmpty(id))
            {
                Id = src;
                if (Id.IndexOf('/') > -1)
                {
                    Id = Id.Split('/').Last();
                }
                if (Id.IndexOf('.') > -1)
                {
                    Id = Id.Split('.').First();
                }
                //If the user does not have the id attribute set,
                //then use src filename with randomly generated id to be unique.
                Random random = new Random();
                const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
                var generated = new string(Enumerable.Repeat(chars, 8)
                    .Select(s => s[random.Next(s.Length)]).ToArray());
                Id = $"{Id}-{generated}";
            }

            htmlAttributes = new HtmlAttributes();
            htmlAttributes.AddProperty("aria-describedby", Id);
            htmlAttributes.AddProperty("src", src);
            htmlAttributes.AddProperty("alt", alt);

            return true;
        }
    }
}
