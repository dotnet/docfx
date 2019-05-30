// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build
{
    public readonly struct SourceInfo<T> : ISourceInfo
    {
        public readonly T Value;

        public readonly SourceInfo Source;

        public override string ToString() => Value?.ToString();

        public SourceInfo<T> Or(in SourceInfo<T> value) => Value != null ? this : value;

        public static implicit operator T(in SourceInfo<T> value) => value.Value;

        public static implicit operator SourceInfo(in SourceInfo<T> value) => value.Source;

        object ISourceInfo.GetValue() => Value;

        public SourceInfo(T value, in SourceInfo source = null)
        {
            Source = source;
            Value = value;
        }
    }
}
