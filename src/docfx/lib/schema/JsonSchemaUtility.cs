// Copyright(c) Microsoft.All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

namespace Microsoft.Docs.Build
{
    internal static class JsonSchemaUtility
    {
        public static JsonSchema GetDefinition(string @ref, JsonSchema root)
        {
            Debug.Assert(!string.IsNullOrEmpty(@ref));

            var (path, _, fragment) = UrlUtility.SplitUrl(@ref);

            Debug.Assert(!string.IsNullOrEmpty(fragment));

            if (!string.IsNullOrEmpty(path))
            {
                // not supported
                throw new NotSupportedException("Schema definition file is not supported");
            }

            string jsonPointer = fragment.TrimStart('#');
            if (string.IsNullOrWhiteSpace(jsonPointer))
            {
                return root;
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

            if (root.Definitions.TryGetValue(parts[1], out var schema))
            {
                return schema;
            }

            return null;
        }
    }
}
