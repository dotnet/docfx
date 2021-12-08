// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build;

internal readonly struct SourceInfo<T> : ISourceInfo, IEquatable<SourceInfo<T>>
{
    public readonly T Value;

    public readonly SourceInfo? Source;

    public override string? ToString() => Value?.ToString();

    public SourceInfo<T> Or(SourceInfo<T> other)
        => new(Value != null ? Value : other.Value, other.Source ?? Source);

    public SourceInfo<T> Or(SourceInfo<T>? other)
        => new(Value != null ? Value : (other != null ? other.Value : default), other?.Source ?? Source);

    public SourceInfo<T> Or(T other)
        => new(Value != null ? Value : other, Source);

    public SourceInfo<T> With(T value) => new(value, Source);

    public static implicit operator T(SourceInfo<T> value) => value.Value;

    public static implicit operator T(SourceInfo<T>? value) => value != null ? value.Value : default;

    public static implicit operator SourceInfo?(in SourceInfo<T> value) => value.Source;

    public static implicit operator SourceInfo?(in SourceInfo<T>? value) => value?.Source;

    public override bool Equals(object? obj) => obj is SourceInfo<T> si && Equals(si);

    public bool Equals(SourceInfo<T> other) => Equals(Value, other.Value) && Equals(Source, other.Source);

    public override int GetHashCode() => HashCode.Combine(Value, Source);

    object? ISourceInfo.GetValue() => Value;

    public SourceInfo(T value, in SourceInfo? source = null)
    {
        Source = source;
        Value = value;
    }
}
