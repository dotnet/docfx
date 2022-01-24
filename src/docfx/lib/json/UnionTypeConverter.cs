// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Newtonsoft.Json;

namespace Microsoft.Docs.Build;

/// <summary>
/// JsonConverter that converts a tuple based on whether input JSON is a scalar, array or object.
/// </summary>
internal class UnionTypeConverter : JsonConverter
{
    public override bool CanWrite => false;

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer) => throw new InvalidOperationException();

    public override bool CanConvert(Type objectType) => typeof(ITuple).IsAssignableFrom(objectType);

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        var args = ReadJsonCore(reader, objectType, serializer);

        return Activator.CreateInstance(objectType, args);
    }

    private static object?[] ReadJsonCore(JsonReader reader, Type objectType, JsonSerializer serializer)
    {
        var genericTypes = objectType.GetGenericArguments();
        var args = new object?[genericTypes.Length];

        // Trying to find an exact match first
        for (var i = 0; i < genericTypes.Length; i++)
        {
            if (TypeExactlyMatches(reader.TokenType, UnwrapKnownGenericType(genericTypes[i])))
            {
                args[i] = serializer.Deserialize(reader, genericTypes[i]);
                return args;
            }
        }

        // Exclude types that never matches
        for (var i = 0; i < genericTypes.Length; i++)
        {
            if (!TypeNeverMatches(reader.TokenType, UnwrapKnownGenericType(genericTypes[i])))
            {
                args[i] = serializer.Deserialize(reader, genericTypes[i]);
                return args;
            }
        }

        return args;
    }

    private static bool TypeExactlyMatches(JsonToken tokenType, Type objectType) => tokenType switch
    {
        JsonToken.StartArray => objectType.IsArray,
        JsonToken.String => objectType == typeof(string),
        JsonToken.Boolean => objectType == typeof(bool),
        JsonToken.Integer => objectType == typeof(int),
        JsonToken.Float => objectType == typeof(float),
        _ => false,
    };

    private static bool TypeNeverMatches(JsonToken tokenType, Type objectType)
    {
        return objectType.IsArray && tokenType != JsonToken.StartArray;
    }

    private static Type UnwrapKnownGenericType(Type type)
    {
        // Unwrap Nullable<T>
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            type = type.GenericTypeArguments[0];
        }

        // Unwrap SourceInfo<T>
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(SourceInfo<>))
        {
            type = type.GenericTypeArguments[0];
        }
        return type;
    }
}
