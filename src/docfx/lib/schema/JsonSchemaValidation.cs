// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal static class JsonSchemaValidation
    {
        public static List<Error> Validate(JsonSchema schema, JToken token)
        {
            var errors = new List<Error>();
            ValidateCore(schema, token, errors);
            return errors;
        }

        private static void ValidateCore(JsonSchema schema, JToken token, List<Error> errors)
        {
            if (!TypeMatches(schema.Type, token.Type))
            {
                errors.Add(Errors.ViolateSchema(
                    JsonUtility.GetSourceInfo(token),
                    $"Expected type {schema.Type}, please input {schema.Type} or type compatible with {schema.Type}."));
                return;
            }

            switch (token)
            {
                case JValue scalar:
                    if (schema.Enum.Count > 0 && !schema.Enum.Contains(scalar))
                    {
                        errors.Add(Errors.UndefinedValue(JsonUtility.GetSourceInfo(token), scalar, schema.Enum));
                    }
                    break;

                case JArray array:
                    if (schema.Items != null)
                    {
                        foreach (var item in array)
                        {
                            ValidateCore(schema.Items, item, errors);
                        }
                    }
                    break;

                case JObject map:
                    foreach (var (key, value) in map)
                    {
                        if (schema.Properties.TryGetValue(key, out var propertySchema))
                        {
                            ValidateCore(propertySchema, value, errors);
                        }
                    }
                    break;
            }
        }

        private static bool TypeMatches(JsonSchemaType schemaType, JTokenType tokenType)
        {
            switch (schemaType)
            {
                case JsonSchemaType.Array:
                    return tokenType == JTokenType.Array;
                case JsonSchemaType.Boolean:
                    return tokenType == JTokenType.Boolean;
                case JsonSchemaType.Integer:
                    return tokenType == JTokenType.Integer;
                case JsonSchemaType.Null:
                    return tokenType == JTokenType.Null;
                case JsonSchemaType.Number:
                    return tokenType == JTokenType.Integer || tokenType == JTokenType.Float;
                case JsonSchemaType.Object:
                    return tokenType == JTokenType.Object;
                case JsonSchemaType.String:
                    return tokenType == JTokenType.String || tokenType == JTokenType.Uri ||
                           tokenType == JTokenType.Date || tokenType == JTokenType.TimeSpan;
                default:
                    return true;
            }
        }
    }
}
