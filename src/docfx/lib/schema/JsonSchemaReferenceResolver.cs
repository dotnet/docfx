// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Docs.Build
{
    internal class JsonSchemaReferenceResolver
    {
        private readonly Dictionary<string, JsonSchema> _definitions;

        public static readonly JsonSchemaReferenceResolver Null = new JsonSchemaReferenceResolver(new Dictionary<string, JsonSchema>());

        public JsonSchemaReferenceResolver(Dictionary<string, JsonSchema> definitions)
        {
            _definitions = definitions;
        }

        public JsonSchema ResolveSchema(JsonSchema schema)
            => ResolveSchemaCore(schema, new HashSet<string>());

        private JsonSchema ResolveSchemaCore(JsonSchema schema, HashSet<string> recursions)
        {
            if (!string.IsNullOrEmpty(schema.Ref) && recursions.Add(schema.Ref))
            {
                if (_definitions.TryGetValue(schema.Ref, out var resolvedSchema))
                {
                    return ResolveSchemaCore(resolvedSchema, recursions);
                }

                throw new InvalidOperationException($"Could not find `{schema.Ref}` schema definition");
            }

            return schema;
        }
    }
}
