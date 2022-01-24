// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Newtonsoft.Json;

namespace Microsoft.Docs.Build;

/// <summary>
/// Enables shorthand JSON form for complex object types.
/// E.g., if an object has a constructor with a string parameter, the JSON could be written as both a string or an object,
/// when it is written as string, the string constructor is called to construct the object.
/// </summary>
internal class ShortHandConverter : JsonConverter
{
    private static readonly ConcurrentDictionary<Type, Type> s_shortHandType = new();

    public override bool CanWrite => false;

    public override bool CanConvert(Type objectType) => GetShortHandType(objectType) != null;

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer) => throw new InvalidOperationException();

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        switch (reader.TokenType)
        {
            case JsonToken.Null:
            case JsonToken.Undefined:
                return null;

            case JsonToken.StartObject:
                var result = Activator.CreateInstance(objectType)!;
                serializer.Populate(reader, result);
                return result;

            default:
                var shortHandType = GetShortHandType(objectType);
                var arg = serializer.Deserialize(reader, shortHandType);
                return Activator.CreateInstance(objectType, arg);
        }
    }

    private static Type GetShortHandType(Type objectType)
    {
        return s_shortHandType.GetOrAdd(objectType, GetShortHandTypeCore);
    }

    private static Type GetShortHandTypeCore(Type objectType)
    {
        var constructors = (
            from construct in objectType.GetConstructors()
            where construct.IsPublic
            let parameters = construct.GetParameters()
            where parameters.Length == 1
            select parameters[0].ParameterType).ToArray();

        return constructors.Length == 1 ? constructors[0] : throw new InvalidOperationException();
    }
}
