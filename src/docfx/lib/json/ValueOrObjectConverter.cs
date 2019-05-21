// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json;

namespace Microsoft.Docs.Build
{
    internal class ValueOrObjectConverter<T1, T2> : JsonConverter where T2 : class
    {
        public override bool CanWrite => false;

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) => throw new InvalidOperationException();

        public override bool CanConvert(Type objectType) => true;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            // T1 should be a nullable type
            if (default(T1) != null)
                throw new TypeInitializationException(typeof(T1).FullName, null);

            if (reader.TokenType == JsonToken.Null)
                return ValueTuple.Create<T1, T2>(default, default);

            if (reader.TokenType != JsonToken.StartObject)
            {
                return ValueTuple.Create<T1, T2>((T1)serializer.Deserialize(reader), default);
            }
            return ValueTuple.Create<T1, T2>(default, (T2)serializer.Deserialize(reader, typeof(T2)));
        }
    }
}
