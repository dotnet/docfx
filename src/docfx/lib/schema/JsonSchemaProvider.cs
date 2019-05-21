// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.IO;

namespace Microsoft.Docs.Build
{
    internal class JsonSchemaProvider
    {
        private ConcurrentDictionary<string, JsonSchema> _jsonSchemas = new ConcurrentDictionary<string, JsonSchema>();

        // TODO: get schema from template
        public JsonSchema GetJsonSchema(Schema schema)
        {
            if (schema == null)
            {
                return null;
            }

            var schemaFilePath = $"data/{schema.Type.Name}.json";
            return _jsonSchemas.GetOrAdd(
                schema.Type.Name,
                File.Exists(schemaFilePath) ? JsonUtility.Deserialize<JsonSchema>(File.ReadAllText(schemaFilePath), schemaFilePath) : null);
        }
    }
}
