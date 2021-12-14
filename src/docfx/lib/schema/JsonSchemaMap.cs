// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build;

/// <summary>
/// Enables traversal of JToken against a JsonSchema, according to dynamic logical JsonSchema dispatch
/// behaviors specified by if-then-else, oneOf, anyOf, not etc.
/// </summary>
internal class JsonSchemaMap
{
    private readonly Func<JsonSchema, bool> _predicate;
    private readonly Dictionary<JToken, JsonSchema> _map = new(ReferenceEqualsComparer.Default);

    public JsonSchemaMap(Func<JsonSchema, bool> predicate) => _predicate = predicate;

    public JsonSchemaMap(JsonSchemaMap map) => _predicate = map._predicate;

    public IEnumerable<(JToken item, JsonSchema? subschema)> ForEachJArray(JsonSchema? schema, JArray array)
    {
        foreach (var item in array)
        {
            var subschema = schema?.Items.schema ?? _map.GetValueOrDefault(item);
            yield return (item, subschema);
        }
    }

    public IEnumerable<(string key, JToken value, JsonSchema? subschema)> ForEachJObject(JsonSchema? schema, JObject obj)
    {
        foreach (var (key, value) in obj)
        {
            if (value != null)
            {
                var subschema = schema != null && schema.Properties.TryGetValue(key, out var result) ? result : _map.GetValueOrDefault(value);
                yield return (key, value, subschema);
            }
        }
    }

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
