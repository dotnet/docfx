// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json;
namespace Microsoft.Docs.Build
{
    /// <summary>
    /// A JsonConverter that allows single item arrays to be shorthanded as that single item.
    /// Only works for Arrays, does not support other collections like List<>.
    /// </summary>
    internal class ExpandArrayJsonConverter : JsonConverter
    {
        public override bool CanWrite => false;

        public override bool CanConvert(Type objectType) => objectType.IsArray;

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) => throw new InvalidOperationException();

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType != JsonToken.StartArray)
            {
                var elementType = objectType.GetElementType();
                var result = Array.CreateInstance(elementType, 1);
                result.SetValue(serializer.Deserialize(reader, elementType), 0);
                return result;
            }
            return serializer.Deserialize(reader, objectType);
        }
    }
}
