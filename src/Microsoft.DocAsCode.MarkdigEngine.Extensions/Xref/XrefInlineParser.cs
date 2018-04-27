// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
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

    public class XrefInlineParser : InlineParser
    {
        private const string StartString = "<xref:";

        public XrefInlineParser()
        {
            OpeningCharacters = new[] { '<' };
        }

        public override bool Match(InlineProcessor processor, ref StringSlice slice)
        {
            var saved = slice;
            if (!ExtensionsHelper.MatchStart(ref slice, StartString, false))
            {
                return false;
            }

            var href = StringBuilderCache.Local();
            var c = slice.CurrentChar;
            var startChar = '\0';
            int line;
            int column;

            if (c == '\'' || c == '"')
            {
                startChar = c;
                c = slice.NextChar();
            }

            while(c != startChar && c != '>')
            {
                href.Append(c);
                c = slice.NextChar();
            }

            if(startChar != '\0')
            {
                if(c != startChar)
                {
                    return false;
                }

                c = slice.NextChar();
            }

            if (c != '>') return false;
            slice.NextChar();

            var xrefInline = new XrefInline
            {
                Href = href.ToString().Trim(),
                Span = new SourceSpan(processor.GetSourcePosition(saved.Start, out line, out column), processor.GetSourcePosition(slice.Start - 1)),
                Line = line,
                Column = column
            };

            var htmlAttributes = xrefInline.GetAttributes();
            htmlAttributes.AddPropertyIfNotExist("data-throw-if-not-resolved", "True");

            var dataRawSource = saved;
            dataRawSource.End = slice.Start - 1;
            htmlAttributes.AddPropertyIfNotExist("data-raw-source", dataRawSource.ToString());
            processor.Inline = xrefInline;

            return true;
        }
    }
}
