// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Docfx.Build.ApiPage;
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
                if (!IsDeserializableType(ref reader, type, typeToConvert))
                    continue;

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

        /// <summary>
        /// Helper method to check it can deserialize to specified type.
        /// </summary>
        /// <param name="reader">Current reader.</param>
        /// <param name="type">The type that to be deserialized by JsonSerializer.</param>
        /// <param name="typeToConvert">The type that to be converted by JsonConverter.</param>
        private static bool IsDeserializableType(ref Utf8JsonReader reader, Type type, Type typeToConvert)
        {
            var tokenType = reader.TokenType;
            switch (tokenType)
            {
                case JsonTokenType.String:
                    if (type == typeof(bool) && typeToConvert == typeof(OneOf<bool, string>))
                        return false;

                    Assert(type, [typeof(string), typeof(Span)]);
                    Assert(typeToConvert, [typeof(Span), typeof(Inline), typeof(OneOf<string, string[]>), typeof(OneOf<bool, string>)]);

                    return true;

                case JsonTokenType.StartArray:
                    Assert(type, [typeof(string), typeof(string[]), typeof(Span), typeof(Span[])]);
                    Assert(typeToConvert, [typeof(Inline), typeof(OneOf<string, string[]>)]);

                    return type.IsArray;

                case JsonTokenType.StartObject:
                    if (!TryGetFirstPropertyName(ref reader, out var propertyName))
                        return false;

                    var key = (typeToConvert, type, propertyName);

                    if (KnownTypes.Contains(key))
                        return true;

                    if (KnownTypesToSkip.Contains(key))
                        return false;

                    // Unknown type/name combinations found.
                    // Fallback to default behavior.
                    return true;

                default:
                    return true;
            }
        }

        private static bool TryGetFirstPropertyName(ref Utf8JsonReader reader, [NotNullWhen(true)] out string? propertyName)
        {
            Contract.Assert(reader.TokenType == JsonTokenType.StartObject);

            var readerCopy = reader;
            if (readerCopy.Read() && readerCopy.TokenType == JsonTokenType.PropertyName)
            {
                propertyName = readerCopy.GetString()!;
                return true;
            }

            propertyName = null;
            return false;
        }

        [Conditional("DEBUG")]
        private static void Assert(
            Type type,
            Type[] expectedTypes,
            [CallerArgumentExpression(nameof(expectedTypes))] string? message = null)
        {
            if (!expectedTypes.Contains(type))
                throw new InvalidOperationException($"{type.Name} is not expected. Expected: {message}");
        }

        /// <summary>
        /// Known type/name combinations that can be deserialize.
        /// </summary>
        private static readonly HashSet<(Type, Type, string)> KnownTypes =
        [
            // Block : OneOfBase<Heading, Api, Markdown, Facts, Parameters, List, Inheritance, Code>
            (typeof(Block),   typeof(Heading),     "h1"),
            (typeof(Block),   typeof(Heading),     "h2"),
            (typeof(Block),   typeof(Heading),     "h3"),
            (typeof(Block),   typeof(Heading),     "h4"),
            (typeof(Block),   typeof(Heading),     "h5"),
            (typeof(Block),   typeof(Heading),     "h6"),
            (typeof(Block),   typeof(Api),         "api1"),
            (typeof(Block),   typeof(Api),         "api2"),
            (typeof(Block),   typeof(Api),         "api3"),
            (typeof(Block),   typeof(Api),         "api4"),
            (typeof(Block),   typeof(Markdown),    "markdown"),
            (typeof(Block),   typeof(Facts),       "facts"),
            (typeof(Block),   typeof(Parameters),  "parameters"),
            (typeof(Block),   typeof(List),        "list"),
            (typeof(Block),   typeof(Inheritance), "inheritance"),
            (typeof(Block),   typeof(Code),        "code"),

            // Heading : OneOfBase<H1, H2, H3, H4, H5, H6>
            (typeof(Heading), typeof(H1),          "h1"),
            (typeof(Heading), typeof(H2),          "h2"),
            (typeof(Heading), typeof(H3),          "h3"),
            (typeof(Heading), typeof(H4),          "h4"),
            (typeof(Heading), typeof(H5),          "h5"),
            (typeof(Heading), typeof(H6),          "h6"),

            // Api : OneOfBase<Api1, Api2, Api3, Api4>
            (typeof(Api),     typeof(Api1),        "api1"),
            (typeof(Api),     typeof(Api2),        "api2"),
            (typeof(Api),     typeof(Api3),        "api3"),
            (typeof(Api),     typeof(Api4),        "api4"),

            // Span : OneOfBase<string, LinkSpan>
            (typeof(Span),    typeof(LinkSpan),    "text"),

            // Inline : OneOfBase<Span, Span[]>
            (typeof(Inline),  typeof(Span),        "text"),
        ];

        /// <summary>
        /// Known type/name combinations that can not be deserialize.
        /// </summary>
        private static readonly HashSet<(Type, Type, string)> KnownTypesToSkip =
        [
            // Block : OneOfBase<Heading, Api, Markdown, Facts, Parameters, List, Inheritance, Code>
            (typeof(Heading), typeof(H1),          "h2"),
            (typeof(Heading), typeof(H1),          "h3"),
            (typeof(Heading), typeof(H2),          "h3"),
            (typeof(Heading), typeof(H1),          "h4"),
            (typeof(Heading), typeof(H2),          "h4"),
            (typeof(Heading), typeof(H3),          "h4"),
            (typeof(Block),   typeof(Heading),     "api1"),
            (typeof(Block),   typeof(Heading),     "api2"),
            (typeof(Block),   typeof(Heading),     "api3"),
            (typeof(Block),   typeof(Heading),     "api4"),
            (typeof(Block),   typeof(Heading),     "markdown"),
            (typeof(Block),   typeof(Api),         "markdown"),
            (typeof(Block),   typeof(Heading),     "facts"),
            (typeof(Block),   typeof(Api),         "facts"),
            (typeof(Block),   typeof(Markdown),    "facts"),
            (typeof(Block),   typeof(Heading),     "parameters"),
            (typeof(Block),   typeof(Api),         "parameters"),
            (typeof(Block),   typeof(Markdown),    "parameters"),
            (typeof(Block),   typeof(Facts),       "parameters"),
            (typeof(Block),   typeof(Heading),     "list"),
            (typeof(Block),   typeof(Api),         "list"),
            (typeof(Block),   typeof(Markdown),    "list"),
            (typeof(Block),   typeof(Facts),       "list"),
            (typeof(Block),   typeof(Parameters),  "list"),
            (typeof(Block),   typeof(Heading),     "inheritance"),
            (typeof(Block),   typeof(Api),         "inheritance"),
            (typeof(Block),   typeof(Markdown),    "inheritance"),
            (typeof(Block),   typeof(Facts),       "inheritance"),
            (typeof(Block),   typeof(Parameters),  "inheritance"),
            (typeof(Block),   typeof(List),        "inheritance"),
            (typeof(Block),   typeof(Heading),     "code"),
            (typeof(Block),   typeof(Api),         "code"),
            (typeof(Block),   typeof(Markdown),    "code"),
            (typeof(Block),   typeof(Facts),       "code"),
            (typeof(Block),   typeof(Parameters),  "code"),
            (typeof(Block),   typeof(List),        "code"),
            (typeof(Block),   typeof(Inheritance), "code"),

            // OneOfBase<Api1, Api2, Api3, Api4>
            (typeof(Api),     typeof(Api1),       "api2"),
            (typeof(Api),     typeof(Api1),       "api3"),
            (typeof(Api),     typeof(Api2),       "api3"),
            (typeof(Api),     typeof(Api1),       "api4"),
            (typeof(Api),     typeof(Api2),       "api4"),
            (typeof(Api),     typeof(Api3),       "api4"),

            //  OneOfBase<string, LinkSpan>
            (typeof(Span),    typeof(String),     "text"),
        ];
    }
}
