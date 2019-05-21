// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal static class JsonSchemaValidation
    {
        public static List<Error> Validate(JsonSchema schema, JToken token)
        {
            var errors = new List<Error>();
            Validate(schema, token, errors, schema);
            return errors;
        }

        private static void Validate(JsonSchema schema, JToken token, List<Error> errors, JsonSchema root)
        {
            schema = JsonSchemaUtility.GetDefinition(schema, root);

            if (!ValidateType(schema, token, errors))
            {
                return;
            }

            switch (token)
            {
                case JValue scalar:
                    ValidateScalar(schema, token, errors, scalar);
                    break;

                case JArray array:
                    ValidateArray(schema, errors, array, root);
                    break;

                case JObject map:
                    ValidateObject(schema, token, errors, map, root);
                    break;
            }
        }

        private static bool ValidateType(JsonSchema schema, JToken token, List<Error> errors)
        {
            if (schema.Type != null)
            {
                if (!schema.Type.Any(schemaType => TypeMatches(schemaType, token.Type)))
                {
                    errors.Add(Errors.UnexpectedType(JsonUtility.GetSourceInfo(token), string.Join(", ", schema.Type), token.Type.ToString()));
                    return false;
                }
            }
            return true;
        }

        private static void ValidateScalar(JsonSchema schema, JToken token, List<Error> errors, JValue scalar)
        {
            if (schema.Enum != null && !schema.Enum.Contains(scalar))
            {
                errors.Add(Errors.UndefinedValue(JsonUtility.GetSourceInfo(token), scalar, schema.Enum));
            }
        }

        private static void ValidateArray(JsonSchema schema, List<Error> errors, JArray array, JsonSchema root)
        {
            if (schema.Items != null)
            {
                foreach (var item in array)
                {
                    Validate(schema.Items, item, errors, root);
                }
            }
        }

        private static void ValidateObject(JsonSchema schema, JToken token, List<Error> errors, JObject map, JsonSchema root)
        {
            foreach (var key in schema.Required)
            {
                if (!map.ContainsKey(key))
                {
                    errors.Add(Errors.FieldRequired(JsonUtility.GetSourceInfo(token), key));
                }
            }

            foreach (var (key, value) in map)
            {
                if (schema.Properties.TryGetValue(key, out var propertySchema))
                {
                    Validate(propertySchema, value, errors, root);
                }
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
