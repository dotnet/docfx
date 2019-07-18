// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class JsonSchemaValidator
    {
        private readonly JsonSchema _schema;
        private readonly JsonSchemaDefinition _definitions;
        private readonly MicrosoftGraphCache _microsoftGraphCache;

        public JsonSchemaValidator(JsonSchema schema, MicrosoftGraphCache microsoftGraphCache = null)
        {
            _schema = schema;
            _definitions = new JsonSchemaDefinition(schema);
            _microsoftGraphCache = microsoftGraphCache;
        }

        public List<Error> Validate(JToken token)
        {
            var errors = new Dictionary<Error, string>();
            Validate(_schema, string.Empty, token, errors);
            return errors.Select(info => OverwriteError(_schema, info.Value, info.Key)).ToList();
        }

        private void Validate(JsonSchema schema, string name, JToken token, Dictionary<Error, string> errors)
        {
            schema = _definitions.GetDefinition(schema);

            if (!ValidateType(schema, name, token, errors))
            {
                return;
            }

            ValidateDeprecated(schema, name, token, errors);
            ValidateConst(schema, name, token, errors);
            ValidateEnum(schema, name, token, errors);

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

        private bool ValidateType(JsonSchema schema, string name, JToken token, Dictionary<Error, string> errors)
        {
            if (schema.Type != null)
            {
                if (!schema.Type.Any(schemaType => TypeMatches(schemaType, token.Type)))
                {
                    errors.Add(Errors.UnexpectedType(JsonUtility.GetSourceInfo(token), string.Join(", ", schema.Type), token.Type.ToString()), name);
                    return false;
                }
            }
            return true;
        }

        private void ValidateScalar(JsonSchema schema, string name, JValue scalar, Dictionary<Error, string> errors)
        {
            switch (scalar.Value)
            {
                case string str:
                    ValidateString(schema, name, scalar, str, errors);
                    break;

                case double _:
                case float _:
                case long _:
                    ValidateNumber(schema, name, scalar, Convert.ToDouble(scalar.Value), errors);
                    break;
            }
        }

        private void ValidateArray(JsonSchema schema, string name, JArray array, Dictionary<Error, string> errors)
        {
            if (schema.MaxItems.HasValue && array.Count > schema.MaxItems.Value)
                errors.Add(Errors.ArrayLengthInvalid(JsonUtility.GetSourceInfo(array), name, $"<= {schema.MaxItems}"), name);

            if (schema.MinItems.HasValue && array.Count < schema.MinItems.Value)
                errors.Add(Errors.ArrayLengthInvalid(JsonUtility.GetSourceInfo(array), name, $">= {schema.MinItems}"), name);

            if (schema.Items != null)
            {
                foreach (var item in array)
                {
                    Validate(schema.Items, name, item, errors);
                }
            }

            if (schema.UniqueItems && array.Distinct(JsonUtility.DeepEqualsComparer).Count() != array.Count)
            {
                errors.Add(Errors.ArrayNotUnique(JsonUtility.GetSourceInfo(array), name), name);
            }
        }

        private void ValidateObject(JsonSchema schema, string name, JObject map, Dictionary<Error, string> errors)
        {
            ValidateRequired(schema, map, errors);
            ValidateDependencies(schema, map, errors);
            ValidateEither(schema, map, errors);
            ValidatePrecludes(schema, map, errors);
            ValidateEnumDependencies(schema, map, errors);
            ValidateProperties(schema, name, map, errors);
        }

        private void ValidateProperties(JsonSchema schema, string name, JObject map, Dictionary<Error, string> errors)
        {
            if (schema.MaxProperties.HasValue && map.Count > schema.MaxProperties.Value)
                errors.Add(Errors.PropertyCountInvalid(JsonUtility.GetSourceInfo(map), name, $"<= {schema.MaxProperties}"), name);

            if (schema.MinProperties.HasValue && map.Count < schema.MinProperties.Value)
                errors.Add(Errors.PropertyCountInvalid(JsonUtility.GetSourceInfo(map), name, $">= {schema.MinProperties}"), name);

            foreach (var property in map.Properties())
            {
                var key = property.Name;
                var value = property.Value;

                if (schema.PropertyNames != null)
                {
                    var propertyName = new JValue(key);
                    JsonUtility.SetSourceInfo(propertyName, JsonUtility.GetSourceInfo(property));
                    Validate(schema.PropertyNames, key, propertyName, errors);
                }

                var isAdditonalProperty = true;

                // properties
                if (schema.Properties.TryGetValue(key, out var propertySchema))
                {
                    Validate(propertySchema, key, value, errors);
                    isAdditonalProperty = false;
                }

                // patternProperties
                foreach (var (pattern, patternPropertySchema) in schema.PatternProperties)
                {
                    if (Regex.IsMatch(key, pattern))
                    {
                        Validate(patternPropertySchema, key, value, errors);
                        isAdditonalProperty = false;
                    }
                }

                // additionalProperties
                if (isAdditonalProperty)
                {
                    if (schema.AdditionalProperties.schema != null)
                    {
                        Validate(schema.AdditionalProperties.schema, name, value, errors);
                    }
                    else if (!schema.AdditionalProperties.value)
                    {
                        errors.Add(Errors.UnknownField(JsonUtility.GetSourceInfo(value), key, value.Type.ToString()), name);
                    }
                }
            }
        }

        private void ValidateString(JsonSchema schema, string name, JValue scalar, string str, Dictionary<Error, string> errors)
        {
            ValidateDateFormat(schema, name, scalar, str, errors);
            ValidateMicrosoftAlias(schema, name, scalar, str, errors);

            if (schema.MaxLength.HasValue || schema.MinLength.HasValue)
            {
                var unicodeLength = str.Where(c => !char.IsLowSurrogate(c)).Count();
                if (schema.MaxLength.HasValue && unicodeLength > schema.MaxLength.Value)
                    errors.Add(Errors.StringLengthInvalid(JsonUtility.GetSourceInfo(scalar), name, $"<= {schema.MaxLength}"), name);

                if (schema.MinLength.HasValue && unicodeLength < schema.MinLength.Value)
                    errors.Add(Errors.StringLengthInvalid(JsonUtility.GetSourceInfo(scalar), name, $">= {schema.MinLength}"), name);
            }

            if (schema.Pattern != null && !Regex.IsMatch(str, schema.Pattern))
            {
                errors.Add(Errors.FormatInvalid(JsonUtility.GetSourceInfo(scalar), str, schema.Pattern), name);
            }

            switch (schema.Format)
            {
                case JsonSchemaStringFormat.DateTime:
                    if (!DateTime.TryParse(str, out var _))
                        errors.Add(Errors.FormatInvalid(JsonUtility.GetSourceInfo(scalar), str, JsonSchemaStringFormat.DateTime), name);
                    break;
            }
        }

        private void ValidateNumber(JsonSchema schema, string name, JValue scalar, double number, Dictionary<Error, string> errors)
        {
            if (schema.Maximum.HasValue && number > schema.Maximum)
                errors.Add(Errors.NumberInvalid(JsonUtility.GetSourceInfo(scalar), name, $"<= {schema.Maximum}"), name);

            if (schema.Minimum.HasValue && number < schema.Minimum)
                errors.Add(Errors.NumberInvalid(JsonUtility.GetSourceInfo(scalar), name, $">= {schema.Minimum}"), name);

            if (schema.ExclusiveMaximum.HasValue && number >= schema.ExclusiveMaximum)
                errors.Add(Errors.NumberInvalid(JsonUtility.GetSourceInfo(scalar), name, $"< {schema.ExclusiveMaximum}"), name);

            if (schema.ExclusiveMinimum.HasValue && number <= schema.ExclusiveMinimum)
                errors.Add(Errors.NumberInvalid(JsonUtility.GetSourceInfo(scalar), name, $"> {schema.ExclusiveMinimum}"), name);
        }

        private void ValidateConst(JsonSchema schema, string name, JToken token, Dictionary<Error, string> errors)
        {
            if (schema.Const != null && !JsonUtility.DeepEqualsComparer.Equals(schema.Const, token))
            {
                errors.Add(Errors.InvalidValue(JsonUtility.GetSourceInfo(token), name, token), name);
            }
        }

        private void ValidateEnum(JsonSchema schema, string name, JToken token, Dictionary<Error, string> errors)
        {
            if (schema.Enum != null && !schema.Enum.Contains(token, JsonUtility.DeepEqualsComparer))
            {
                errors.Add(Errors.InvalidValue(JsonUtility.GetSourceInfo(token), name, token), name);
            }
        }

        private void ValidateDependencies(JsonSchema schema, JObject map, Dictionary<Error, string> errors)
        {
            foreach (var (key, value) in schema.Dependencies)
            {
                if (map.ContainsKey(key))
                {
                    foreach (var otherKey in value)
                    {
                        if (!map.ContainsKey(otherKey))
                        {
                            errors.Add(Errors.MissingPairedAttribute(JsonUtility.GetSourceInfo(map), key, otherKey), key);
                        }
                    }
                }
            }
        }

        private void ValidateRequired(JsonSchema schema, JObject map, Dictionary<Error, string> errors)
        {
            foreach (var key in schema.Required)
            {
                if (!map.ContainsKey(key))
                {
                    errors.Add(Errors.MissingAttribute(JsonUtility.GetSourceInfo(map), key), key);
                }
            }
        }

        private void ValidateEither(JsonSchema schema, JObject map, Dictionary<Error, string> errors)
        {
            foreach (var keys in schema.Either)
            {
                if (keys.Length == 0)
                {
                    continue;
                }

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
                    errors.Add(Errors.MissingEitherAttribute(JsonUtility.GetSourceInfo(map), keys), keys[0]);
                }
            }
        }

        private void ValidatePrecludes(JsonSchema schema, JObject map, Dictionary<Error, string> errors)
        {
            foreach (var keys in schema.Precludes)
            {
                var existNum = 0;
                foreach (var key in keys)
                {
                    if (map.ContainsKey(key) && ++existNum > 1)
                    {
                        errors.Add(Errors.PrecludedAttributes(JsonUtility.GetSourceInfo(map), keys), keys[0]);
                        break;
                    }
                }
            }
        }

        private void ValidateDateFormat(JsonSchema schema, string name, JValue scalar, string dateString, Dictionary<Error, string> errors)
        {
            if (!string.IsNullOrEmpty(schema.DateFormat))
            {
                if (DateTime.TryParseExact(dateString, schema.DateFormat, null, System.Globalization.DateTimeStyles.None, out var date))
                {
                    ValidateDateRange(schema, name, scalar, date, dateString, errors);
                }
                else
                {
                    errors.Add(Errors.DateFormatInvalid(JsonUtility.GetSourceInfo(scalar), name, dateString, schema.DateFormat), name);
                }
            }
        }

        private void ValidateMicrosoftAlias(JsonSchema schema, string name, JValue scalar, string alias, Dictionary<Error, string> errors)
        {
            if (schema.MicrosoftAlias != null)
            {
                if (Array.IndexOf(schema.MicrosoftAlias.AllowedDLs, alias) == -1)
                {
                    if (_microsoftGraphCache != null)
                    {
                        var (error, msAlias) = _microsoftGraphCache.GetMicrosoftAliasAsync(alias).GetAwaiter().GetResult();

                        if (error != null)
                        {
                            errors.Add(error, name);
                        }

                        // Mute error, when unable to connect to Microsoft Graph API
                        if (msAlias == null && _microsoftGraphCache.IsConnectedToGraphApi())
                        {
                            errors.Add(Errors.MsAliasInvalid(JsonUtility.GetSourceInfo(scalar), name, alias), name);
                        }
                    }
                }
            }
        }

        private void ValidateDateRange(JsonSchema schema, string name, JValue scalar, DateTime date, string dateString, Dictionary<Error, string> errors)
        {
            var diff = date - DateTime.Now;

            if ((schema.RelativeMinDate.HasValue && diff < schema.RelativeMinDate) || (schema.RelativeMaxDate.HasValue && diff > schema.RelativeMaxDate))
            {
                errors.Add(Errors.DateOutOfRange(JsonUtility.GetSourceInfo(scalar), name, dateString), name);
            }
        }

        private void ValidateDeprecated(JsonSchema schema, string name, JToken token, Dictionary<Error, string> errors)
        {
            if (schema.ReplacedBy != null)
            {
                errors.Add(Errors.AttributeDeprecated(JsonUtility.GetSourceInfo(token), name, schema.ReplacedBy), name);
            }
        }

        private void ValidateEnumDependencies(JsonSchema schema, JObject map, Dictionary<Error, string> errors)
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
                                errors.Add(Errors.InvalidPairedAttribute(JsonUtility.GetSourceInfo(map), fieldName, fieldValue, dependentFieldName, dependentFieldValue), dependentFieldName);
                            }
                        }
                        else
                        {
                            errors.Add(Errors.MissingPairedAttribute(JsonUtility.GetSourceInfo(map), fieldName, dependentFieldName), fieldName);
                        }
                    }
                }
            }
        }

        private Error OverwriteError(JsonSchema schema, string name, Error baseError)
        {
            if (!string.IsNullOrEmpty(name) && schema.OverwriteErrors.TryGetValue(name, out var attributeOverwriteErrors) && attributeOverwriteErrors.TryGetValue(baseError.Code, out var overwriteError))
            {
                return new Error(
                    overwriteError.Severity == ErrorLevel.Off ? baseError.Level : overwriteError.Severity,
                    string.IsNullOrEmpty(overwriteError.Code) ? baseError.Code : overwriteError.Code,
                    string.IsNullOrEmpty(overwriteError.Message) ? baseError.Message : overwriteError.Message,
                    baseError.File,
                    baseError.Line,
                    baseError.Column,
                    baseError.EndLine,
                    baseError.EndColumn);
            }

            return baseError;
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
