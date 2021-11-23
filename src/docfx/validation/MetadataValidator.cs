// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build;

internal class MetadataValidator
{
    private readonly JsonSchemaValidator[] _schemaValidators;
    private readonly HashSet<string> _reservedMetadata;

    public JsonSchema[] MetadataSchemas { get; }

    public MetadataValidator(
        Config config,
        MicrosoftGraphAccessor microsoftGraphAccessor,
        JsonSchemaLoader jsonSchemaLoader,
        MonikerProvider monikerProvider,
        CustomRuleProvider customRuleProvider)
    {
        MetadataSchemas = Array.ConvertAll(config.MetadataSchema, jsonSchemaLoader.LoadSchema);

        _schemaValidators = Array.ConvertAll(
            MetadataSchemas,
            schema => new JsonSchemaValidator(schema, microsoftGraphAccessor, monikerProvider, false, customRuleProvider));

        _reservedMetadata = JsonUtility.GetPropertyNames(typeof(SystemMetadata))
            .Concat(JsonUtility.GetPropertyNames(typeof(ConceptualModel)))
            .Concat(MetadataSchemas.SelectMany(schema => schema.Reserved))
            .Except(JsonUtility.GetPropertyNames(typeof(UserMetadata)))
            .ToHashSet();
    }

    public void ValidateMetadata(ErrorBuilder errors, JObject metadata, FilePath file)
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

        foreach (var schemaValidator in _schemaValidators)
        {
            errors.AddRange(schemaValidator.Validate(metadata, file));
        }
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
