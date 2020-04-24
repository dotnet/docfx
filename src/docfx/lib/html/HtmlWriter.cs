// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;

namespace Microsoft.Docs.Build
{
    internal class HtmlWriter
    {
        private readonly IBufferWriter<char> _writer;

        public HtmlWriter(IBufferWriter<char> writer)
        {
            _writer = writer;
        }

        public void Write(in HtmlToken token)
        {
            if (token.Type == HtmlTokenType.StartTag)
            {
                // Detect if attributes have changed
                var raw = true;
                foreach (ref readonly var attribute in token.Attributes.Span)
                {
                    if (attribute.RawText.Length == 0)
                    {
                        raw = false;
                    }
                }

                if (raw)
                {
                    _writer.Write(token.RawText.Span);
                }
                else
                {
                    WriteStartTag(token.Name.Span, token.Attributes.Span, token.IsSelfClosing);
                }
            }
            else if (token.RawText.Length > 0)
            {
                _writer.Write(token.RawText.Span);
            }
        }

        public void Write(ReadOnlySpan<char> rawText)
        {
            _writer.Write(rawText);
        }

        public void WriteStartTag(ReadOnlySpan<char> name, ReadOnlySpan<HtmlAttribute> attributes, bool isSelfClosing)
        {
            // Estimate length
            var sizeHint = 1 + name.Length + 2;
            foreach (ref readonly var attribute in attributes)
            {
                if (attribute.RawText.Length > 0)
                {
                    sizeHint += attribute.RawText.Length;
                }
                else
                {
                    sizeHint += attribute.Name.Length + attribute.Value.Length + 3;
                }
            }

            // Write
            var pos = 0;
            var span = _writer.GetSpan(sizeHint);
            span[pos++] = '<';
            name.CopyTo(span.Slice(pos));
            pos += name.Length;

            foreach (ref readonly var attribute in attributes)
            {
                if (attribute.Name.Length == 0 && attribute.Value.Length == 0)
                {
                    continue;
                }

                span[pos++] = ' ';

                if (attribute.RawText.Length > 0)
                {
                    attribute.RawText.Span.CopyTo(span.Slice(pos));
                    pos += attribute.RawText.Length;
                    continue;
                }

                attribute.Name.Span.CopyTo(span.Slice(pos));
                pos += attribute.Name.Length;

                switch (attribute.Type)
                {
                    case HtmlAttributeType.NameOnly:
                        break;

                    case HtmlAttributeType.Unquoted:
                        span[pos++] = '=';
                        attribute.Value.Span.CopyTo(span.Slice(pos));
                        pos += attribute.Name.Length;
                        break;

                    case HtmlAttributeType.SingleQuoted:
                        span[pos++] = '=';
                        span[pos++] = '\'';
                        attribute.Value.Span.CopyTo(span.Slice(pos));
                        pos += attribute.Value.Length;
                        span[pos++] = '\'';
                        break;

                    case HtmlAttributeType.DoubleQuoted:
                        span[pos++] = '=';
                        span[pos++] = '"';
                        attribute.Value.Span.CopyTo(span.Slice(pos));
                        pos += attribute.Value.Length;
                        span[pos++] = '"';
                        break;
                }
            }

            if (isSelfClosing)
            {
                span[pos++] = '/';
            }
            span[pos++] = '>';

            _writer.Advance(pos);
        }
    }
}
