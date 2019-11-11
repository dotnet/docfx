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
    internal readonly struct PathString : IEquatable<PathString>, IComparable<PathString>
    {
        public readonly string Value;

        public PathString(string value) => Value = PathUtility.Normalize(value);

        public override string ToString() => Value?.ToString();

        public bool Equals(PathString other) => PathUtility.PathComparer.Equals(Value, other.Value);

        public override bool Equals(object obj) => obj is PathString && Equals((PathString)obj);

        public override int GetHashCode() => Value is null ? 0 : PathUtility.PathComparer.GetHashCode(Value);

        public int CompareTo(PathString other) => PathUtility.PathComparer.Compare(Value, other.Value);

        public static bool operator ==(PathString a, PathString b) => Equals(a, b);

        public static bool operator !=(PathString a, PathString b) => !Equals(a, b);

        public static implicit operator string(PathString value) => value.Value;

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
