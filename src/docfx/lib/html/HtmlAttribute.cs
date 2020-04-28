// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Docs.Build
{
    internal readonly struct HtmlAttribute
    {
        public readonly HtmlAttributeType Type { get; }

        public readonly ReadOnlyMemory<char> Name { get; }

        public readonly ReadOnlyMemory<char> Value { get; }

        public readonly ReadOnlyMemory<char> RawText { get; }

        public readonly HtmlTextRange Range { get; }

        public readonly HtmlTextRange NameRange { get; }

        public readonly HtmlTextRange ValueRange { get; }

        public bool NameIs(string name)
        {
            return Name.Span.Equals(name, StringComparison.OrdinalIgnoreCase);
        }

        public HtmlAttribute(string name, string? value = default, HtmlAttributeType? type = null)
        {
            Type = value is null ? HtmlAttributeType.NameOnly : (type ?? HtmlAttributeType.DoubleQuoted);
            Name = name.AsMemory();
            Value = value.AsMemory();
            RawText = default;
            Range = default;
            NameRange = default;
            ValueRange = default;
        }

        internal HtmlAttribute(
            HtmlAttributeType type,
            ReadOnlyMemory<char> name,
            ReadOnlyMemory<char> value,
            ReadOnlyMemory<char> rawText,
            in HtmlTextRange range,
            in HtmlTextRange nameRange,
            in HtmlTextRange valueRange)
        {
            Type = type;
            Name = name;
            Value = value;
            RawText = rawText;
            Range = range;
            NameRange = nameRange;
            ValueRange = valueRange;
        }

        public HtmlAttribute WithValue(string value)
        {
            return new HtmlAttribute(Type, Name, value.AsMemory(), default, Range, NameRange, ValueRange);
        }
    }
}
