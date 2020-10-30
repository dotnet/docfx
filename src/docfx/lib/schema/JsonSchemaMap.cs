// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class JsonSchemaMap
    {
        private readonly Predicate<JsonSchema> _predicate;
        private readonly Dictionary<JToken, JsonSchema> _map = new Dictionary<JToken, JsonSchema>(ReferenceEqualsComparer.Default);

        public JsonSchemaMap(Predicate<JsonSchema> predicate) => _predicate = predicate;

        public JsonSchemaMap(JsonSchemaMap map) => _predicate = map._predicate;

        public bool TryGetSchema(JToken token, [MaybeNullWhen(false)] out JsonSchema schema) => _map.TryGetValue(token, out schema);

        public void Add(JToken token, JsonSchema schema)
        {
            if (_predicate(schema))
            {
                _map.TryAdd(token, schema);
            }
        }

        public void Add(JsonSchemaMap map)
        {
            foreach (var (token, schema) in map._map)
            {
                _map.TryAdd(token, schema);
            }
        }
    }
}
