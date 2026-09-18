// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Docfx.Common;
using Json.Schema;

#nullable enable

namespace Docfx.Build.SchemaDriven;

public class SchemaValidator
{
    private readonly JsonSchema _schema;

    private static readonly EvaluationOptions DefaultEvaluationOptions = new()
    {
        OutputFormat = OutputFormat.List,
    };

    private static readonly JsonDocumentOptions DefaultJsonDocumentOptions = new()
    {
        AllowTrailingCommas = true,
    };

    static SchemaValidator()
    {
        Uri[] uris =
        [
            new Uri("http://dotnet.github.io/docfx/schemas/v1.0/schema.json#"),
            new Uri("https://dotnet.github.io/docfx/schemas/v1.0/schema.json#"),
        ];

        foreach (var uri in uris)
        {
            SchemaRegistry.Global.Register(uri, MetaSchemas.Draft7);
            DialectRegistry.Global.Register(Dialect.Draft07.With([], id: uri));
        }
    }

    public SchemaValidator(string json)
    {
        var builldOptions = new BuildOptions()
        {
            // Create SchemaRegistry instance to avoid exception.
            // https://github.com/json-everything/json-everything/issues/957
            SchemaRegistry = new SchemaRegistry(),
            Dialect = Dialect.Draft07,
        };

        _schema = JsonSchema.FromText(json, builldOptions, jsonOptions: DefaultJsonDocumentOptions);
    }

    public void Validate(object obj)
    {
        var json = JsonSerializer.Serialize(obj);
        var result = _schema.Evaluate(JsonDocument.Parse(json, DefaultJsonDocumentOptions).RootElement, DefaultEvaluationOptions);

        if (result.IsValid)
            return;

        foreach (var detail in result.Details ?? [])
        {
            if (detail.Errors != null)
            {
                foreach (var (type, message) in detail.Errors)
                {
                    Logger.LogError($"[{detail.InstanceLocation}] {type}: {message} ", code: "ViolateSchema");
                }
            }
        }
    }
}
