// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json;

namespace Microsoft.Docs.Build
{
    /// <summary>
    /// Represents a normalized file path string.
    /// </summary>
    [JsonConverter(typeof(JsonFilePathConverter))]
    internal readonly struct PathString : IEquatable<PathString>
    {
        public readonly string Value;

        public PathString(string value) => Value = PathUtility.Normalize(value);

        public override string ToString() => Value?.ToString();

        public bool Equals(PathString other) => PathUtility.PathComparer.Equals(Value, other.Value);

        public override bool Equals(object obj) => obj is PathString && Equals((PathString)obj);

        public override int GetHashCode() => PathUtility.PathComparer.GetHashCode(Value);

        public static implicit operator string(PathString value) => value.Value ?? ".";

        private class JsonFilePathConverter : JsonConverter
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
    }
}
