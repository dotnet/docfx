// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json;

namespace Microsoft.Docs.Build
{
    internal class SourceInfoJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType.IsGenericType && objectType.GetGenericTypeDefinition() == typeof(SourceInfo<>);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var source = JsonUtility.ToSourceInfo((IJsonLineInfo)reader);
            var valueType = objectType.GenericTypeArguments[0];
            var value = serializer.Deserialize(reader, valueType);

            if (value is null)
                return null;

            // TODO: populate file info
            return Activator.CreateInstance(objectType, value, source);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, ((SourceInfo)value)?.GetValue());
        }
    }
}
