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
        private readonly MicrosoftAliasCache _microsoftAliasCache;

        public JsonSchemaValidator(JsonSchema schema, MicrosoftAliasCache microsoftAliasCache = null)
        {
            _schema = schema;
            _definitions = new JsonSchemaDefinition(schema);
            _microsoftAliasCache = microsoftAliasCache;
        }

        public List<Error> Validate(JToken token)
        {
            var errors = new List<Error>();
            Validate(_schema, string.Empty, token, errors);
            return errors;
        }

        private void Validate(JsonSchema schema, string name, JToken token, List<Error> errors)
        {
            schema = _definitions.GetDefinition(schema);

            if (!ValidateType(schema, token, errors))
            {
                return;
            }

            ValidateDeprecated(schema, name, token, errors);

            switch (token)
            {
                case JValue scalar:
                    ValidateScalar(schema, name, scalar, errors);
                    break;

                case JArray array:
                    ValidateArray(schema, name, array, errors);
                    break;

                case JObject map:
                    ValidateObject(schema, name, map, errors);
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

        private void ValidateScalar(JsonSchema schema, string name, JValue scalar, List<Error> errors)
        {
            if (schema.Enum != null && Array.IndexOf(schema.Enum, scalar) == -1)
            {
                errors.Add(Errors.UndefinedValue(JsonUtility.GetSourceInfo(scalar), scalar, schema.Enum));
            }

            if (scalar.Value is string str)
            {
                ValidateDateFormat(schema, name, scalar, str, errors);
                ValidateMicrosoftAlias(schema, name, scalar, str, errors);

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

        private void ValidateArray(JsonSchema schema, string name, JArray array, List<Error> errors)
        {
            if (schema.Items != null)
            {
                foreach (var item in array)
                {
                    Validate(schema.Items, name, item, errors);
                }
            }

            if (schema.MaxItems.HasValue && array.Count > schema.MaxItems.Value)
                errors.Add(Errors.ArrayLengthInvalid(JsonUtility.GetSourceInfo(array), array.Path, maxItems: schema.MaxItems));

            if (schema.MinItems.HasValue && array.Count < schema.MinItems.Value)
                errors.Add(Errors.ArrayLengthInvalid(JsonUtility.GetSourceInfo(array), array.Path, minItems: schema.MinItems));
        }

        private void ValidateObject(JsonSchema schema, string name, JObject map, List<Error> errors)
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
                    Validate(propertySchema, key, value, errors);
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
                        Validate(schema.AdditionalProperties.additionalPropertyJsonSchema, key, value, errors);
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

        private void ValidateDateFormat(JsonSchema schema, string name, JValue scalar, string dateString, List<Error> errors)
        {
            if (!string.IsNullOrEmpty(schema.DateFormat))
            {
                if (DateTime.TryParseExact(dateString, schema.DateFormat, null, System.Globalization.DateTimeStyles.None, out var date))
                {
                    ValidateDateRange(schema, name, scalar, date, errors);
                }
                else
                {
                    errors.Add(Errors.DateFormatInvalid(JsonUtility.GetSourceInfo(scalar), name, schema.DateFormat));
                }
            }
        }

        private void ValidateMicrosoftAlias(JsonSchema schema, string name, JValue scalar, string alias, List<Error> errors)
        {
            if (schema.MicrosoftAlias != null)
            {
                if (Array.IndexOf(schema.MicrosoftAlias.AllowedDLs, alias) == -1)
                {
                    if (_microsoftAliasCache != null && _microsoftAliasCache.GetAsync(alias).GetAwaiter().GetResult() == null)
                    {
                        errors.Add(Errors.MsAliasInvalid(JsonUtility.GetSourceInfo(scalar), name, alias));
                    }
                }
            }
        }

        private void ValidateDateRange(JsonSchema schema, string name, JValue scalar, DateTime date, List<Error> errors)
        {
            var diff = date - DateTime.Now;

            if ((schema.RelativeMinDate.HasValue && diff < schema.RelativeMinDate) || (schema.RelativeMaxDate.HasValue && diff > schema.RelativeMaxDate))
            {
                errors.Add(Errors.OverDateRange(JsonUtility.GetSourceInfo(scalar), name, schema.RelativeMinDate, schema.RelativeMaxDate));
            }
        }

        private void ValidateDeprecated(JsonSchema schema, string name, JToken token, List<Error> errors)
        {
            if (schema.ReplacedBy != null)
            {
                errors.Add(Errors.FieldDeprecated(JsonUtility.GetSourceInfo(token), name, schema.ReplacedBy));
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
    }
}
