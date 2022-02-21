// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Globalization;
using Newtonsoft.Json;

namespace Microsoft.Docs.Build;

/// <summary>
/// Represents a normalized file path, directory path, or URL path string.
/// </summary>
[JsonConverter(typeof(PathStringJsonConverter))]
[TypeConverter(typeof(PathStringTypeConverter))]
internal struct PathString : IEquatable<PathString>, IComparable<PathString>
{
    private string? _value;

    /// <summary>
    /// A non-nullable, non-empty string that can never contain
    ///     - backslashes
    ///     - consecutive dots
    ///     - consecutive forward slashes
    ///     - leading ./
    /// </summary>
    public string Value => string.IsNullOrEmpty(_value) ? "." : _value;

    public bool IsDefault => string.IsNullOrEmpty(_value);

    public PathString(string value) => _value = PathUtility.Normalize(value);

    public PathString GetFileName() => new() { _value = Path.GetFileName(Value) };

    public override string ToString() => Value;

    public bool Equals(PathString other) => PathUtility.PathComparer.Equals(Value, other.Value);

    public override bool Equals(object? obj) => obj is PathString value && Equals(value);

    public override int GetHashCode() => PathUtility.PathComparer.GetHashCode(Value);

    public int CompareTo(PathString other) => string.CompareOrdinal(Value, other.Value);

    public static PathString DangerousCreate(string value) => new(value);

    public static bool operator ==(PathString a, PathString b) => Equals(a, b);

    public static bool operator !=(PathString a, PathString b) => !Equals(a, b);

    public static implicit operator string(PathString value) => value.Value;

    /// <summary>
    /// Concat two <see cref="PathString"/>s together.
    /// </summary>
    public PathString Concat(PathString path)
    {
        if (string.IsNullOrEmpty(_value))
        {
            return path;
        }

        if (string.IsNullOrEmpty(path._value))
        {
            return this;
        }

        if (path._value[0] == '/')
        {
            return path;
        }

        if (path._value.Contains(':'))
        {
            return path;
        }

        var str = _value[^1] == '/'
            ? _value + path._value
            : _value + '/' + path._value;

        if (path._value[0] == '.')
        {
            return new PathString { _value = PathUtility.Normalize(str) };
        }

        return new PathString { _value = str };
    }

    /// <summary>
    /// Check if the file is the same as matcher or is inside the directory specified by matcher.
    /// </summary>
    public bool StartsWithPath(PathString basePath, out PathString remainingPath)
    {
        if (string.IsNullOrEmpty(basePath._value))
        {
            remainingPath = this;
            return true;
        }

        if (string.IsNullOrEmpty(_value))
        {
            remainingPath = default;
            return false;
        }

        if (!_value.StartsWith(basePath._value, PathUtility.PathComparison))
        {
            remainingPath = default;
            return false;
        }

        var i = basePath._value.Length;
        if (basePath._value[i - 1] == '/')
        {
            // a/b starts with a/
            remainingPath = new PathString { _value = _value[i..] };
            return true;
        }

        if (_value.Length <= i)
        {
            // a starts with a
            remainingPath = default;
            return true;
        }

        if (_value[i] == '/')
        {
            // a/b starts with a
            remainingPath = new PathString { _value = _value[(i + 1)..] };
            return true;
        }

        remainingPath = default;
        return false;
    }

    public bool FolderEquals(PathString value)
    {
        var a = _value ?? "";
        var b = value._value ?? "";

        if (a.Length == b.Length)
        {
            return string.Equals(a, b, PathUtility.PathComparison);
        }

        if (a.Length == b.Length + 1)
        {
            return string.Compare(a, 0, b, 0, b.Length, PathUtility.PathComparison) == 0 && a[^1] == '/';
        }

        if (b.Length == a.Length + 1)
        {
            return string.Compare(a, 0, b, 0, a.Length, PathUtility.PathComparison) == 0 && b[^1] == '/';
        }

        return false;
    }

    public string GetRelativePath(PathString path)
    {
        return Path.GetRelativePath(Value, path);
    }

    private class PathStringJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => objectType == typeof(PathString);

        public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            var value = serializer.Deserialize<string>(reader);
            return value is null ? default : new PathString(PathUtility.CheckInvalidPathString(value));
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            if (value is PathString path)
            {
                writer.WriteValue(path.Value);
            }
        }
    }

    private class PathStringTypeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        {
            return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
        }

        public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
        {
            return destinationType == typeof(string) || base.CanConvertTo(context, destinationType);
        }

        public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
        {
            return value is string str ? new PathString(str) : base.ConvertFrom(context, culture, value);
        }

        public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
        {
            return destinationType == typeof(string) && value is PathString path ? path.Value : base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
