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
        private int _line;
        private int _column;

        private HtmlTokenType _type;
        private bool _isSelfClosing;

        private HtmlTextPosition _tokenStart;
        private HtmlTextPosition _tokenEnd;
        private HtmlTextPosition _nameStart;
        private HtmlTextPosition _nameEnd;

        private int _attributesLength;
        private HtmlAttribute[]? _attributes;
        private HtmlAttributeType _attributeType;

        private HtmlTextPosition _attributeNameStart;
        private HtmlTextPosition _attributeNameEnd;
        private HtmlTextPosition _attributeValueStart;
        private HtmlTextPosition _attributeValueEnd;
        private HtmlTextPosition _attributeStart;
        private HtmlTextPosition _attributeEnd;

        public HtmlReader(string html)
            : this()
        {
            _html = html;
            _length = html.Length;
        }

        public bool ReadToEndTag(ReadOnlySpan<char> name)
        {
            if (((_type == HtmlTokenType.StartTag && _isSelfClosing) || _type == HtmlTokenType.EndTag) &&
                _html.AsSpan(_nameStart.Index, _nameEnd.Index - _nameStart.Index).Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            while (Read(out var token))
            {
                if (token.Type == HtmlTokenType.EndTag && token.NameIs(name))
                {
                    return true;
                }
            }

            return false;
        }

        public bool Read(out HtmlToken token)
        {
            // Simplified pasing algorithm based on https://html.spec.whatwg.org/multipage/parsing.html
            _attributesLength = 0;
            _tokenStart = _tokenEnd = Position();
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

            switch (Current())
            {
                case '<':
                    Consume();
                    TagOpen();
                    break;

                default:
                    Consume();
                    Data();
                    break;
            }

            _tokenEnd = Position();

            token = new HtmlToken(
                _type,
                _isSelfClosing,
                _html.AsMemory(_nameStart.Index, _nameEnd.Index - _nameStart.Index),
                _html.AsMemory(_tokenStart.Index, _tokenEnd.Index - _tokenStart.Index),
                _attributes.AsMemory(0, _attributesLength),
                new HtmlTextRange(_tokenStart, _tokenEnd),
                new HtmlTextRange(_nameStart, _nameEnd));

            return true;
        }

        private void Data()
        {
            _type = HtmlTokenType.Text;

            while (true)
            {
                switch (Current())
                {
                    case '\0':
                        Consume();
                        return;

                    case '<':
                        return;

                    default:
                        Consume();
                        break;
                }
            }
        }

        private void TagOpen()
        {
            _type = HtmlTokenType.StartTag;

            switch (Current())
            {
                case '!':
                    Consume();
                    MarkdownDeclarationOpen();
                    break;

                case '?':
                    BogusComment();
                    break;

                case '/':
                    Consume();
                    EndTagOpen();
                    break;

                case char c when IsASCIIAlpha(c):
                    TagName(readAttributes: true);
                    break;

                default:
                    Data();
                    break;
            }
        }

        private void MarkdownDeclarationOpen()
        {
            switch (Current())
            {
                case '-' when Peek() == '-':
                    Consume();
                    Consume();
                    CommentStart();
                    break;

                default:
                    Consume();
                    BogusComment();
                    break;
            }
        }

        private void CommentStart()
        {
            _type = HtmlTokenType.Comment;
            switch (Current())
            {
                case '-':
                    Consume();
                    CommentStartDash();
                    break;

                case '>':
                    Consume();
                    break;

                default:
                    Comment();
                    break;
            }
        }

        private void CommentStartDash()
        {
            switch (Current())
            {
                case '-':
                    Consume();
                    CommentEnd();
                    break;

                case '\0':
                case '>':
                    Consume();
                    break;

                default:
                    Comment();
                    break;
            }
        }

        private void Comment()
        {
            while (true)
            {
                switch (Current())
                {
                    case '<':
                        Consume();
                        CommentLessThanSign();
                        return;

                    case '-':
                        Consume();
                        CommendEndDash();
                        return;

                    case '\0':
                        Consume();
                        return;

                    default:
                        Consume();
                        break;
                }
            }
        }

        private void CommentLessThanSign()
        {
            while (true)
            {
                switch (Current())
                {
                    case '!':
                        Consume();
                        CommentLessThanSignBang();
                        return;

                    case '<':
                        Consume();
                        break;

                    default:
                        Comment();
                        return;
                }
            }
        }

        private void CommentLessThanSignBang()
        {
            switch (Current())
            {
                case '-':
                    Consume();
                    CommentLessThanSignBangDash();
                    return;

                default:
                    Comment();
                    return;
            }
        }

        private void CommentLessThanSignBangDash()
        {
            switch (Current())
            {
                case '-':
                    CommentEnd();
                    return;

                default:
                    Comment();
                    return;
            }
        }

        private void CommendEndDash()
        {
            switch (Current())
            {
                case '-':
                    Consume();
                    CommentEnd();
                    return;

                case '\0':
                    Consume();
                    return;

                default:
                    Comment();
                    break;
            }
        }

        private void CommentEnd()
        {
            while (true)
            {
                switch (Current())
                {
                    case '\0':
                    case '>':
                        Consume();
                        return;

                    case '!':
                        Consume();
                        CommentEndBang();
                        return;

                    case '-':
                        Consume();
                        break;

                    default:
                        Comment();
                        return;
                }
            }
        }

        private void CommentEndBang()
        {
            switch (Current())
            {
                case '-':
                    Consume();
                    CommentEnd();
                    return;

                case '>':
                case '\0':
                    Consume();
                    return;

                default:
                    Comment();
                    break;
            }
        }

        private void BogusComment()
        {
            _type = HtmlTokenType.Comment;
            while (true)
            {
                switch (Current())
                {
                    case '>':
                        Consume();
                        return;

                    default:
                        Consume();
                        break;
                }
            }
        }

        private void EndTagOpen()
        {
            _type = HtmlTokenType.EndTag;

            switch (Current())
            {
                case '>':
                    Consume();
                    _nameEnd = _nameStart;
                    break;

                case '\0':
                    Consume();
                    _type = HtmlTokenType.Text;
                    break;

                case char c when IsASCIIAlpha(c):
                    TagName(readAttributes: false);
                    break;

                default:
                    Consume();
                    BogusComment();
                    break;
            }
        }

        private void TagName(bool readAttributes)
        {
            _nameStart = _nameEnd = Position();

            while (true)
            {
                switch (Current())
                {
                    case '\0':
                        _type = HtmlTokenType.Comment;
                        Consume();
                        return;

                    case '>':
                        if (_nameEnd.Index == _nameStart.Index)
                        {
                            _nameEnd = Position();
                        }
                        Consume();
                        return;

                    case '/':
                        if (_nameEnd.Index == _nameStart.Index)
                        {
                            _nameEnd = Position();
                        }
                        Consume();
                        SelfClosingStartTag();
                        return;

                    case '\t':
                    case '\r':
                    case '\n':
                    case '\f':
                    case ' ':
                        if (_nameEnd.Index == _nameStart.Index)
                        {
                            _nameEnd = Position();
                        }
                        Consume();
                        if (readAttributes)
                        {
                            BeforeAttributeName();
                            return;
                        }
                        break;

                    default:
                        Consume();
                        break;
                }
            }
        }

        private void SelfClosingStartTag()
        {
            switch (Current())
            {
                case '>':
                    _isSelfClosing = true;
                    Consume();
                    break;

                case '\0':
                    _type = HtmlTokenType.Comment;
                    Consume();
                    break;

                default:
                    BeforeAttributeName();
                    break;
            }
        }

        private void BeforeAttributeName()
        {
            while (true)
            {
                switch (Current())
                {
                    case '\0':
                    case '/':
                    case '>':
                        AfterAttributeName();
                        return;

                    case '\t':
                    case '\r':
                    case '\n':
                    case '\f':
                    case ' ':
                        Consume();
                        break;

                    case '=':
                        AttributeName(consumeOnce: true);
                        return;

                    default:
                        AttributeName();
                        return;
                }
            }
        }

        private void AttributeName(bool consumeOnce = false)
        {
            _attributeType = HtmlAttributeType.NameOnly;
            _attributeNameStart = _attributeNameEnd = _attributeStart = _attributeEnd = Position();
            _attributeValueStart = _attributeValueEnd = default;

            if (consumeOnce)
            {
                Consume();
            }

            while (true)
            {
                switch (Current())
                {
                    case '\t':
                    case '\r':
                    case '\n':
                    case '\f':
                    case ' ':
                    case '/':
                    case '>':
                    case '\0':
                        _attributeNameEnd = Position();
                        AfterAttributeName();
                        return;

                    case '=':
                        _attributeNameEnd = Position();
                        Consume();
                        BeforeAttributeValue();
                        return;

                    default:
                        Consume();
                        break;
                }
            }
        }

        private void AfterAttributeName()
        {
            while (true)
            {
                switch (Current())
                {
                    case '\0':
                        _type = HtmlTokenType.Comment;
                        Consume();
                        return;

                    case '>':
                        if (_attributeEnd.Index == _attributeStart.Index)
                        {
                            _attributeEnd = Position();
                        }
                        Consume();
                        AddAttribute();
                        return;

                    case '/':
                        if (_attributeEnd.Index == _attributeStart.Index)
                        {
                            _attributeEnd = Position();
                        }
                        Consume();
                        AddAttribute();
                        SelfClosingStartTag();
                        return;

                    case '=':
                        Consume();
                        BeforeAttributeValue();
                        return;

                    case '\t':
                    case '\r':
                    case '\n':
                    case '\f':
                    case ' ':
                        if (_attributeEnd.Index == _attributeStart.Index)
                        {
                            _attributeEnd = Position();
                        }
                        Consume();
                        break;

                    default:
                        AddAttribute();
                        AttributeName();
                        return;
                }
            }
        }

        private void BeforeAttributeValue()
        {
            while (true)
            {
                switch (Current())
                {
                    case '\t':
                    case '\r':
                    case '\n':
                    case '\f':
                    case ' ':
                        Consume();
                        break;

                    case '\'':
                        _attributeType = HtmlAttributeType.SingleQuoted;
                        Consume();
                        AttributeValue('\'');
                        return;

                    case '"':
                        _attributeType = HtmlAttributeType.DoubleQuoted;
                        Consume();
                        AttributeValue('"');
                        return;

                    case '>':
                        _attributeValueEnd = _attributeValueStart;
                        _attributeEnd = Position();
                        Consume();
                        AddAttribute();
                        return;

                    default:
                        _attributeType = HtmlAttributeType.Unquoted;
                        AttributeValueUnquoted();
                        return;
                }
            }
        }

        private void AttributeValue(char quote)
        {
            _attributeValueStart = Position();

            while (true)
            {
                switch (Current())
                {
                    case '\0':
                        _type = HtmlTokenType.Comment;
                        Consume();
                        return;

                    case char c when c == quote:
                        _attributeValueEnd = Position();
                        Consume();
                        _attributeEnd = Position();
                        AddAttribute();
                        BeforeAttributeName();
                        return;

                    default:
                        Consume();
                        break;
                }
            }
        }

        private void AttributeValueUnquoted()
        {
            _attributeValueStart = Position();

            while (true)
            {
                switch (Current())
                {
                    case '>':
                        _attributeValueEnd = Position();
                        _attributeEnd = Position();
                        Consume();
                        AddAttribute();
                        return;

                    case '\0':
                        _type = HtmlTokenType.Comment;
                        Consume();
                        return;

                    case '\t':
                    case '\r':
                    case '\n':
                    case '\f':
                    case ' ':
                        _attributeValueEnd = Position();
                        _attributeEnd = Position();
                        Consume();
                        AddAttribute();
                        BeforeAttributeName();
                        return;

                    default:
                        Consume();
                        break;
                }
            }
        }

        private void AddAttribute()
        {
            if (_attributeNameEnd.Index == _attributeNameStart.Index)
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
                _html.AsMemory(_attributeNameStart.Index, _attributeNameEnd.Index - _attributeNameStart.Index),
                _html.AsMemory(_attributeValueStart.Index, _attributeValueEnd.Index - _attributeValueStart.Index),
                _html.AsMemory(_attributeStart.Index, _attributeEnd.Index - _attributeStart.Index),
                new HtmlTextRange(_attributeStart, _attributeEnd),
                new HtmlTextRange(_attributeNameStart, _attributeNameEnd),
                new HtmlTextRange(_attributeValueStart, _attributeValueEnd));

            _attributeNameStart = _attributeNameEnd = default;
        }

        private void Consume()
        {
            if (_position < _length)
            {
                if (_html[_position] == '\n')
                {
                    _column = 0;
                    _line++;
                }
                else
                {
                    _column++;
                }
                _position++;
            }
        }

        private char Current()
        {
            return _position < _length ? _html[_position] : '\0';
        }

        private char Peek()
        {
            return _position + 1 < _length ? _html[_position + 1] : '\0';
        }

        private HtmlTextPosition Position()
        {
            return new HtmlTextPosition(_position, _line, _column);
        }

        private static bool IsASCIIAlpha(char c)
        {
            return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
        }
    }
}
