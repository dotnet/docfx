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

    public class TripleColonBlockParser : BlockParser
    {
        private static readonly IDictionary<string, string> EmptyAttributes = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>());
        private readonly MarkdownContext _context;
        private readonly IDictionary<string, ITripleColonExtensionInfo> _extensions;

        public TripleColonBlockParser(MarkdownContext context, IDictionary<string, ITripleColonExtensionInfo> extensions)
        {
            OpeningCharacters = new[] { ':' };
            _context = context;
            _extensions = extensions;
        }

        public override BlockState TryOpen(BlockProcessor processor)
        {
            var slice = processor.Line;
            var sourcePosition = processor.Start;

            if (processor.IsCodeIndent
                || !ExtensionsHelper.MatchStart(ref slice, ":::"))
            {
                return BlockState.None;
            }

            ExtensionsHelper.SkipSpaces(ref slice);

            if (!TryMatchIdentifier(ref slice, out var extensionName) || !_extensions.TryGetValue(extensionName, out var extension))
            {
                return BlockState.None;
            }

            var block = new TripleColonBlock(this)
            {
                Closed = false,
                Column = processor.Column,
                Line = processor.LineIndex,
                Span = new SourceSpan(sourcePosition, slice.End),
            };

            var logError = new Action<string>(message => _context.LogError($"invalid-{extensionName}", message, block));
            var logWarning = new Action<string>(message => _context.LogWarning($"invalid-{extensionName}", message, block));

            if (!extension.TryValidateAncestry(processor.CurrentContainer, logError) ||
                !TryMatchAttributes(ref slice, out var attributes, extension.SelfClosing, logError) ||
                !extension.TryProcessAttributes(attributes, out var htmlAttributes, out var renderProperties, logError, logWarning, block))
            {
                return BlockState.None;
            }

            block.Extension = extension;
            block.Attributes = attributes;
            block.RenderProperties = renderProperties;

            if (htmlAttributes != null)
            {
                block.SetData(typeof(HtmlAttributes), htmlAttributes);
            }

            if (extension.GetType() == typeof(ImageExtension))
            {
                if(htmlAttributes != null
                && !ImageExtension.RequiresClosingTripleColon(attributes))
                {
                    return BlockState.None;
                }

                processor.NewBlocks.Push(block);
                block.EndingTripleColons = true;
                return BlockState.ContinueDiscard;
            } else
            {
                processor.NewBlocks.Push(block);
            }

            if (extension.SelfClosing)
            {
                return BlockState.BreakDiscard;
            }

            return BlockState.ContinueDiscard;
        }

        public override BlockState TryContinue(BlockProcessor processor, Block block)
        {
            var slice = processor.Line;
            var endingTripleColons = ((TripleColonBlock)block).EndingTripleColons;

            if(((TripleColonBlock)block).Extension.GetType() != typeof(ImageExtension)
                || endingTripleColons)
            {
                if (processor.IsBlankLine)
                {
                    return BlockState.Continue;
                }

                ExtensionsHelper.SkipSpaces(ref slice);

                if (!ExtensionsHelper.MatchStart(ref slice, ":::"))
                {
                    // create a block for the image long description
                    ((TripleColonBlock)block).Body = slice.ToString();
                    ExtensionsHelper.ResetLineIndent(processor);
                    return BlockState.Continue;
                }

                ExtensionsHelper.SkipSpaces(ref slice);

                var extensionName = ((TripleColonBlock)block).Extension.Name;

                if (!ExtensionsHelper.MatchStart(ref slice, extensionName) || !ExtensionsHelper.MatchStart(ref slice, "-end"))
                {
                    ExtensionsHelper.ResetLineIndent(processor);
                    return BlockState.Continue;
                }

                var c = ExtensionsHelper.SkipSpaces(ref slice);

                if (endingTripleColons && !ExtensionsHelper.MatchStart(ref slice, ":::"))
                {
                    _context.LogWarning(
                        $"invalid-{extensionName}",
                        $"Invalid {extensionName} on line {block.Line}. \"{slice.Text}\" is invalid. Missing ending \":::{extensionName}-end:::\"",
                        block);
                    return BlockState.Continue;
                }

                if (!c.IsZero() && !endingTripleColons)
                {
                    _context.LogWarning(
                        $"invalid-{extensionName}",
                        $"Invalid {extensionName} on line {block.Line}. \"{slice.Text}\" is invalid. Invalid character after \"::: {extensionName}-end\": \"{c}\"",
                        block);
                }

                block.UpdateSpanEnd(slice.End);
                block.IsOpen = false;
                (block as TripleColonBlock).Closed = true;

                return BlockState.BreakDiscard;
            }

            block.IsOpen = false;
            (block as TripleColonBlock).Closed = true;

            if (!processor.IsBlankLine)
            {
                return BlockState.Continue;
            }

            return BlockState.BreakDiscard;
        }

        public override bool Close(BlockProcessor processor, Block block)
        {
            var tripleColonBlock = (TripleColonBlock)block;
             if (tripleColonBlock.Extension.SelfClosing)
            {
                block.IsOpen = false;
                return true;
            }

            var extensionName = tripleColonBlock.Extension.Name;
            if (block.IsOpen)
            {
                _context.LogWarning(
                    $"invalid-{extensionName}",
                    $"Invalid {extensionName} on line {block.Line}. No \"::: {extensionName}-end\" found. Blocks should be explicitly closed.",
                    block);
            }
            return true;
        }

        public static bool TryMatchIdentifier(ref StringSlice slice, out string name)
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

        public static bool TryMatchAttributeValue(ref StringSlice slice, out string value, string attributeName, Action<string> logError)
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

        public static bool TryMatchAttributes(ref StringSlice slice, out IDictionary<string, string> attributes, bool selfClosing, Action<string> logError)
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
                    if (!TryMatchAttributeValue(ref slice, out value, attributeName, logError))
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
