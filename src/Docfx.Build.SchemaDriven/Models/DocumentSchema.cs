// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;
using Docfx.Common;
using Docfx.Exceptions;
using Json.Schema;

namespace Docfx.Build.SchemaDriven;

public class DocumentSchema : BaseSchema
{
    // JsonSerializerOptions should be reused.
    // https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/configure-options#reuse-jsonserializeroptions-instances
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        AllowTrailingCommas = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = {
                        new JsonStringEnumConverter()
                     },
    };

    public string Metadata { get; set; }

    public JsonPointer MetadataReference { get; private set; }

    public SchemaValidator Validator { get; private set; }

    /// <summary>
    /// Overwrites are only allowed when the schema contains "uid" definition
    /// </summary>
    public bool AllowOverwrite { get; private set; }

    public static DocumentSchema Load(string content, string title)
    {
        DocumentSchema schema;

        try
        {
            schema = JsonSerializer.Deserialize<DocumentSchema>(
                content,
                SerializerOptions);
        }
        catch (Exception e)
        {
            var message = $"{title} is not a valid schema: {e.Message}";
            Logger.LogError(message, code: ErrorCodes.Build.ViolateSchema);
            throw new InvalidSchemaException(message, e);
        }

        if (string.IsNullOrWhiteSpace(schema.Title))
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                var message = "Title of schema must be specified.";
                Logger.LogError(message, code: ErrorCodes.Build.ViolateSchema);
                throw new InvalidSchemaException(message);
            }
            schema.Title = title;
        }

        if (schema.Type != SchemaValueType.Object)
        {
            var message = "Type for the root schema object must be object";
            Logger.LogError(message, code: ErrorCodes.Build.ViolateSchema);
            throw new InvalidSchemaException(message);
        }

        if (!JsonPointer.TryCreate(schema.Metadata, out var pointer))
        {
            var message = $"Metadata's json pointer {schema.Metadata} is invalid.";
            Logger.LogError(message, code: ErrorCodes.Build.ViolateSchema);
            throw new InvalidSchemaException(message);
        }

        var metadataSchema = pointer.FindSchema(schema);
        if (metadataSchema != null && metadataSchema.Type != SchemaValueType.Object)
        {
            throw new InvalidJsonPointerException($"The referenced object is in type: {metadataSchema.Type}, only object can be a metadata reference");
        }

        ResolveRef(schema);

        schema.MetadataReference = pointer;
        schema.AllowOverwrite = CheckOverwriteAbility(schema);

        schema.Validator = new(content);
        return schema;
    }

    private static void ResolveRef(DocumentSchema root)
    {
        if (root.Definitions is null)
            return;

        ResolveRefCore(root);

        BaseSchema ResolveRefCore(BaseSchema schema)
        {
            if (schema is null)
                return schema;

            if (!string.IsNullOrEmpty(schema.Ref))
            {
                if (!schema.Ref.StartsWith("#/definitions/"))
                {
                    Logger.LogError($"JSON schema $ref {schema.Ref} must start with #/definitions/", code: ErrorCodes.Build.ViolateSchema);
                }
                else if (!root.Definitions.TryGetValue(schema.Ref.Substring("#/definitions/".Length), out var definition))
                {
                    Logger.LogError($"Cannot resolve JSON schema $ref: {schema.Ref}", code: ErrorCodes.Build.ViolateSchema);
                }
                else
                {
                    return definition;
                }
            }

            schema.Items = ResolveRefCore(schema.Items);

            if (schema.Properties != null)
            {
                foreach (var (key, value) in schema.Properties)
                {
                    schema.Properties[key] = ResolveRefCore(value);
                }
            }

            if (schema.Definitions != null)
            {
                foreach (var (key, value) in schema.Definitions)
                {
                    schema.Definitions[key] = ResolveRefCore(value);
                }
            }

            return schema;
        }
    }

    private static bool CheckOverwriteAbility(BaseSchema schema)
    {
        return CheckOverwriteAbilityCore(schema, []);
    }

    private static bool CheckOverwriteAbilityCore(BaseSchema schema, Dictionary<BaseSchema, bool> cache)
    {
        if (schema == null)
        {
            return false;
        }

        if (cache.TryGetValue(schema, out var result))
        {
            return result;
        }

        if (schema.ContentType == ContentType.Uid)
        {
            cache[schema] = result = true;
            return result;
        }

        cache[schema] = result = false;

        if (CheckOverwriteAbilityCore(schema.Items, cache))
        {
            cache[schema] = result = true;
            return result;
        }

        if (schema.Properties != null)
        {
            foreach (var value in schema.Properties.Values)
            {
                if (CheckOverwriteAbilityCore(value, cache))
                {
                    cache[schema] = result = true;
                    return result;
                }
            }
        }

        return result;
    }
}
