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

        public (int start, int length) TokenRange => _tokenRange;

        public ReadOnlySpan<char> Name => _html.AsSpan(_nameRange.start, _nameRange.length);

        public ReadOnlySpan<char> Token => _html.AsSpan(_tokenRange.start, _tokenRange.length);

        public ReadOnlySpan<HtmlAttribute> Attributes => _attributes.AsSpan(0, _attributesLength);

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

        public bool NameIs(string name)
        {
            return _html.AsSpan(_nameRange.start, _nameRange.length).Equals(name, StringComparison.OrdinalIgnoreCase);
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
                    MarkdownDeclarationOpen();
                    break;

                case '?':
                    Back();
                    BogusComment();
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

        private void MarkdownDeclarationOpen()
        {
            switch (Consume())
            {
                case '-' when Peek() == '-':
                    Consume();
                    CommentStart();
                    break;

                default:
                    BogusComment();
                    break;
            }
        }

        private void CommentStart()
        {
            _type = HtmlTokenType.Comment;
            switch (Consume())
            {
                case '-':
                    CommentStartDash();
                    break;

                case '>':
                    break;

                default:
                    Back();
                    Comment();
                    break;
            }
        }

        private void CommentStartDash()
        {
            switch (Consume())
            {
                case '-':
                    CommentEnd();
                    break;

                case '\0':
                case '>':
                    break;

                default:
                    Back();
                    Comment();
                    break;
            }
        }

        private void Comment()
        {
            while (true)
            {
                switch (Consume())
                {
                    case '<':
                        CommentLessThanSign();
                        return;

                    case '-':
                        CommendEndDash();
                        return;

                    case '\0':
                        return;
                }
            }
        }

        private void CommentLessThanSign()
        {
            while (true)
            {
                switch (Consume())
                {
                    case '!':
                        CommentLessThanSignBang();
                        return;

                    case '<':
                        break;

                    default:
                        Back();
                        Comment();
                        return;
                }
            }
        }

        private void CommentLessThanSignBang()
        {
            switch (Consume())
            {
                case '-':
                    CommentLessThanSignBangDash();
                    return;

                default:
                    Back();
                    Comment();
                    return;
            }
        }

        private void CommentLessThanSignBangDash()
        {
            switch (Consume())
            {
                case '-':
                    Back();
                    CommentEnd();
                    return;

                default:
                    Back();
                    Comment();
                    return;
            }
        }

        private void CommendEndDash()
        {
            switch (Consume())
            {
                case '-':
                    CommentEnd();
                    return;

                case '\0':
                    return;

                default:
                    Back();
                    Comment();
                    break;
            }
        }

        private void CommentEnd()
        {
            while (true)
            {
                switch (Consume())
                {
                    case '\0':
                    case '>':
                        return;

                    case '!':
                        CommentEndBang();
                        return;

                    case '-':
                        break;

                    default:
                        Back();
                        Comment();
                        return;
                }
            }
        }

        private void CommentEndBang()
        {
            switch (Consume())
            {
                case '-':
                    CommentEnd();
                    return;

                case '>':
                case '\0':
                    return;

                default:
                    Back();
                    Comment();
                    break;
            }
        }

        private void BogusComment()
        {
            _type = HtmlTokenType.Comment;
            while (true)
            {
                switch (Consume())
                {
                    case '>':
                        return;
                }
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
                    BogusComment();
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

                    case '\t':
                    case '\r':
                    case '\n':
                    case '\f':
                    case ' ':
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

                    case '\t':
                    case '\r':
                    case '\n':
                    case '\f':
                    case ' ':
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
                    case '\t':
                    case '\r':
                    case '\n':
                    case '\f':
                    case ' ':
                    case '/':
                    case '>':
                    case '\0':
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
                        if (_attributeRange.length == 0)
                        {
                            _attributeRange.length = _position - 1 - _attributeRange.start;
                        }
                        AddAttribute();
                        return;

                    case '/':
                        if (_attributeRange.length == 0)
                        {
                            _attributeRange.length = _position - 1 - _attributeRange.start;
                        }
                        AddAttribute();
                        SelfClosingStartTag();
                        return;

                    case '=':
                        BeforeAttributeValue();
                        return;

                    case '\t':
                    case '\r':
                    case '\n':
                    case '\f':
                    case ' ':
                        if (_attributeRange.length == 0)
                        {
                            _attributeRange.length = _position - 1 - _attributeRange.start;
                        }
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
                    case '\t':
                    case '\r':
                    case '\n':
                    case '\f':
                    case ' ':
                        break;

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

                    case '\t':
                    case '\r':
                    case '\n':
                    case '\f':
                    case ' ':
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

        private char Peek()
        {
            return _position < _length ? _html[_position] : '\0';
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
    }
}
