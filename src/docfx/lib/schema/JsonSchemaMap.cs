// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
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
        for (var i = 0; i < array.Count; i++)
        {
            var item = array[i];

            JsonSchema? subschema = null;

            if (schema != null)
            {
                var (items, eachItem) = schema.Items;
                if (items != null)
                {
                    subschema = items;
                }
                else if (eachItem != null)
                {
                    if (i < eachItem.Length)
                    {
                        subschema = eachItem[i];
                    }
                    else if (schema.AdditionalItems != null)
                    {
                        subschema = schema.AdditionalItems;
                    }
                }
            }

            yield return (item, schema?.SchemaResolver.ResolveSchema(subschema) ?? _map.GetValueOrDefault(item));
        }
    }

    public JsonSchema? GetPropertySchema(JsonSchema? schema, JObject obj, string key)
    {
        if (schema is null)
        {
            return null;
        }

        var subschema = GetPropertySchemaCore();
        if (subschema != null)
        {
            return schema.SchemaResolver.ResolveSchema(subschema);
        }

        return _map.GetValueOrDefault(obj[key]!);

        JsonSchema? GetPropertySchemaCore()
        {
            // properties
            if (schema.Properties.TryGetValue(key, out var result))
            {
                return result;
            }

            // patternProperties
            foreach (var (pattern, patternPropertySchema) in schema.PatternProperties)
            {
                if (Regex.IsMatch(key, pattern))
                {
                    return patternPropertySchema;
                }
            }

            // additionalProperties
            return schema.AdditionalProperties;
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
