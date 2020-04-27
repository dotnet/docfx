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

        public Memory<HtmlAttribute> Attributes { get; }

        public (int start, int length) Range { get; }

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
            (int start, int length) range)
        {
            Type = type;
            IsSelfClosing = isSelfClosing;
            Name = name;
            RawText = rawText;
            Attributes = attributes;
            Range = range;
        }
    }
}
