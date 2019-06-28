// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;

namespace Microsoft.Docs.Build
{
    internal class TemplateSchema
    {
        private string _htmlTransformJsPath;
        private string _metadataTransformJsPath;
        private Lazy<bool> _isData;
        private Lazy<bool> _transformMetadata;
        private Lazy<(JsonSchemaValidator JsonSchemaValidator, JsonSchemaTransformer JsonSchemaTransformer)> _jsonSchema;

        public string SchemaName { get; }

        public bool IsData => _isData.Value;

        public bool TransformMetadata => _transformMetadata.Value;

        public JsonSchemaValidator JsonSchemaValidator => _jsonSchema.Value.JsonSchemaValidator;

        public JsonSchemaTransformer JsonSchemaTransformer => _jsonSchema.Value.JsonSchemaTransformer;

        public TemplateSchema(string schemaName, string schemaDir, string contentTemplateDir)
        {
            Debug.Assert(!string.IsNullOrEmpty(schemaName));

            SchemaName = SchemaName;
            _isData = new Lazy<bool>(() => GetIsDataCore(schemaName, contentTemplateDir));
            _transformMetadata = new Lazy<bool>(() => File.Exists(_metadataTransformJsPath = Path.Combine(contentTemplateDir, $"{schemaName}.mta.json.js")));
            _jsonSchema = new Lazy<(JsonSchemaValidator, JsonSchemaTransformer)>(GetJsonSchemaCore(schemaDir, schemaName));
        }

        private bool GetIsDataCore(string schemaName, string contentTemplateDir)
        {
            if (string.Equals(schemaName, "LandingData"))
                return false;
            return !File.Exists(_htmlTransformJsPath = Path.Combine(contentTemplateDir, $"{schemaName}.html.primary.js"));
        }

        private (JsonSchemaValidator, JsonSchemaTransformer) GetJsonSchemaCore(string schemaDir, string schemaName)
        {
            if (schemaName is null)
            {
                return default;
            }

            var schemaFilePath = Path.Combine(schemaDir, $"{schemaName}.schema.json");
            if (string.Equals(schemaName, "LandingData", StringComparison.OrdinalIgnoreCase))
            {
                schemaFilePath = Path.Combine(AppContext.BaseDirectory, "data", "schemas", "LandingData.json");
            }
            if (!File.Exists(schemaFilePath))
            {
                return default;
            }

            var jsonSchema = JsonUtility.Deserialize<JsonSchema>(File.ReadAllText(schemaFilePath), schemaFilePath);
            return (new JsonSchemaValidator(jsonSchema), new JsonSchemaTransformer(jsonSchema));
        }
    }
}
