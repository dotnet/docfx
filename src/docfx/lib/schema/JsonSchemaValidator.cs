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
            ValidateConst(schema, token, errors);

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
            if (schema.Enum != null && Array.IndexOf(schema.Enum, scalar) == -1)
            {
                errors.Add(Errors.UndefinedValue(JsonUtility.GetSourceInfo(scalar), scalar, schema.Enum));
            }

            switch (scalar.Value)
            {
                case string str:
                    ValidateString(schema, scalar, str, errors);
                    break;

                case double _:
                case float _:
                case long _:
                    ValidateNumber(schema, scalar, Convert.ToDouble(scalar.Value), errors);
                    break;
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
                errors.Add(Errors.ArrayLengthInvalid(JsonUtility.GetSourceInfo(array), array.Path, $"<= {schema.MaxItems}"));

            if (schema.MinItems.HasValue && array.Count < schema.MinItems.Value)
                errors.Add(Errors.ArrayLengthInvalid(JsonUtility.GetSourceInfo(array), array.Path, $">= {schema.MinItems}"));
        }

        private void ValidateObject(JsonSchema schema, JObject map, List<Error> errors)
        {
            ValidateAdditionalProperties(schema, map, errors);
            ValidateRequired(schema, map, errors);
            ValidateDependencies(schema, map, errors);
            ValidateEither(schema, map, errors);
            ValidatePrecludes(schema, map, errors);
            ValidateEnumDependencies(schema, map, errors);

            foreach (var (key, value) in map)
            {
                if (schema.Properties.TryGetValue(key, out var propertySchema))
                {
                    Validate(propertySchema, value, errors);
                }
            }
        }

        private void ValidateString(JsonSchema schema, JValue scalar, string str, List<Error> errors)
        {
            ValidateDateFormat(schema, scalar, str, errors);

            if (schema.MaxLength.HasValue || schema.MinLength.HasValue)
            {
                var unicodeLength = str.Where(c => !char.IsLowSurrogate(c)).Count();
                if (schema.MaxLength.HasValue && unicodeLength > schema.MaxLength.Value)
                    errors.Add(Errors.StringLengthInvalid(JsonUtility.GetSourceInfo(scalar), scalar.Path, $"<= {schema.MaxLength}"));

                if (schema.MinLength.HasValue && unicodeLength < schema.MinLength.Value)
                    errors.Add(Errors.StringLengthInvalid(JsonUtility.GetSourceInfo(scalar), scalar.Path, $">= {schema.MinLength}"));
            }

            switch (schema.Format)
            {
                case JsonSchemaStringFormat.DateTime:
                    if (!DateTime.TryParse(str, out var _))
                        errors.Add(Errors.FormatInvalid(JsonUtility.GetSourceInfo(scalar), scalar.Value<string>(), JsonSchemaStringFormat.DateTime));
                    break;
            }
        }

        private static void ValidateNumber(JsonSchema schema, JValue scalar, double number, List<Error> errors)
        {
            if (schema.Maximum.HasValue && number > schema.Maximum)
                errors.Add(Errors.NumberInvalid(JsonUtility.GetSourceInfo(scalar), scalar.Path, $"<= {schema.Maximum}"));

            if (schema.Minimum.HasValue && number < schema.Minimum)
                errors.Add(Errors.NumberInvalid(JsonUtility.GetSourceInfo(scalar), scalar.Path, $">= {schema.Minimum}"));

            if (schema.ExclusiveMaximum.HasValue && number >= schema.ExclusiveMaximum)
                errors.Add(Errors.NumberInvalid(JsonUtility.GetSourceInfo(scalar), scalar.Path, $"< {schema.ExclusiveMaximum}"));

            if (schema.ExclusiveMinimum.HasValue && number <= schema.ExclusiveMinimum)
                errors.Add(Errors.NumberInvalid(JsonUtility.GetSourceInfo(scalar), scalar.Path, $"> {schema.ExclusiveMinimum}"));
        }

        private void ValidateConst(JsonSchema schema, JToken token, List<Error> errors)
        {
            if (schema.Const != null && !JTokenDeepEquals(schema.Const, token))
            {
                errors.Add(Errors.UndefinedValue(JsonUtility.GetSourceInfo(token), token, new object[] { schema.Const }));
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

        private void ValidateDateFormat(JsonSchema schema, JValue scalar, string dateString, List<Error> errors)
        {
            if (!string.IsNullOrEmpty(schema.DateFormat))
            {
                if (DateTime.TryParseExact(dateString, schema.DateFormat, null, System.Globalization.DateTimeStyles.None, out var date))
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
                errors.Add(Errors.FieldDeprecated(JsonUtility.GetSourceInfo(token), token.Path, schema.ReplacedBy));
            }
        }

        private void ValidateEnumDependencies(JsonSchema schema, JObject map, List<Error> errors)
        {
            foreach (var (fieldName, enumDependencyRules) in schema.EnumDependencies)
            {
                if (map.TryGetValue(fieldName, out var fieldValue))
                {
                    foreach (var (dependentFieldName, allowLists) in enumDependencyRules)
                    {
                        if (map.TryGetValue(dependentFieldName, out var dependentFieldValue))
                        {
                            if (allowLists.TryGetValue(dependentFieldValue, out var allowList) &&
                                Array.IndexOf(allowList, fieldValue) == -1)
                            {
                                errors.Add(Errors.ValuesNotMatch(JsonUtility.GetSourceInfo(map), fieldName, fieldValue, dependentFieldName, dependentFieldValue, allowList));
                            }
                        }
                        else
                        {
                            errors.Add(Errors.LackDependency(JsonUtility.GetSourceInfo(map), fieldName, dependentFieldName));
                        }
                    }
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

        private static bool JTokenDeepEquals(JToken a, JToken b)
        {
            switch (a)
            {
                case JValue valueA when b is JValue valueB:
                    return Equals(valueA.Value, valueB.Value);

                case JArray arrayA when b is JArray arrayB:
                    if (arrayA.Count != arrayB.Count)
                    {
                        return false;
                    }

                    for (var i = 0; i < arrayA.Count; i++)
                    {
                        if (!JTokenDeepEquals(arrayA[i], arrayB[i]))
                        {
                            return false;
                        }
                    }
                    return true;

                case JObject mapA when b is JObject mapB:
                    if (mapA.Count != mapB.Count)
                    {
                        return false;
                    }

                    foreach (var (key, valueA) in mapA)
                    {
                        if (!mapB.TryGetValue(key, out var valueB) || !JTokenDeepEquals(valueA, valueB))
                        {
                            return false;
                        }
                    }
                    return true;

                default:
                    return false;
            }
        }
    }
}
