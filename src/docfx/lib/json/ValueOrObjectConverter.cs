// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json;

namespace Microsoft.Docs.Build
{
    internal class ValueOrObjectConverter : JsonConverter
    {
        public override bool CanWrite => false;

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) => throw new InvalidOperationException();

        public override bool CanConvert(Type objectType) => true;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var genericTypes = objectType.GetGenericArguments();
            if (reader.TokenType != JsonToken.StartObject)
                return Activator.CreateInstance(objectType, serializer.Deserialize(reader, genericTypes[0]), default);
            return Activator.CreateInstance(objectType, default, serializer.Deserialize(reader, genericTypes[1]));
        }
    }
}
