// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Json.Schema;

namespace Docfx.Tests;

internal static class JsonSchemaUtility
{
    public static readonly EvaluationOptions DefaultEvaluationOptions = new()
    {
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

        var schema = JsonSchema.FromText(File.ReadAllText(jsonSchemaPath), jsonOptions: DefaultJsonDocumentOptions);

        var result = schema.Evaluate(jsonElement, DefaultEvaluationOptions);
        return result;
    }
}
