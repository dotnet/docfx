// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal class JsonSchemaValidationContext
    {
        private readonly JsonSchema _root;
        private readonly Dictionary<string, JsonSchema> _definitions;

        public JsonSchemaValidationContext(JsonSchema root)
        {
            Debug.Assert(root != null);

            _root = root;
            _definitions = _root.Definitions.ToDictionary(k => $"#/definitions/{k.Key}", v => v.Value);
            _definitions.Add("#", root);
        }

        public JsonSchema GetDefinition(JsonSchema jsonSchema, HashSet<string> recursions = null, string key = "#")
        {
            recursions = recursions ?? new HashSet<string>();

            if (recursions.Add(key) && jsonSchema != null && !jsonSchema.Properties.TryGetValue("$ref", out _) && !string.IsNullOrEmpty(jsonSchema.Ref))
            {
                var (subKey, sub) = GetDefinitionCore(jsonSchema.Ref);
                return GetDefinition(sub, recursions, subKey);
            }

            return jsonSchema;
        }

        private (string key, JsonSchema schema) GetDefinitionCore(SourceInfo<string> @ref)
        {
            Debug.Assert(!string.IsNullOrEmpty(@ref));

            return _definitions.TryGetValue(@ref, out var schema)
                ? (@ref, schema)
                : throw Errors.SchemaDefinitionNotFound(@ref).ToException();
        }
    }
}
