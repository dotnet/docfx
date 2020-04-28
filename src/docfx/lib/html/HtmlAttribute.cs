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

        public readonly (int start, int end) Range { get; }

        public readonly (int start, int end) ValueRange { get; }

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
            ValueRange = default;
        }

        internal HtmlAttribute(
            HtmlAttributeType type,
            ReadOnlyMemory<char> name,
            ReadOnlyMemory<char> value,
            ReadOnlyMemory<char> rawText,
            (int start, int end) range,
            (int start, int end) valueRange)
        {
            Type = type;
            Name = name;
            Value = value;
            RawText = rawText;
            Range = range;
            ValueRange = valueRange;
        }

        public HtmlAttribute WithValue(string value)
        {
            return new HtmlAttribute(Type, Name, value.AsMemory(), default, Range, ValueRange);
        }
    }
}
