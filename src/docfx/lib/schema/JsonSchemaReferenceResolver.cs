// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Microsoft.Docs.Build
{
    internal class JsonSchemaReferenceResolver
    {
        private readonly Uri _baseUrl;
        private readonly Dictionary<string, JsonSchema> _definitions;
        private readonly ConcurrentDictionary<JsonSchema, JsonSchema?> _references
                   = new ConcurrentDictionary<JsonSchema, JsonSchema?>(ReferenceEqualsComparer.Default);

        public static readonly JsonSchemaReferenceResolver Null = new JsonSchemaReferenceResolver(new Uri("http://me"), new Dictionary<string, JsonSchema>());

        public JsonSchemaReferenceResolver(Uri baseUrl, Dictionary<string, JsonSchema> definitions)
        {
            _baseUrl = baseUrl;
            _definitions = definitions;
        }

        public JsonSchema ResolveSchema(JsonSchema schema)
        {
            return _references.GetOrAdd(schema, schema => ResolveSchemaCore(schema, new HashSet<string>())) ?? schema;
        }

        private JsonSchema? ResolveSchemaCore(JsonSchema schema, HashSet<string> recursions)
        {
            if (string.IsNullOrEmpty(schema.Ref))
            {
                return null;
            }

            // https://tools.ietf.org/html/rfc6901
            // This is performed by first transforming any
            // occurrence of the sequence '~1' to '/', and then transforming any
            // occurrence of the sequence '~0' to '~'.
            var url = new Uri(_baseUrl, schema.Ref).ToString().Replace("~1", "/").Replace("~0", "~").TrimEnd('/', '#');
            if (!recursions.Add(url))
            {
                return JsonSchema.FalseSchema;
            }

            if (_definitions.TryGetValue(url, out var resolvedSchema))
            {
                return ResolveSchemaCore(resolvedSchema, recursions) ?? resolvedSchema;
            }

            return null;
        }
    }
}
