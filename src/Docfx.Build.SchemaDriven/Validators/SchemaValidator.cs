// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Docfx.Common;
using Json.Schema;

namespace Docfx.Build.SchemaDriven;

public class SchemaValidator
{
    private readonly JsonSchema _schema;

    private static readonly EvaluationOptions DefaultOptions = new()
    {
        IncludeApplicatorErrors = false,
        OutputFormat = OutputFormat.List,
    };

    static SchemaValidator()
    {
        RegisterDialect(new("http://dotnet.github.io/docfx/schemas/v1.0/schema.json#"));
        RegisterDialect(new("https://dotnet.github.io/docfx/schemas/v1.0/schema.json#"));

        static void RegisterDialect(Uri id)
        {
            SchemaRegistry.Global.Register(id, MetaSchemas.Draft7);
            DialectRegistry.Global.Register(Dialect.Draft07.With([], id));
        }
    }

    public SchemaValidator(string json)
    {
        _schema = JsonSchema.FromText(json, jsonOptions: new() { AllowTrailingCommas = true });
    }

    public void Validate(object obj)
    {
        var json = JsonSerializer.Serialize(obj);
        using var document = JsonDocument.Parse(json);
        var result = _schema.Evaluate(document.RootElement, DefaultOptions);

        if (result.IsValid)
            return;

        foreach (var detail in result.Details)
        {
            if (detail.Errors is { } errors)
            {
                foreach (var (type, message) in errors)
                {
                    Logger.LogError($"[{detail.InstanceLocation}] {type}: {message} ", code: "ViolateSchema");
                }
            }
        }
    }
}
