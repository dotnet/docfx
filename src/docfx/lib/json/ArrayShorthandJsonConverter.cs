// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.Docs.Build
{
    /// <summary>
    /// A JsonConverter that allows single item arrays to be shorthanded as that single item.
    /// </summary>
    internal class ArrayShorthandJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType.IsArray;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType != JsonToken.StartArray)
            {
                var contract = (JsonArrayContract)serializer.ContractResolver.ResolveContract(objectType);
                return serializer.Deserialize(reader, contract.CollectionItemType);
            }
            return serializer.Deserialize(reader, objectType);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }
    }
}
