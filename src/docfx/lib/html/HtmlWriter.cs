// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text;

namespace Microsoft.Docs.Build
{
    /// <summary>
    /// Provides a high-performance API for forward-only, non-cached writing of HTML read from HtmlReader.
    /// </summary>
    internal class HtmlWriter
    {
        private readonly StringBuilder _result;

        private string? _replacedTag;
        private string? _replacedTagName;
        private string? _replacedToken;

        private ArrayBuilder<string> _newAttributes;
        private ArrayBuilder<(int, string)> _replacedAttributes;
        private ArrayBuilder<(int, string)> _replacedAttributeValues;

        public HtmlWriter(int capacity)
        {
            _result = new StringBuilder(capacity);
        }

        public override string ToString()
        {
            return _result.ToString();
        }

        public void Write(HtmlReader reader)
        {
            if (_replacedTag != null && reader.Type == HtmlTokenType.StartTag)
            {
                if (!reader.IsSelfClosing)
                {
                    _replacedTagName = reader.Name.ToString();
                }
                _result.Append(_replacedTag);
                _replacedTag = null;
            }
            else if (_replacedTagName != null)
            {
                if (reader.Type == HtmlTokenType.EndTag && reader.NameIs(_replacedTagName))
                {
                    _replacedTagName = null;
                }
            }
            else if (_replacedToken != null)
            {
                _result.Append(_replacedToken);
            }
            else if (reader.Type != HtmlTokenType.StartTag)
            {
                _result.Append(reader.Token);
            }
            else if (_newAttributes.Length == 0 && _replacedAttributes.Length == 0 && _replacedAttributeValues.Length == 0)
            {
                _result.Append(reader.Token);
            }
            else
            {
                _result.Append('<').Append(reader.Name);

                var pos = reader.TokenRange.start + reader.Name.Length + 1;

                foreach (ref readonly var attribute in reader.Attributes)
                {
                    var replaced = false;

                    foreach (var (start, value) in _replacedAttributes.Span)
                    {
                        var tokenRange = attribute.TokenRange;
                        if (tokenRange.start == start)
                        {
                            _result.Append(reader.Html, pos, start - pos);
                            _result.Append(value);
                            pos = tokenRange.start + tokenRange.length;
                            replaced = true;
                            break;
                        }
                    }

                    if (!replaced)
                    {
                        foreach (var (start, value) in _replacedAttributeValues.Span)
                        {
                            var valueRange = attribute.ValueRange;
                            if (valueRange.start == start && start > 0)
                            {
                                _result.Append(reader.Html, pos, start - pos);
                                _result.Append(value);
                                pos = valueRange.start + valueRange.length;
                                break;
                            }
                        }
                    }
                }

                foreach (var attribute in _newAttributes.Span)
                {
                    _result.Append(' ').Append(attribute).Append(' ');
                }

                var end = reader.TokenRange.start + reader.TokenRange.length;
                _result.Append(reader.Html, pos, end - pos);
            }

            // Reset
            _replacedToken = null;
            _newAttributes.Clear();
            _replacedAttributes.Clear();
            _replacedAttributeValues.Clear();
        }

        public void ReplaceTag(string tag)
        {
            if (_replacedTag is null)
            {
                _replacedTag = tag;
            }
        }

        public void RemoveTag()
        {
            ReplaceTag("");
        }

        public void ReplaceToken(string token)
        {
            _replacedToken = token;
        }

        public void RemoveToken()
        {
            _replacedToken = "";
        }

        public void ReplaceAttribute(in HtmlAttribute attribute, string value)
        {
            _replacedAttributes.Add((attribute.TokenRange.start, value));
        }

        public void RemoveAttribute(in HtmlAttribute attribute)
        {
            _replacedAttributes.Add((attribute.TokenRange.start, ""));
        }

        public void InsertAttribute(string value)
        {
            _newAttributes.Add(value);
        }

        public void ReplaceAttributeValue(in HtmlAttribute attribute, string value)
        {
            _replacedAttributeValues.Add((attribute.ValueRange.start, value));
        }
    }
}
