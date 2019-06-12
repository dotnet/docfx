// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal class JsonSchemaDefinition
    {
        private readonly JsonSchema _root;
        private readonly Dictionary<string, JsonSchema> _definitions;

        public JsonSchemaDefinition(JsonSchema root)
        {
            Debug.Assert(root != null);

            _root = root;
            _definitions = _root.Definitions.ToDictionary(k => $"#/definitions/{k.Key}", v => v.Value);
            _definitions.Add("#", root);
        }

        public JsonSchema GetDefinition(JsonSchema schema)
            => GetDefinitionCore(schema, new HashSet<string>());

        private JsonSchema GetDefinitionCore(JsonSchema schema, HashSet<string> recursions)
        {
            if (schema != null &&
                !string.IsNullOrEmpty(schema.Ref) &&
                recursions.Add(schema.Ref))
            {
                if (_definitions.TryGetValue(schema.Ref, out var resolvedSchema))
                {
                    return GetDefinitionCore(resolvedSchema, recursions);
                }

                throw new ApplicationException($"Could not find `{schema.Ref}` schema definition");
            }

            return schema;
        }
    }
}
