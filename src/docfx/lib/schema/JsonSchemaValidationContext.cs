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

        public JsonSchema GetDefinition(JsonSchema jsonSchema)
            => GetDefinitionCore(jsonSchema);

        private JsonSchema GetDefinitionCore(JsonSchema jsonSchema, HashSet<string> recursions = null)
        {
            recursions = recursions ?? new HashSet<string>();

            if (jsonSchema != null &&
                !jsonSchema.Properties.TryGetValue("$ref", out _) &&
                !string.IsNullOrEmpty(jsonSchema.Ref) &&
                recursions.Add(jsonSchema.Ref))
            {
                if (_definitions.TryGetValue(jsonSchema.Ref, out var schema))
                {
                    return GetDefinitionCore(schema, recursions);
                }

                throw new ApplicationException($"Could not found `{jsonSchema.Ref}` schema definition");
            }

            return jsonSchema;
        }
    }
}
