// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

#nullable enable

namespace Microsoft.Docs.Build
{
    internal class SourceInfoJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType.IsGenericType && objectType.GetGenericTypeDefinition() == typeof(SourceInfo<>);
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            SourceInfo? source = null;

            switch (reader)
            {
                case JTokenReader tokenReader:
                    source = JsonUtility.GetSourceInfo(tokenReader.CurrentToken);
                    break;

                case JsonTextReader textReader:
                    source = new SourceInfo(JsonUtility.State.FilePath, textReader.LineNumber, textReader.LinePosition);
                    break;
            }

            var valueType = objectType.GenericTypeArguments[0];
            var value = serializer.Deserialize(reader, valueType);

            if (value is null)
            {
                JsonUtility.SkipToken(reader);

                if (existingValue is ISourceInfo existingSourceInfo)
                {
                    value = existingSourceInfo.GetValue();
                }
            }

            return Activator.CreateInstance(objectType, value, source);
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            if (value is ISourceInfo sourceInfo)
            {
                serializer.Serialize(writer, sourceInfo.GetValue());
            }
        }
    }
}
