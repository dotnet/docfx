// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using Newtonsoft.Json;

namespace Microsoft.Docs.Build
{
    /// <summary>
    /// Represents a normalized file path, directory path, or URL path string.
    /// </summary>
    [JsonConverter(typeof(PathStringJsonConverter))]
    [TypeConverter(typeof(PathStringTypeConverter))]
    internal struct PathString : IEquatable<PathString>, IComparable<PathString>
    {
        private string _value;

        /// <summary>
        /// A non-nullable, non-empty string that can never contain
        ///     - backslashes
        ///     - consegtive dots
        ///     - consegtive forward slashes
        ///     - leading ./
        /// </summary>
        public string Value => string.IsNullOrEmpty(_value) ? "." : _value;

        public bool IsDefault => string.IsNullOrEmpty(_value);

        public PathString(string value) => _value = PathUtility.Normalize(value);

        public PathString GetFileName() => new PathString { _value = Path.GetFileName(Value) };

        public override string ToString() => Value;

        public bool Equals(PathString other) => PathUtility.PathComparer.Equals(Value, other.Value);

        public override bool Equals(object obj) => obj is PathString && Equals((PathString)obj);

        public override int GetHashCode() => PathUtility.PathComparer.GetHashCode(Value);

        public int CompareTo(PathString other) => string.CompareOrdinal(Value, other.Value);

        public static bool operator ==(PathString a, PathString b) => Equals(a, b);

        public static bool operator !=(PathString a, PathString b) => !Equals(a, b);

        public static implicit operator string(PathString value) => value.Value;

        /// <summary>
        /// Concat two <see cref="PathString"/>s together.
        /// </summary>
        public static PathString operator +(PathString a, PathString b)
        {
            if (string.IsNullOrEmpty(a._value))
                return b;

            if (string.IsNullOrEmpty(b._value))
                return a;

            if (b._value[0] == '/')
                return b;

            var str = a._value[a._value.Length - 1] == '/'
                ? a._value + b._value
                : a._value + '/' + b._value;

            if (b._value[0] == '.')
                return new PathString { _value = PathUtility.Normalize(str) };

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
                remainingPath = new PathString { _value = _value.Substring(i) };
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
                remainingPath = new PathString { _value = _value.Substring(i + 1) };
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
                return string.Compare(a, 0, b, 0, b.Length, PathUtility.PathComparison) == 0 && a[a.Length - 1] == '/';
            }

            if (b.Length == a.Length + 1)
            {
                return string.Compare(a, 0, b, 0, a.Length, PathUtility.PathComparison) == 0 && b[b.Length - 1] == '/';
            }

            return false;
        }

        private class PathStringJsonConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType) => objectType == typeof(PathString);

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                var value = serializer.Deserialize<string>(reader);
                return new PathString(value is null ? null : PathUtility.NormalizeFile(value));
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                writer.WriteValue(((PathString)value).Value);
            }
        }

        private class PathStringTypeConverter : TypeConverter
        {
            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            {
                return sourceType == typeof(string) ? true : base.CanConvertFrom(context, sourceType);
            }

            public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
            {
                return destinationType == typeof(string) ? true : base.CanConvertTo(context, destinationType);
            }

            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
            {
                return value is string str ? new PathString(str) : base.ConvertFrom(context, culture, value);
            }

            public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
            {
                return destinationType == typeof(string) ? ((PathString)value).Value : base.ConvertTo(context, culture, value, destinationType);
            }
        }
    }
}
