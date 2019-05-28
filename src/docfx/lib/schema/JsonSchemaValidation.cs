// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
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
            Validate(new JsonSchemaContext(schema), schema, token, errors);
            return errors;
        }

        private static void Validate(JsonSchemaContext context, JsonSchema schema, JToken token, List<Error> errors)
        {
            schema = context.GetDefinition(schema);

            if (!ValidateType(schema, token, errors))
            {
                return;
            }

            switch (token)
            {
                case JValue scalar:
                    ValidateScalar(schema, errors, scalar);
                    break;

                case JArray array:
                    ValidateArray(context, schema, errors, array);
                    break;

                case JObject map:
                    ValidateObject(context, schema, errors, map);
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

        private static void ValidateScalar(JsonSchema schema, List<Error> errors, JValue scalar)
        {
            if (schema.Enum != null && !schema.Enum.Contains(scalar))
            {
                errors.Add(Errors.UndefinedValue(JsonUtility.GetSourceInfo(scalar), scalar, schema.Enum));
            }

            if (scalar.Value is string str)
            {
                if (schema.MaxLength.HasValue || schema.MinLength.HasValue)
                {
                    var unicodeLength = str.Where(c => !char.IsLowSurrogate(c)).Count();
                    if (schema.MaxLength.HasValue && unicodeLength > schema.MaxLength.Value)
                        errors.Add(Errors.StringLengthInvalid(JsonUtility.GetSourceInfo(scalar), scalar.Path, maxLength: schema.MaxLength));

                    if (schema.MinLength.HasValue && unicodeLength < schema.MinLength.Value)
                        errors.Add(Errors.StringLengthInvalid(JsonUtility.GetSourceInfo(scalar), scalar.Path, minLength: schema.MinLength));
                }

                switch (schema.Format)
                {
                    case JsonSchemaStringFormat.DateTime:
                        if (!DateTime.TryParse(str, out var _))
                            errors.Add(Errors.FormatInvalid(JsonUtility.GetSourceInfo(scalar), scalar.Value<string>(), JsonSchemaStringFormat.DateTime));
                        break;
                }
            }
        }

        private static void ValidateArray(JsonSchemaContext context, JsonSchema schema, List<Error> errors, JArray array)
        {
            if (schema.Items != null)
            {
                foreach (var item in array)
                {
                    Validate(context, schema.Items, item, errors);
                }
            }

            if (schema.MaxItems.HasValue && array.Count > schema.MaxItems.Value)
                errors.Add(Errors.ArrayLengthInvalid(JsonUtility.GetSourceInfo(array), array.Path, maxItems: schema.MaxItems));

            if (schema.MinItems.HasValue && array.Count < schema.MinItems.Value)
                errors.Add(Errors.ArrayLengthInvalid(JsonUtility.GetSourceInfo(array), array.Path, minItems: schema.MinItems));
        }

        private static void ValidateObject(JsonSchemaContext context, JsonSchema schema, List<Error> errors, JObject map)
        {
            if (schema.AdditionalProperties.additionalPropertyJsonSchema != null)
            {
                foreach (var (key, value) in map)
                {
                    if (!schema.Properties.Keys.Contains(key))
                    {
                        Validate(context, schema.AdditionalProperties.additionalPropertyJsonSchema, value, errors);
                    }
                }
            }
            else if (!schema.AdditionalProperties.additionalProperties)
            {
                foreach (var (key, value) in map)
                {
                    if (!schema.Properties.Keys.Contains(key))
                    {
                        errors.Add(Errors.UnknownField(JsonUtility.GetSourceInfo(value), key, value.Type.ToString()));
                    }
                }
            }

            foreach (var key in schema.Required)
            {
                if (!map.ContainsKey(key))
                {
                    errors.Add(Errors.FieldRequired(JsonUtility.GetSourceInfo(map), key));
                }
            }

            foreach (var (key, value) in map)
            {
                if (schema.Properties.TryGetValue(key, out var propertySchema))
                {
                    Validate(context, propertySchema, value, errors);
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
                    return tokenType == JTokenType.String;
                default:
                    return true;
            }
        }
    }
}
