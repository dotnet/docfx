// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace Microsoft.Docs.Build
{
    internal readonly struct SourceInfo<T> : ISourceInfo
    {
        public readonly T Value;

        public readonly SourceInfo Source;

        public override string ToString() => Value?.ToString();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SourceInfo<T> Or(SourceInfo<T> other)
            => new SourceInfo<T>(Value != null ? Value : other.Value, other.Source ?? Source);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SourceInfo<T> Or(SourceInfo<T>? other)
            => new SourceInfo<T>(Value != null ? Value : (other != null ? other.Value : default), other?.Source ?? Source);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SourceInfo<T> Or(T other)
            => new SourceInfo<T>(Value != null ? Value : other, Source);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator T(SourceInfo<T> value) => value.Value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator T(SourceInfo<T>? value) => value != null ? value.Value : default;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator SourceInfo(in SourceInfo<T> value) => value.Source;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator SourceInfo(in SourceInfo<T>? value) => value?.Source;

        object ISourceInfo.GetValue() => Value;

        public SourceInfo(T value, in SourceInfo source = null)
        {
            Source = source;
            Value = value;
        }
    }
}
