// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Docs.Build
{
    internal struct HtmlToken
    {
        public HtmlTokenType Type { get; }

        public bool IsSelfClosing { get; }

        public ReadOnlyMemory<char> Name { get; }

        public ReadOnlyMemory<char> RawText { get; }

        public Memory<HtmlAttribute> Attributes { get; private set; }

        public HtmlTextRange Range { get; }

        public HtmlTextRange NameRange { get; }

        public bool NameIs(ReadOnlySpan<char> name)
        {
            return Name.Span.Equals(name, StringComparison.OrdinalIgnoreCase);
        }

        public HtmlToken(string rawText)
            : this()
        {
            RawText = rawText.AsMemory();
        }

        internal HtmlToken(
            HtmlTokenType type,
            bool isSelfClosing,
            ReadOnlyMemory<char> name,
            ReadOnlyMemory<char> rawText,
            Memory<HtmlAttribute> attributes,
            in HtmlTextRange range,
            in HtmlTextRange nameRange)
        {
            Type = type;
            IsSelfClosing = isSelfClosing;
            Name = name;
            RawText = rawText;
            Attributes = attributes;
            Range = range;
            NameRange = nameRange;
        }

        public void SetAttributeValue(string name, string value)
        {
            foreach (ref var attribute in Attributes.Span)
            {
                if (attribute.NameIs(name))
                {
                    attribute = attribute.WithValue(value);
                    return;
                }
            }

            // TODO: This API is very specific for appending data-linktype
            var attributes = new HtmlAttribute[Attributes.Length + 1];
            attributes[Attributes.Length] = new HtmlAttribute(name, value);
            Attributes.CopyTo(attributes);
            Attributes = attributes;
        }

        public string? GetAttributeValueByName(string name)
        {
            foreach (ref var attribute in Attributes.Span)
            {
                if (attribute.NameIs(name))
                {
                    return attribute.Value.ToString();
                }
            }

            return null;
        }
    }
}
