// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;

namespace Microsoft.Docs.Build
{
    /// <summary>
    /// Provides a high-performance, fault-tolerant, standard-compliant API
    /// for forward-only, read-only access to HTML text.
    /// </summary>
    internal ref struct HtmlReader
    {
        private readonly string _html;
        private readonly int _length;

        private int _position;

        private HtmlTokenType _type;
        private bool _isSelfClosing;

        private int _tokenStart;
        private int _tokenEnd;
        private int _nameStart;
        private int _nameEnd;

        private int _attributesLength;
        private HtmlAttribute[]? _attributes;
        private HtmlAttributeType _attributeType;

        private int _attributeNameStart;
        private int _attributeNameEnd;
        private int _attributeValueStart;
        private int _attributeValueEnd;
        private int _attributeStart;
        private int _attributeEnd;

        public HtmlReader(string html)
            : this()
        {
            _html = html;
            _length = html.Length;
        }

        public bool Read(out HtmlToken token)
        {
            // Simplified pasing algorithm based on https://html.spec.whatwg.org/multipage/parsing.html
            _attributesLength = 0;
            _tokenStart = _tokenEnd = _position;
            _nameStart = _nameEnd = default;
            _isSelfClosing = false;

            if (_position >= _length)
            {
                if (_attributes != null)
                {
                    ArrayPool<HtmlAttribute>.Shared.Return(_attributes);
                    _attributes = null;
                }
                token = default;
                return false;
            }

            switch (Consume())
            {
                case '<':
                    TagOpen();
                    break;

                default:
                    Data();
                    break;
            }

            _tokenEnd = _position;

            token = new HtmlToken(
                _type,
                _isSelfClosing,
                _html.AsMemory(_nameStart, _nameEnd - _nameStart),
                _html.AsMemory(_tokenStart, _tokenEnd - _tokenStart),
                _attributes.AsMemory(0, _attributesLength),
                (_tokenStart, _tokenEnd));

            return true;
        }

        private void Data()
        {
            _type = HtmlTokenType.Text;

            while (true)
            {
                switch (Consume())
                {
                    case '\0':
                        return;

                    case '<':
                        Back();
                        return;

                    default:
                        break;
                }
            }
        }

        private void TagOpen()
        {
            _type = HtmlTokenType.StartTag;

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
                    Data();
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
                    _nameEnd = _nameStart;
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
            _nameStart = _nameEnd = _position;

            while (true)
            {
                switch (Consume())
                {
                    case '\0':
                        _type = HtmlTokenType.Comment;
                        return;

                    case '>':
                        if (_nameEnd == _nameStart)
                        {
                            _nameEnd = _position - 1;
                        }
                        return;

                    case '/':
                        if (_nameEnd == _nameStart)
                        {
                            _nameEnd = _position - 1;
                        }
                        SelfClosingStartTag();
                        return;

                    case '\t':
                    case '\r':
                    case '\n':
                    case '\f':
                    case ' ':
                        if (_nameEnd == _nameStart)
                        {
                            _nameEnd = _position - 1;
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
                    _isSelfClosing = true;
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
                        AttributeName(-1);
                        return;

                    default:
                        Back();
                        AttributeName();
                        return;
                }
            }
        }

        private void AttributeName(int offset = 0)
        {
            _attributeType = HtmlAttributeType.NameOnly;
            _attributeNameStart = _attributeNameEnd = _attributeStart = _attributeEnd = _position + offset;
            _attributeValueStart = _attributeValueEnd = default;

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
                        _attributeNameEnd = _position;
                        AfterAttributeName();
                        return;

                    case '=':
                        _attributeNameEnd = _position - 1;
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
                        if (_attributeEnd == _attributeStart)
                        {
                            _attributeEnd = _position - 1;
                        }
                        AddAttribute();
                        return;

                    case '/':
                        if (_attributeEnd == _attributeStart)
                        {
                            _attributeEnd = _position - 1;
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
                        if (_attributeEnd == _attributeStart)
                        {
                            _attributeEnd = _position - 1;
                        }
                        break;

                    default:
                        AddAttribute();
                        Back();
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
                        _attributeType = HtmlAttributeType.SingleQuoted;
                        AttributeValue('\'');
                        return;

                    case '"':
                        _attributeType = HtmlAttributeType.DoubleQuoted;
                        AttributeValue('"');
                        return;

                    case '>':
                        _attributeValueEnd = _attributeValueStart;
                        _attributeEnd = _position - 1;
                        AddAttribute();
                        return;

                    default:
                        _attributeType = HtmlAttributeType.Unquoted;
                        Back();
                        AttributeValueUnquoted();
                        return;
                }
            }
        }

        private void AttributeValue(char quote)
        {
            _attributeValueStart = _position;

            while (true)
            {
                switch (Consume())
                {
                    case '\0':
                        _type = HtmlTokenType.Comment;
                        return;

                    case char c when c == quote:
                        _attributeValueEnd = _position - 1;
                        _attributeEnd = _position;
                        AddAttribute();
                        BeforeAttributeName();
                        return;
                }
            }
        }

        private void AttributeValueUnquoted()
        {
            _attributeValueStart = _position;

            while (true)
            {
                switch (Consume())
                {
                    case '>':
                        _attributeValueEnd = _position - 1;
                        _attributeEnd = _position - 1;
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
                        _attributeValueEnd = _position - 1;
                        _attributeEnd = _position - 1;
                        AddAttribute();
                        BeforeAttributeName();
                        return;
                }
            }
        }

        private void AddAttribute()
        {
            if (_attributeNameEnd == _attributeNameStart)
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

            _attributes[_attributesLength++] = new HtmlAttribute(
                _attributeType,
                _html.AsMemory(_attributeNameStart, _attributeNameEnd - _attributeNameStart),
                _html.AsMemory(_attributeValueStart, _attributeValueEnd - _attributeValueStart),
                _html.AsMemory(_attributeStart, _attributeEnd - _attributeStart),
                (_attributeStart, _attributeEnd),
                (_attributeValueStart, _attributeValueEnd));

            _attributeNameStart = _attributeNameEnd = default;
        }

        private char Consume()
        {
            return _position < _length ? _html[_position++] : '\0';
        }

        private char Peek()
        {
            return _position < _length ? _html[_position] : '\0';
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
