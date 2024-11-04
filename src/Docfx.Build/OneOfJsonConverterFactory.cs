// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using OneOf;

#nullable enable

namespace Docfx.Build;

class OneOfJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert.IsAssignableTo(typeof(IOneOf));
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        return (JsonConverter)Activator.CreateInstance(typeof(OneOfJsonConverter<>).MakeGenericType(typeToConvert))!;
    }

    private class OneOfJsonConverter<T> : JsonConverter<T> where T : IOneOf
    {
        private static readonly (Type type, MethodInfo cast)[] s_types = GetOneOfTypes();

        [DebuggerNonUserCode]
        public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType is JsonTokenType.Null)
                return default;

            // NOTE:
            // This is a pretty slow implementation by attempting to deserialize into every possible union case.
            // It also depends on marking discriminator properties as required.
            foreach (var (type, cast) in s_types)
            {
                try
                {
                    Utf8JsonReader readerCopy = reader;
                    var result = JsonSerializer.Deserialize(ref readerCopy, type, options);
                    reader.Skip();
                    return (T)cast.Invoke(null, [result])!;
                }
                catch (JsonException)
                {
                }
            }

            throw new JsonException($"Cannot deserialize into one of the supported types for {typeToConvert}");
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            object obj = value;
            while (obj is IOneOf oneof)
                obj = oneof.Value;

            JsonSerializer.Serialize(writer, obj, options);
        }

        private static (Type type, MethodInfo cast)[] GetOneOfTypes()
        {
            var casts = typeof(T).GetRuntimeMethods().Where(m => m.IsSpecialName && m.Name == "op_Implicit").ToArray();
            var type = typeof(T);
            while (type != null)
            {
                if (type.IsGenericType && (type.Name.StartsWith("OneOf`") || type.Name.StartsWith("OneOfBase`")))
                {
                    return type.GetGenericArguments().Select(t => (t, casts.First(c => c.GetParameters()[0].ParameterType == t))).ToArray();
                }

                type = type.BaseType;
            }
            throw new InvalidOperationException($"{typeof(T)} isn't OneOf or OneOfBase");
        }
    }
}
