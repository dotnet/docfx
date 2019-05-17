// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal static class TravelJsonSchema
    {
        public static void Travel(JsonSchema schema, JToken token, Action<JsonSchema, JToken> validate, Action<JsonSchema, JToken> transform)
        {
            validate?.Invoke(schema, token);
            transform?.Invoke(schema, token);

            switch (token)
            {
                case JArray array:
                    if (schema.Items != null)
                    {
                        foreach (var item in array)
                        {
                            Travel(schema.Items, item, validate, transform);
                        }
                    }
                    break;

                case JObject map:
                    foreach (var (key, value) in map)
                    {
                        if (schema.Properties.TryGetValue(key, out var propertySchema))
                        {
                            Travel(propertySchema, value, validate, transform);
                        }
                    }
                    break;
            }
        }
    }
}
