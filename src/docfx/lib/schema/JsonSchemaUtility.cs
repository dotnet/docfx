// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal static class JsonSchemaUtility
    {
        public static JsonSchema GetRefDefinition(JsonSchema root, string @ref)
        {
            Debug.Assert(string.IsNullOrEmpty(@ref));

            // todo: better support json schema ref
            var (_, _, fragment) = UrlUtility.SplitUrl(@ref);
            var schemaName = fragment.Split("/").LastOrDefault();
            if (string.IsNullOrEmpty(schemaName))
            {
                return root;
            }
            if (!string.IsNullOrEmpty(schemaName) && root.Definitions.TryGetValue(schemaName, out var schemaDefinition))
            {
                return schemaDefinition;
            }

            throw new ApplicationException($"{schemaName} schema definition is not found");
        }
    }
}
