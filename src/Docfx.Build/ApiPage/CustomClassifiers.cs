// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#if NET11_0_OR_GREATER
using System.Text.Json;
using System.Text.Json.Serialization;

#nullable enable

namespace Docfx.Build.ApiPage;


/// <summary>
/// Custom classifier for inline types.
/// It's required because built-in JsonUnionTypeStructuralClassifier don't support nested union type.
/// </summary>
internal sealed class InlineClassifier : JsonTypeClassifierFactory<Inline>
{
    public override JsonTypeClassifier CreateJsonClassifier(JsonTypeClassifierContext context, JsonSerializerOptions options)
    {
        return static (ref Utf8JsonReader reader) => reader.TokenType switch
        {
            JsonTokenType.String => typeof(Span),
            JsonTokenType.StartObject => typeof(Span),
            JsonTokenType.StartArray => typeof(Span[]),
            _ => null,
        };
    }
}

/// <summary>
/// Custom classifier for block types.
/// It's required because built-in JsonUnionTypeStructuralClassifier don't support nested union type.
/// </summary>
internal sealed class BlockClassifier : JsonTypeClassifierFactory<Block>
{
    public override JsonTypeClassifier CreateJsonClassifier(JsonTypeClassifierContext context, JsonSerializerOptions options)
    {
        return static (ref Utf8JsonReader reader) =>
        {
            if (reader.TokenType is not JsonTokenType.StartObject)
                return null;

            while (reader.Read() && reader.TokenType is not JsonTokenType.EndObject)
            {
                if (reader.TokenType is not JsonTokenType.PropertyName)
                    continue;

                Type? type = reader.GetString() switch
                {
                    "h1" or "h2" or "h3" or "h4" or "h5" or "h6" => typeof(Heading),
                    "api1" or "api2" or "api3" or "api4" => typeof(Api),
                    "markdown" => typeof(Markdown),
                    "facts" => typeof(Facts),
                    "parameters" => typeof(Parameters),
                    "list" => typeof(List),
                    "inheritance" => typeof(Inheritance),
                    "code" => typeof(Code),
                    _ => null,
                };

                if (type is not null)
                    return type;

                reader.Read();
                reader.Skip();
            }

            return null;
        };
    }
}
#endif
