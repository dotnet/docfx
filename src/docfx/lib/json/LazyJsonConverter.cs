// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json;

namespace Microsoft.Docs.Build
{
    internal class LazyJsonConverter<T> : JsonConverter
    {
        public override bool CanConvert(Type objectType)
            => objectType.IsGenericType && objectType.GetGenericTypeDefinition() == typeof(Lazy<>);

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            var value = serializer.Deserialize(reader, objectType.GenericTypeArguments[0]);

            return Activator.CreateInstance(objectType, value);
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            if (value is Lazy<T> lazy)
            {
                serializer.Serialize(writer, lazy.Value);
            }
        }
    }
}
