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
    private readonly Dictionary<JToken, JsonSchema> _map = new(ReferenceEqualsComparer.Default);

    public IEnumerable<(JToken item, JsonSchema? subschema)> ForEachJArray(JsonSchema? schema, JArray array)
    {
        for (var i = 0; i < array.Count; i++)
        {
            var item = array[i];
            var subschema = schema?.SchemaResolver.ResolveSchema(GetItemSchemaCore(item, i));
            yield return (item, subschema);
        }

        JsonSchema? GetItemSchemaCore(JToken item, int i)
        {
            if (_map.TryGetValue(item, out var subschema))
            {
                return subschema;
            }

            var (allItems, eachItem) = schema.Items;
            if (allItems != null)
            {
                return allItems;
            }
            else if (eachItem != null)
            {
                if (i < eachItem.Length)
                {
                    return eachItem[i];
                }
                else if (schema.AdditionalItems != null)
                {
                    return schema.AdditionalItems;
                }
            }

            return null;
        }
    }

    public JsonSchema? GetPropertySchema(JsonSchema? schema, JObject obj, string key)
    {
        if (schema is null)
        {
            return null;
        }

        return schema.SchemaResolver.ResolveSchema(GetPropertySchemaCore());

        JsonSchema? GetPropertySchemaCore()
        {
            var value = obj[key];
            if (value != null && _map.TryGetValue(value, out var result))
            {
                return result;
            }

            if (schema.Properties.TryGetValue(key, out result))
            {
                return result;
            }

            foreach (var (pattern, patternPropertySchema) in schema.PatternProperties)
            {
                if (Regex.IsMatch(key, pattern))
                {
                    return patternPropertySchema;
                }
            }

            return schema.AdditionalProperties;
        }
    }

    public void Add(JToken token, JsonSchema schema)
    {
        _map.TryAdd(token, schema);
    }

    public void Add(JsonSchemaMap map)
    {
        foreach (var (token, schema) in map._map)
        {
            _map.TryAdd(token, schema);
        }
    }
}
