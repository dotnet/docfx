// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;

namespace Microsoft.Docs.Build
{
    /// <summary>
    /// Provides a high-performance, near-zero-allocation, fault-tolerant, standard-compliant API
    /// for forward-only, read-only access to HTML text.
    /// </summary>
    internal ref struct HtmlReader
    {
        private string _html;
        private int _length;
        private int _position;

        private HtmlTokenType _type;
        private (int start, int length) _nameRange;
        private (int start, int length) _tokenRange;

        private (int start, int length) _attributeNameRange;
        private (int start, int length) _attributeValueRange;
        private (int start, int length) _attributeRange;
        private HtmlAttribute[]? _attributes;
        private int _attributesLength;

        public HtmlTokenType Type => _type;

        public (int start, int length) NameRange => _nameRange;

        public (int start, int length) ContentRange => _tokenRange;

        public ReadOnlySpan<char> Name => _html.AsSpan().Slice(_nameRange.start, _nameRange.length);

        public ReadOnlySpan<char> Token => _html.AsSpan().Slice(_tokenRange.start, _tokenRange.length);

        public ReadOnlySpan<HtmlAttribute> Attributes => _attributes.AsSpan().Slice(0, _attributesLength);

        public HtmlReader(string html)
        {
            _html = html;
            _length = html.Length;
            _position = default;
            _type = HtmlTokenType.Text;
            _nameRange = default;
            _tokenRange = default;
            _attributeNameRange = default;
            _attributeValueRange = default;
            _attributeRange = default;
            _attributes = default;
            _attributesLength = default;
        }

        public bool Read()
        {
            // Simplified pasing algorithm based on https://html.spec.whatwg.org/multipage/parsing.html
            _attributesLength = 0;
            if (_attributes != null)
            {
                ArrayPool<HtmlAttribute>.Shared.Return(_attributes);
                _attributes = null;
            }

            if (_position >= _length)
            {
                return false;
            }

            _tokenRange.start = _position;
            _nameRange = default;

            switch (Consume())
            {
                case '<':
                    _type = HtmlTokenType.StartTag;
                    TagOpen();
                    break;

                default:
                    _type = HtmlTokenType.Text;
                    ConsumeUntilNextIs('<');
                    break;
            }

            _tokenRange.length = _position - _tokenRange.start;
            return true;
        }

        private void TagOpen()
        {
            switch (Consume())
            {
                case '!':
                case '?':
                    _type = HtmlTokenType.Comment;
                    ConsumeUntil('>');
                    break;

                case '/':
                    _type = HtmlTokenType.EndTag;
                    EndTagOpen();
                    break;

                case char c when IsASCIIAlpha(c):
                    Back();
                    TagName(readAttributes: true);
                    break;

                default:
                    _type = HtmlTokenType.Text;
                    ConsumeUntilNextIs('<');
                    break;
            }
        }

        private void EndTagOpen()
        {
            switch (Consume())
            {
                case '>':
                    _nameRange.length = 0;
                    break;

                case '\0':
                    _type = HtmlTokenType.Text;
                    break;

                case char c when IsASCIIAlpha(c):
                    Back();
                    TagName(readAttributes: false);
                    break;

                default:
                    _type = HtmlTokenType.Comment;
                    ConsumeUntil('>');
                    break;
            }
        }

        private void TagName(bool readAttributes)
        {
            _nameRange.start = _position;

            while (true)
            {
                switch (Consume())
                {
                    case '\0':
                        _type = HtmlTokenType.Comment;
                        return;

                    case '>':
                        if (_nameRange.length == 0)
                        {
                            _nameRange.length = _position - 1 - _nameRange.start;
                        }
                        return;

                    case '/':
                        if (_nameRange.length == 0)
                        {
                            _nameRange.length = _position - 1 - _nameRange.start;
                        }
                        SelfClosingStartTag();
                        return;

                    case char c when IsWhiteSpace(c):
                        if (_nameRange.length == 0)
                        {
                            _nameRange.length = _position - 1 - _nameRange.start;
                        }
                        if (readAttributes)
                        {
                            BeforeAttributeName();
                            return;
                        }
                        break;
                }
            }
        }

        private void SelfClosingStartTag()
        {
            switch (Consume())
            {
                case '>':
                    break;

                case '\0':
                    _type = HtmlTokenType.Comment;
                    break;

                default:
                    Back();
                    BeforeAttributeName();
                    break;
            }
        }

        private void BeforeAttributeName()
        {
            _attributeNameRange = default;
            _attributeRange = default;
            _attributeValueRange = default;

            while (true)
            {
                switch (Consume())
                {
                    case '\0':
                    case '/':
                    case '>':
                        Back();
                        AfterAttributeName();
                        return;

                    case char c when IsWhiteSpace(c):
                        break;

                    case '=':
                        _attributeNameRange.start = _attributeRange.start = _position - 1;
                        AttributeName();
                        return;

                    default:
                        Back();

                        _attributeNameRange.start = _attributeRange.start = _position;
                        AttributeName();
                        return;
                }
            }
        }

        private void AttributeName()
        {
            while (true)
            {
                switch (Consume())
                {
                    case '\0':
                    case '/':
                    case '>':
                    case char ch when IsWhiteSpace(ch):
                        Back();
                        _attributeNameRange.length = _position - _attributeNameRange.start;
                        AfterAttributeName();
                        return;

                    case '=':
                        _attributeNameRange.length = _position - 1 - _attributeNameRange.start;
                        BeforeAttributeValue();
                        return;
                }
            }
        }

        private void AfterAttributeName()
        {
            while (true)
            {
                switch (Consume())
                {
                    case '\0':
                        _type = HtmlTokenType.Comment;
                        return;

                    case '>':
                        _attributeRange.length = _position - 1 - _attributeRange.start;
                        AddAttribute();
                        return;

                    case '/':
                        _attributeRange.length = _position - 1 - _attributeRange.start;
                        AddAttribute();
                        SelfClosingStartTag();
                        return;

                    case '=':
                        BeforeAttributeValue();
                        return;

                    case char c when IsWhiteSpace(c):
                        break;

                    default:
                        AddAttribute();
                        Back();
                        _attributeNameRange.start = _attributeRange.start = _position;
                        AttributeName();
                        return;
                }
            }
        }

        private void BeforeAttributeValue()
        {
            while (true)
            {
                switch (Consume())
                {
                    case '\'':
                        AttributeValue('\'');
                        return;

                    case '"':
                        AttributeValue('"');
                        return;

                    case '>':
                        _attributeValueRange.length = 0;
                        _attributeRange.length = _position - 1 - _attributeRange.start;
                        AddAttribute();
                        return;

                    case char ch when IsWhiteSpace(ch):
                        break;

                    default:
                        Back();
                        AttributeValueUnquoted();
                        return;
                }
            }
        }

        private void AttributeValue(char quote)
        {
            _attributeValueRange.start = _position;

            while (true)
            {
                switch (Consume())
                {
                    case '\0':
                        _type = HtmlTokenType.Comment;
                        return;

                    case char c when c == quote:
                        _attributeValueRange.length = _position - 1 - _attributeValueRange.start;
                        _attributeRange.length = _position - _attributeRange.start;
                        AddAttribute();
                        BeforeAttributeName();
                        return;
                }
            }
        }

        private void AttributeValueUnquoted()
        {
            _attributeValueRange.start = _position;

            while (true)
            {
                switch (Consume())
                {
                    case '>':
                        _attributeValueRange.length = _position - 1 - _attributeValueRange.start;
                        _attributeRange.length = _position - 1 - _attributeRange.start;
                        AddAttribute();
                        return;

                    case '\0':
                        _type = HtmlTokenType.Comment;
                        return;

                    case char ch when IsWhiteSpace(ch):
                        _attributeValueRange.length = _position - 1 - _attributeValueRange.start;
                        _attributeRange.length = _position - 1 - _attributeRange.start;
                        AddAttribute();
                        BeforeAttributeName();
                        return;
                }
            }
        }

        private void AddAttribute()
        {
            if (_attributeNameRange.length <= 0)
            {
                return;
            }

            _attributes ??= ArrayPool<HtmlAttribute>.Shared.Rent(4);

            // Grow array as needed
            if (_attributesLength == _attributes.Length)
            {
                var newAttributes = ArrayPool<HtmlAttribute>.Shared.Rent(_attributes.Length * 2);
                Array.Copy(_attributes, 0, newAttributes, 0, _attributes.Length);
                ArrayPool<HtmlAttribute>.Shared.Return(_attributes);
                _attributes = newAttributes;
            }

            _attributes[_attributesLength++] = new HtmlAttribute(_html, _attributeNameRange, _attributeValueRange, _attributeRange);
        }

        private char Consume()
        {
            return _position < _length ? _html[_position++] : '\0';
        }

        private void ConsumeUntil(char c)
        {
            while (_position < _length)
            {
                if (_html[_position++] == c)
                {
                    break;
                }
            }
        }

        private void ConsumeUntilNextIs(char c)
        {
            while (_position < _length)
            {
                if (_html[_position] == c)
                {
                    break;
                }
                _position++;
            }
        }

        private void Back()
        {
            _position--;
        }

        private static bool IsASCIIAlpha(char c)
        {
            return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
        }

        private static bool IsWhiteSpace(char c)
        {
            return c == ' ' || c == '\t' || c == '\r' || c == '\n' || c == '\f';
        }
    }
}
