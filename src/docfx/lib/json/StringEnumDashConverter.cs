// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace Microsoft.Docs.Build
{
    internal class StringEnumDashConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => objectType == typeof(string);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var sourceStr = (string)serializer.Deserialize(reader, typeof(string));
            if (!string.IsNullOrEmpty(sourceStr))
                sourceStr = Regex.Replace(sourceStr, "(^|-)([a-z])", m => m.Groups[2].ToString().ToUpperInvariant());

            if (Nullable.GetUnderlyingType(objectType) != null)
                objectType = Nullable.GetUnderlyingType(objectType);

            if (objectType.GetEnumNames().ToList().Contains(sourceStr))
                return Enum.Parse(objectType, sourceStr);
            return null;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var sourceStr = string.Empty;
            if (value != null)
                sourceStr = Regex.Replace(value.ToString(), "[A-Z]", m => $"-{m.ToString().ToLowerInvariant()}").Substring(1);
            serializer.Serialize(writer, sourceStr);
        }
    }
}
