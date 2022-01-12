// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build;

internal class SourceInfoJsonConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        objectType = UnwrapNullable(objectType);

        return objectType.IsGenericType && objectType.GetGenericTypeDefinition() == typeof(SourceInfo<>);
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        SourceInfo? source = null;

        switch (reader)
        {
            case JTokenReader tokenReader:
                source = tokenReader.CurrentToken?.GetSourceInfo();
                break;

            case JsonTextReader textReader:
                var filePath = JsonUtility.State?.FilePath;
                if (filePath != null)
                {
                    source = new SourceInfo(filePath, textReader.LineNumber, textReader.LinePosition);
                }
                break;
        }

        objectType = UnwrapNullable(objectType);

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

    private static Type UnwrapNullable(Type objectType)
    {
        return objectType.IsGenericType && objectType.GetGenericTypeDefinition() == typeof(Nullable<>)
            ? objectType.GetGenericArguments()[0]
            : objectType;
    }
}
