// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Microsoft.Docs.Build
{
    public sealed class SourceInfo<T> : SourceInfo
    {
        public T Value { get; set; }

        internal override object GetValue() => Value;

        public SourceInfo(T value, SourceInfo source = null)
            : base(source?.File, source?.Line ?? 0, source?.Column ?? 0, source?.EndLine ?? 0, source?.EndColumn ?? 0)
        {
            Value = value;
        }

        public SourceInfo<T> WithValue(T value)
        {
            if (value == default)
                return null;

            return new SourceInfo<T>(value, this);
        }

        public override string ToString()
            => Value?.ToString();

        public static implicit operator T(SourceInfo<T> value)
        {
            return value != null ? value.Value : default;
        }
    }
}
