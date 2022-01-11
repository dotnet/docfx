// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build;

internal class MetadataValidator
{
    private readonly DocumentProvider _documentProvider;
    private readonly JsonSchemaProvider _jsonSchemaProvider;
    private readonly JsonSchemaTransformer _jsonSchemaTransformer;
    private readonly JsonSchemaValidator[] _schemaValidators;
    private readonly HashSet<string> _reservedMetadata;

    public JsonSchema[] MetadataSchemas { get; }

    public MetadataValidator(
        Config config,
        MicrosoftGraphAccessor microsoftGraphAccessor,
        DocumentProvider documentProvider,
        JsonSchemaLoader jsonSchemaLoader,
        JsonSchemaProvider jsonSchemaProvider,
        JsonSchemaTransformer jsonSchemaTransformer,
        MonikerProvider monikerProvider,
        CustomRuleProvider customRuleProvider)
    {
        _documentProvider = documentProvider;
        _jsonSchemaProvider = jsonSchemaProvider;
        _jsonSchemaTransformer = jsonSchemaTransformer;

        var metadataSchemas = config.MetadataSchema.Select(jsonSchemaLoader.LoadSchema).ToList();

        if (jsonSchemaProvider.TryGetSchema("Metadata") is JsonSchema schema)
        {
            metadataSchemas.Add(schema);
        }

        MetadataSchemas = metadataSchemas.ToArray();

        _schemaValidators = Array.ConvertAll(
            MetadataSchemas,
            schema => new JsonSchemaValidator(schema, microsoftGraphAccessor, monikerProvider, false, customRuleProvider));

        _reservedMetadata = JsonUtility.GetPropertyNames(typeof(SystemMetadata))
            .Concat(JsonUtility.GetPropertyNames(typeof(ConceptualModel)))
            .Concat(MetadataSchemas.SelectMany(schema => schema.Reserved))
            .Except(JsonUtility.GetPropertyNames(typeof(UserMetadata)))
            .ToHashSet();
    }

    public JObject ValidateAndTransformMetadata(ErrorBuilder errors, JObject metadata, FilePath file)
    {
        foreach (var (key, value) in metadata)
        {
            if (value is null)
            {
                continue;
            }
            if (_reservedMetadata.Contains(key))
            {
                errors.Add(Errors.Metadata.AttributeReserved(JsonUtility.GetKeySourceInfo(value), key));
            }
        }

        JToken token = metadata;

        foreach (var schemaValidator in _schemaValidators)
        {
            token = _jsonSchemaTransformer.TransformMetadata(errors, file, token, schemaValidator);
        }

        var mime = _documentProvider.GetMime(file).Value;
        if (mime != null && _jsonSchemaProvider.TryGetSchema(mime) is JsonSchema schema &&
            schema.Properties.TryGetValue("metadata", out var metadataSchema))
        {
            token = _jsonSchemaTransformer.TransformMetadata(errors, file, token, new(metadataSchema));
        }

        return (JObject)token;
    }

    public List<Error> PostValidate()
    {
        var errors = new List<Error>();
        foreach (var validator in _schemaValidators)
        {
            errors.AddRange(validator.PostValidate());
        }

        return errors;
    }
}
