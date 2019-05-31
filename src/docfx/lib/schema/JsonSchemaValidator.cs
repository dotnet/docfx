// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class JsonSchemaValidator
    {
        private readonly JsonSchema _schema;
        private readonly JsonSchemaDefinition _definitions;

        public JsonSchemaValidator(JsonSchema schema)
        {
            _schema = schema;
            _definitions = new JsonSchemaDefinition(schema);
        }

        public List<Error> Validate(JToken token)
        {
            var errors = new List<Error>();
            Validate(_schema, token, errors);
            return errors;
        }

        private void Validate(JsonSchema schema, JToken token, List<Error> errors)
        {
            schema = _definitions.GetDefinition(schema);

            if (!ValidateType(schema, token, errors))
            {
                return;
            }

            ValidateDeprecated(schema, token, errors);

            switch (token)
            {
                case JValue scalar:
                    ValidateScalar(schema, scalar, errors);
                    break;

                case JArray array:
                    ValidateArray(schema, array, errors);
                    break;

                case JObject map:
                    ValidateObject(schema, map, errors);
                    break;
            }
        }

        private bool ValidateType(JsonSchema schema, JToken token, List<Error> errors)
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

        private void ValidateScalar(JsonSchema schema, JValue scalar, List<Error> errors)
        {
            if (schema.Enum != null && !schema.Enum.Contains(scalar))
            {
                errors.Add(Errors.UndefinedValue(JsonUtility.GetSourceInfo(scalar), scalar, schema.Enum));
            }

            ValidateDateFormat(schema, scalar, errors);

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

        private void ValidateArray(JsonSchema schema, JArray array, List<Error> errors)
        {
            if (schema.Items != null)
            {
                foreach (var item in array)
                {
                    Validate(schema.Items, item, errors);
                }
            }

            if (schema.MaxItems.HasValue && array.Count > schema.MaxItems.Value)
                errors.Add(Errors.ArrayLengthInvalid(JsonUtility.GetSourceInfo(array), array.Path, maxItems: schema.MaxItems));

            if (schema.MinItems.HasValue && array.Count < schema.MinItems.Value)
                errors.Add(Errors.ArrayLengthInvalid(JsonUtility.GetSourceInfo(array), array.Path, minItems: schema.MinItems));
        }

        private void ValidateObject(JsonSchema schema, JObject map, List<Error> errors)
        {
            ValidateAdditionalProperties(schema, map, errors);
            ValidateRequired(schema, map, errors);
            ValidateDependencies(schema, map, errors);
            ValidateEither(schema, map, errors);
            ValidatePrecludes(schema, map, errors);

            foreach (var (key, value) in map)
            {
                if (schema.Properties.TryGetValue(key, out var propertySchema))
                {
                    Validate(propertySchema, value, errors);
                }
            }
        }

        private void ValidateAdditionalProperties(JsonSchema schema, JObject map, List<Error> errors)
        {
            if (schema.AdditionalProperties.additionalPropertyJsonSchema != null)
            {
                foreach (var (key, value) in map)
                {
                    if (!schema.Properties.Keys.Contains(key))
                    {
                        Validate(schema.AdditionalProperties.additionalPropertyJsonSchema, value, errors);
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
        }

        private void ValidateDependencies(JsonSchema schema, JObject map, List<Error> errors)
        {
            foreach (var (key, value) in schema.Dependencies)
            {
                if (map.ContainsKey(key))
                {
                    foreach (var otherKey in value)
                    {
                        if (!map.ContainsKey(otherKey))
                        {
                            errors.Add(Errors.LackDependency(JsonUtility.GetSourceInfo(map), key, otherKey));
                        }
                    }
                }
            }
        }

        private void ValidateRequired(JsonSchema schema, JObject map, List<Error> errors)
        {
            foreach (var key in schema.Required)
            {
                if (!map.ContainsKey(key))
                {
                    errors.Add(Errors.FieldRequired(JsonUtility.GetSourceInfo(map), key));
                }
            }
        }

        private void ValidateEither(JsonSchema schema, JObject map, List<Error> errors)
        {
            foreach (var keys in schema.Either)
            {
                var result = false;
                foreach (var key in keys)
                {
                    if (map.ContainsKey(key))
                    {
                        result = true;
                        break;
                    }
                }

                if (!result)
                {
                    errors.Add(Errors.EitherLogicFailed(JsonUtility.GetSourceInfo(map), keys));
                }
            }
        }

        private void ValidatePrecludes(JsonSchema schema, JObject map, List<Error> errors)
        {
            foreach (var keys in schema.Precludes)
            {
                var existNum = 0;
                foreach (var key in keys)
                {
                    if (map.ContainsKey(key) && ++existNum > 1)
                    {
                        errors.Add(Errors.PrecludesLogicFailed(JsonUtility.GetSourceInfo(map), keys));
                        break;
                    }
                }
            }
        }

        private void ValidateDateFormat(JsonSchema schema, JValue scalar, List<Error> errors)
        {
            if (!string.IsNullOrEmpty(schema.DateFormat))
            {
                if (DateTime.TryParseExact(scalar.Value.ToString(), schema.DateFormat, null, System.Globalization.DateTimeStyles.None, out var date))
                {
                    ValidateDateRange(schema, scalar, date, errors);
                }
                else
                {
                    errors.Add(Errors.DateFormatInvalid(JsonUtility.GetSourceInfo(scalar), scalar.Path, schema.DateFormat));
                }
            }
        }

        private void ValidateDateRange(JsonSchema schema, JValue scalar, DateTime date, List<Error> errors)
        {
            var diff = date - DateTime.Now;

            if ((schema.RelativeMinDate.HasValue && diff < schema.RelativeMinDate) || (schema.RelativeMaxDate.HasValue && diff > schema.RelativeMaxDate))
            {
                errors.Add(Errors.OverDateRange(JsonUtility.GetSourceInfo(scalar), scalar.Path, schema.RelativeMinDate, schema.RelativeMaxDate));
            }
        }

        private void ValidateDeprecated(JsonSchema schema,  JToken token, List<Error> errors)
        {
            if (schema.ReplacedBy != null)
            {
                errors.Add(Errors.FieldDeprecated(JsonUtility.GetSourceInfo(token), token.Path, schema.ReplacedBy.Value.ToString()));
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
