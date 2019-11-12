// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel;
using System.Globalization;
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
        /// <summary>
        /// A nullable string that can never contains
        ///     - backslashes
        ///     - consegtive dots
        ///     - consegtive forward slashes
        ///     - leading ./
        /// </summary>
        private string _value;

        public string Value => _value ?? "";

        public PathString(string value) => _value = PathUtility.Normalize(value);

        public override string ToString() => Value?.ToString();

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
                : a.Value + '/' + b._value;

            if (b._value[0] == '.')
                return new PathString { _value = PathUtility.Normalize(str) };

            return new PathString { _value = str };
        }

        /// <summary>
        /// Check if the file is the same as matcher or is inside the directory specified by matcher.
        /// </summary>
        public bool StartsWithPath(PathString basePath, out PathString remainingPath)
        {
            var basePathValue = basePath.Value;
            var pathValue = Value;

            if (basePathValue.Length == 0)
            {
                remainingPath = this;
                return true;
            }

            if (!pathValue.StartsWith(basePathValue, PathUtility.PathComparison))
            {
                remainingPath = default;
                return false;
            }

            var i = basePathValue.Length;
            if (basePathValue[i - 1] == '/')
            {
                // a/b starts with a/
                remainingPath = new PathString { _value = pathValue.Substring(i) };
                return true;
            }

            if (pathValue.Length <= i)
            {
                // a starts with a
                remainingPath = default;
                return true;
            }

            if (pathValue[i] == '/')
            {
                // a/b starts with a
                remainingPath = new PathString { _value = pathValue.Substring(i + 1) };
                return true;
            }

            remainingPath = default;
            return false;
        }

        private class PathStringJsonConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType) => objectType == typeof(PathString);

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                var value = reader.ReadAsString();
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
