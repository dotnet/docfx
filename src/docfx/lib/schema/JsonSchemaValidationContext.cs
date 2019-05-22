// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Docs.Build
{
    internal class JsonSchemaValidationContext
    {
        public JsonSchema Root { get; private set; }

        public JsonSchemaValidationContext(JsonSchema root)
        {
            Debug.Assert(root != null);

            Root = root;
        }

        public JsonSchema GetDefinition(JsonSchema jsonSchema, HashSet<string> recursion = null, string key = "root")
        {
            recursion = recursion ?? new HashSet<string>();

            if (recursion.Add(key) && jsonSchema != null && !jsonSchema.Properties.TryGetValue("$ref", out _) && !string.IsNullOrEmpty(jsonSchema.Ref))
            {
                var (subKey, sub) = GetDefinitionCore(jsonSchema.Ref);
                return GetDefinition(sub, recursion, subKey ?? "root");
            }

            return jsonSchema;
        }

        private (string key, JsonSchema schema) GetDefinitionCore(string @ref)
        {
            Debug.Assert(!string.IsNullOrEmpty(@ref));

            var (path, _, fragment) = UrlUtility.SplitUrl(@ref);

            Debug.Assert(!string.IsNullOrEmpty(fragment));

            if (!string.IsNullOrEmpty(path))
            {
                // not supported
                throw new NotSupportedException("Schema definition file is not supported");
            }

            string jsonPointer = fragment.TrimStart(new char[] { '#', '/' });
            if (string.IsNullOrWhiteSpace(jsonPointer))
            {
                // root pointer ref
                return (null, Root);
            }

            if (!jsonPointer.StartsWith("definitions", StringComparison.OrdinalIgnoreCase))
            {
                // not supported
                throw new NotSupportedException("Schema id is not supported");
            }

            var parts = jsonPointer.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2)
            {
                // not supported
                throw new NotSupportedException("Non-root level schema definition is not supported");
            }

            if (Root.Definitions.TryGetValue(parts[1], out var schema))
            {
                // definition ref
                return (parts[1], schema);
            }

            return default;
        }
    }
}
