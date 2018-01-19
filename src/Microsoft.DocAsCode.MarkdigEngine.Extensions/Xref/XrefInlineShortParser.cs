// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MarkdigEngine.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using Markdig.Helpers;
    using Markdig.Parsers;
    using Markdig.Renderers.Html;
    using Markdig.Syntax;

    class XrefInlineShortParser : InlineParser
    {
        private const string Punctuation = ".,;:!?`~";

        public XrefInlineShortParser()
        {
            OpeningCharacters = new[] { '@' };
        }

        public override bool Match(InlineProcessor processor, ref StringSlice slice)
        {
            var c = slice.PeekCharExtra(-1);

            if (c == '\\')
            {
                return false;
            }

            var saved = slice;
            var startChar = '\0';
            int line;
            int column;

            c = slice.NextChar();

            if (c == '\'' || c == '"')
            {
                startChar = c;
                c = slice.NextChar();
            }
            else
            {
                return false;
            }

            if (!c.IsAlpha())
            {
                return false;
            }

            var href = StringBuilderCache.Local();

            while (c != startChar && c != '\0' && c != '\n')
            {
                href.Append(c);
                c = slice.NextChar();
            }

            if (c != startChar)
            {
                return false;
            }

            slice.NextChar();


            var xrefInline = new XrefInline
            {
                Href = href.ToString().Trim(),
                Span = new SourceSpan(processor.GetSourcePosition(saved.Start, out line, out column), processor.GetSourcePosition(slice.Start - 1)),
                Line = line,
                Column = column
            };

            var htmlAttributes = xrefInline.GetAttributes();

            var sourceContent = href.Insert(0, startChar).Insert(0, '@').Append(startChar);
            htmlAttributes.AddPropertyIfNotExist("data-throw-if-not-resolved", "False");
            htmlAttributes.AddPropertyIfNotExist("data-raw-source", sourceContent.ToString());
            processor.Inline = xrefInline;
            return true;
        }
    }
}
