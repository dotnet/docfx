// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Json.Schema;

namespace Docfx.Tests;

internal static class JsonSchemaUtility
{
    public static readonly JsonSerializerOptions DefaultSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    public static readonly EvaluationOptions DefaultEvaluationOptions = new()
    {
        ValidateAgainstMetaSchema = false,
        OutputFormat = OutputFormat.List,
    };

    public static readonly JsonDocumentOptions DefaultJsonDocumentOptions = new()
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip,
    };

    public static EvaluationResults ValidateJsonSchema(JsonElement jsonElement, string schemaPath)
    {
        var solutionDir = PathHelper.GetSolutionFolder();
        var jsonSchemaPath = Path.Combine(solutionDir, schemaPath);

        if (!File.Exists(jsonSchemaPath))
            throw new FileNotFoundException(jsonSchemaPath);

        var schema = JsonSchema.FromFile(jsonSchemaPath, DefaultSerializerOptions);

        var result = schema.Evaluate(jsonElement, DefaultEvaluationOptions);
        return result;
    }
}
