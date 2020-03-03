// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;

#nullable enable

namespace Microsoft.Docs.Build
{
    internal class TemplateSchema
    {
        public string SchemaName { get; }

        public bool IsPage { get; }

        public JsonSchemaValidator JsonSchemaValidator { get; }

        public JsonSchemaTransformer JsonSchemaTransformer { get; }

        public TemplateSchema(string schemaName, string schemaDir, string contentTemplateDir)
        {
            Debug.Assert(!string.IsNullOrEmpty(schemaName));

            SchemaName = schemaName;
            IsPage = GetIsPageCore(schemaName, contentTemplateDir);
            (JsonSchemaValidator, JsonSchemaTransformer) = GetJsonSchemaCore(schemaDir, schemaName);
        }

        private bool GetIsPageCore(string schemaName, string contentTemplateDir)
        {
            if (string.Equals(schemaName, "LandingData"))
                return true;

            return File.Exists(Path.Combine(contentTemplateDir, $"{schemaName}.html.primary.tmpl"))
                || File.Exists(Path.Combine(contentTemplateDir, $"{schemaName}.html.primary.js"));
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
                schemaFilePath = Path.Combine(AppContext.BaseDirectory, "data/schemas/LandingData.json");
            }
            if (!File.Exists(schemaFilePath))
            {
                return default;
            }

            var jsonSchema = JsonUtility.Deserialize<JsonSchema>(File.ReadAllText(schemaFilePath), new FilePath(schemaFilePath));
            return (new JsonSchemaValidator(jsonSchema, forceError: true), new JsonSchemaTransformer(jsonSchema));
        }
    }
}
