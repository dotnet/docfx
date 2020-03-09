// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class JsonSchemaValidator
    {
        private readonly bool _forceError;
        private readonly JsonSchema _schema;
        private readonly JsonSchemaDefinition _definitions;
        private readonly MicrosoftGraphAccessor? _microsoftGraphAccessor;

        public JsonSchemaValidator(JsonSchema schema, MicrosoftGraphAccessor? microsoftGraphAccessor = null, bool forceError = false)
        {
            _schema = schema;
            _forceError = forceError;
            _definitions = new JsonSchemaDefinition(schema);
            _microsoftGraphAccessor = microsoftGraphAccessor;
        }

        public List<Error> Validate(JToken token)
        {
            return Validate(_schema, token);
        }

        private List<Error> Validate(JsonSchema schema, JToken token)
        {
            var errors = new List<(string name, Error)>();
            Validate(schema, "", token, errors);
            return errors.Select(info => GetError(_schema, info.name, info.Item2)).ToList();
        }

        private void Validate(JsonSchema schema, string name, JToken token, List<(string name, Error)> errors)
        {
            schema = _definitions.GetDefinition(schema);

            if (!ValidateType(schema, name, token, errors))
            {
                return;
            }

            ValidateBooleanSchema(schema, name, token, errors);
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

        private bool ValidateType(JsonSchema schema, string name, JToken token, List<(string name, Error)> errors)
        {
            if (schema.Type != null)
            {
                if (!schema.Type.Any(schemaType => TypeMatches(schemaType, token.Type)))
                {
                    errors.Add((name, Errors.JsonSchema.UnexpectedType(JsonUtility.GetSourceInfo(token), string.Join(", ", schema.Type), token.Type.ToString())));
                    return false;
                }
            }
            return true;
        }

        private void ValidateScalar(JsonSchema schema, string name, JValue scalar, List<(string name, Error)> errors)
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

        private void ValidateArray(JsonSchema schema, string name, JArray array, List<(string name, Error)> errors)
        {
            if (schema.MaxItems.HasValue && array.Count > schema.MaxItems.Value)
                errors.Add((name, Errors.JsonSchema.ArrayLengthInvalid(JsonUtility.GetSourceInfo(array), name, $"<= {schema.MaxItems}")));

            if (schema.MinItems.HasValue && array.Count < schema.MinItems.Value)
                errors.Add((name, Errors.JsonSchema.ArrayLengthInvalid(JsonUtility.GetSourceInfo(array), name, $">= {schema.MinItems}")));

            ValidateItems(schema, name, array, errors);

            if (schema.UniqueItems && array.Distinct(JsonUtility.DeepEqualsComparer).Count() != array.Count)
            {
                errors.Add((name, Errors.JsonSchema.ArrayNotUnique(JsonUtility.GetSourceInfo(array), name)));
            }

            if (schema.Contains != null && !array.Any(item => Validate(schema.Contains, item).Count == 0))
            {
                errors.Add((name, Errors.JsonSchema.ArrayContainsFailed(JsonUtility.GetSourceInfo(array), name)));
            }
        }

        private void ValidateItems(JsonSchema schema, string name, JArray array, List<(string name, Error)> errors)
        {
            var (items, eachItem) = schema.Items;

            if (items != null)
            {
                foreach (var item in array)
                {
                    Validate(items, name, item, errors);
                }
            }
            else if (eachItem != null)
            {
                for (var i = 0; i < array.Count; i++)
                {
                    if (i < eachItem.Length)
                    {
                        Validate(eachItem[i], name, array[i], errors);
                    }
                    else if (schema.AdditionalItems == JsonSchema.FalseSchema)
                    {
                        errors.Add((name, Errors.JsonSchema.ArrayLengthInvalid(JsonUtility.GetSourceInfo(array), name, $"<= {eachItem.Length}")));
                        break;
                    }
                    else if (schema.AdditionalItems != null && schema.AdditionalItems != JsonSchema.FalseSchema)
                    {
                        Validate(schema.AdditionalItems, name, array[i], errors);
                    }
                }
            }
        }

        private void ValidateObject(JsonSchema schema, string name, JObject map, List<(string name, Error)> errors)
        {
            ValidateRequired(schema, map, errors);
            ValidateStrictRequired(schema, map, errors);
            ValidateDependencies(schema, name, map, errors);
            ValidateEither(schema, map, errors);
            ValidatePrecludes(schema, map, errors);
            ValidateEnumDependencies(schema.EnumDependencies, "", "", null, null, map, errors);
            ValidateProperties(schema, name, map, errors);
        }

        private void ValidateProperties(JsonSchema schema, string name, JObject map, List<(string name, Error)> errors)
        {
            if (schema.MaxProperties.HasValue && map.Count > schema.MaxProperties.Value)
                errors.Add((name, Errors.JsonSchema.PropertyCountInvalid(JsonUtility.GetSourceInfo(map), name, $"<= {schema.MaxProperties}")));

            if (schema.MinProperties.HasValue && map.Count < schema.MinProperties.Value)
                errors.Add((name, Errors.JsonSchema.PropertyCountInvalid(JsonUtility.GetSourceInfo(map), name, $">= {schema.MinProperties}")));

            foreach (var (key, value) in map)
            {
                if (value is null)
                {
                    continue;
                }

                if (schema.PropertyNames != null)
                {
                    var propertyName = new JValue(key);
                    JsonUtility.SetSourceInfo(propertyName, JsonUtility.GetKeySourceInfo(value));
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
                if (isAdditonalProperty && schema.AdditionalProperties != null)
                {
                    if (schema.AdditionalProperties == JsonSchema.FalseSchema)
                    {
                        errors.Add((name, Errors.JsonSchema.UnknownField(JsonUtility.GetSourceInfo(value), key, value.Type.ToString())));
                    }
                    else if (schema.AdditionalProperties != JsonSchema.TrueSchema)
                    {
                        Validate(schema.AdditionalProperties, name, value, errors);
                    }
                }
            }
        }

        private void ValidateBooleanSchema(JsonSchema schema, string name, JToken token, List<(string name, Error)> errors)
        {
            if (schema == JsonSchema.FalseSchema)
            {
                errors.Add((name, Errors.JsonSchema.BooleanSchemaFailed(JsonUtility.GetSourceInfo(token), name)));
            }
        }

        private void ValidateString(JsonSchema schema, string name, JValue scalar, string str, List<(string name, Error)> errors)
        {
            ValidateDateFormat(schema, name, scalar, str, errors);
            ValidateMicrosoftAlias(schema, name, scalar, str, errors);

            if (schema.MaxLength.HasValue || schema.MinLength.HasValue)
            {
                var unicodeLength = str.Where(c => !char.IsLowSurrogate(c)).Count();
                if (schema.MaxLength.HasValue && unicodeLength > schema.MaxLength.Value)
                    errors.Add((name, Errors.JsonSchema.StringLengthInvalid(JsonUtility.GetSourceInfo(scalar), name, $"<= {schema.MaxLength}")));

                if (schema.MinLength.HasValue && unicodeLength < schema.MinLength.Value)
                    errors.Add((name, Errors.JsonSchema.StringLengthInvalid(JsonUtility.GetSourceInfo(scalar), name, $">= {schema.MinLength}")));
            }

            if (schema.Pattern != null && !Regex.IsMatch(str, schema.Pattern))
            {
                errors.Add((name, Errors.JsonSchema.FormatInvalid(JsonUtility.GetSourceInfo(scalar), str, schema.Pattern)));
            }

            switch (schema.Format)
            {
                case JsonSchemaStringFormat.DateTime:
                    if (!DateTime.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.None, out var _))
                        errors.Add((name, Errors.JsonSchema.FormatInvalid(JsonUtility.GetSourceInfo(scalar), str, JsonSchemaStringFormat.DateTime)));
                    break;

                case JsonSchemaStringFormat.Date:
                    if (!DateTime.TryParseExact(str, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var _))
                        errors.Add((name, Errors.JsonSchema.FormatInvalid(JsonUtility.GetSourceInfo(scalar), str, JsonSchemaStringFormat.Date)));
                    break;

                case JsonSchemaStringFormat.Time:
                    if (!DateTime.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.NoCurrentDateDefault, out var time) || time.Date != default)
                        errors.Add((name, Errors.JsonSchema.FormatInvalid(JsonUtility.GetSourceInfo(scalar), str, JsonSchemaStringFormat.Time)));
                    break;
            }
        }

        private static void ValidateNumber(JsonSchema schema, string name, JValue scalar, double number, List<(string name, Error)> errors)
        {
            if (schema.Maximum.HasValue && number > schema.Maximum)
                errors.Add((name, Errors.JsonSchema.NumberInvalid(JsonUtility.GetSourceInfo(scalar), number, $"<= {schema.Maximum}")));

            if (schema.Minimum.HasValue && number < schema.Minimum)
                errors.Add((name, Errors.JsonSchema.NumberInvalid(JsonUtility.GetSourceInfo(scalar), number, $">= {schema.Minimum}")));

            if (schema.ExclusiveMaximum.HasValue && number >= schema.ExclusiveMaximum)
                errors.Add((name, Errors.JsonSchema.NumberInvalid(JsonUtility.GetSourceInfo(scalar), number, $"< {schema.ExclusiveMaximum}")));

            if (schema.ExclusiveMinimum.HasValue && number <= schema.ExclusiveMinimum)
                errors.Add((name, Errors.JsonSchema.NumberInvalid(JsonUtility.GetSourceInfo(scalar), number, $"> {schema.ExclusiveMinimum}")));

            if (schema.MultipleOf != 0)
            {
                var n = number / schema.MultipleOf;
                if (Math.Abs(n - Math.Floor(n)) > double.Epsilon)
                    errors.Add((name, Errors.JsonSchema.NumberInvalid(JsonUtility.GetSourceInfo(scalar), number, $"multiple of {schema.MultipleOf}")));
            }
        }

        private void ValidateConst(JsonSchema schema, string name, JToken token, List<(string name, Error)> errors)
        {
            if (schema.Const != null && !JsonUtility.DeepEqualsComparer.Equals(schema.Const, token))
            {
                errors.Add((name, Errors.JsonSchema.InvalidValue(JsonUtility.GetSourceInfo(token), name, token)));
            }
        }

        private void ValidateEnum(JsonSchema schema, string name, JToken token, List<(string name, Error)> errors)
        {
            if (schema.Enum != null && !schema.Enum.Contains(token, JsonUtility.DeepEqualsComparer))
            {
                errors.Add((name, Errors.JsonSchema.InvalidValue(JsonUtility.GetSourceInfo(token), name, token)));
            }
        }

        private void ValidateDependencies(JsonSchema schema, string name, JObject map, List<(string name, Error)> errors)
        {
            foreach (var (key, (propertyNames, subschema)) in schema.Dependencies)
            {
                if (IsStrictContain(map, key))
                {
                    if (propertyNames != null)
                    {
                        foreach (var otherKey in propertyNames)
                        {
                            if (!IsStrictContain(map, otherKey))
                            {
                                errors.Add((key, Errors.JsonSchema.MissingPairedAttribute(JsonUtility.GetSourceInfo(map), key, otherKey)));
                            }
                        }
                    }
                    else if (subschema != null)
                    {
                        Validate(subschema, name, map, errors);
                    }
                }
            }
        }

        private void ValidateRequired(JsonSchema schema, JObject map, List<(string name, Error)> errors)
        {
            foreach (var key in schema.Required)
            {
                if (!map.ContainsKey(key))
                {
                    errors.Add((key, Errors.JsonSchema.MissingAttribute(JsonUtility.GetSourceInfo(map), key)));
                }
            }
        }

        private void ValidateStrictRequired(JsonSchema schema, JObject map, List<(string name, Error)> errors)
        {
            foreach (var key in schema.StrictRequired)
            {
                if (!IsStrictContain(map, key))
                {
                    errors.Add((key, Errors.JsonSchema.MissingAttribute(JsonUtility.GetSourceInfo(map), key)));
                }
            }
        }

        private bool IsStrictHaveValue(JToken value)
        {
            return value switch
            {
                JObject _ => true,
                JArray _ => true,
                JValue v when v.Value is null => false,
                JValue v when v.Value is string str => !string.IsNullOrWhiteSpace(str),
                JValue _ => true,
                _ => false,
            };
        }

        private bool IsStrictContain(JObject map, string key) =>
            map.TryGetValue(key, out var value) && IsStrictHaveValue(value);

        private void ValidateEither(JsonSchema schema, JObject map, List<(string name, Error)> errors)
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
                    if (IsStrictContain(map, key))
                    {
                        result = true;
                        break;
                    }
                }

                if (!result)
                {
                    errors.Add((keys[0], Errors.JsonSchema.MissingEitherAttribute(JsonUtility.GetSourceInfo(map), keys)));
                }
            }
        }

        private void ValidatePrecludes(JsonSchema schema, JObject map, List<(string name, Error)> errors)
        {
            foreach (var keys in schema.Precludes)
            {
                var existNum = 0;
                foreach (var key in keys)
                {
                    if (IsStrictContain(map, key) && ++existNum > 1)
                    {
                        errors.Add((keys[0], Errors.JsonSchema.PrecludedAttributes(JsonUtility.GetSourceInfo(map), keys)));
                        break;
                    }
                }
            }
        }

        private void ValidateDateFormat(JsonSchema schema, string name, JValue scalar, string dateString, List<(string name, Error)> errors)
        {
            if (!string.IsNullOrEmpty(schema.DateFormat) && !string.IsNullOrWhiteSpace(dateString))
            {
                if (DateTime.TryParseExact(dateString, schema.DateFormat, null, DateTimeStyles.None, out var date))
                {
                    ValidateDateRange(schema, name, scalar, date, dateString, errors);
                }
                else
                {
                    errors.Add((name, Errors.JsonSchema.DateFormatInvalid(JsonUtility.GetSourceInfo(scalar), name, dateString)));
                }
            }
        }

        private void ValidateMicrosoftAlias(JsonSchema schema, string name, JValue scalar, string alias, List<(string name, Error)> errors)
        {
            if (schema.MicrosoftAlias != null && !string.IsNullOrWhiteSpace(alias))
            {
                if (Array.IndexOf(schema.MicrosoftAlias.AllowedDLs, alias) == -1)
                {
                    if (_microsoftGraphAccessor != null)
                    {
                        var error = _microsoftGraphAccessor.ValidateMicrosoftAlias(
                            new SourceInfo<string>(alias, JsonUtility.GetSourceInfo(scalar)), name).GetAwaiter().GetResult();
                        if (error != null)
                        {
                            errors.Add((name, error));
                        }
                    }
                }
            }
        }

        private void ValidateDateRange(JsonSchema schema, string name, JValue scalar, DateTime date, string dateString, List<(string name, Error)> errors)
        {
            var diff = date - DateTime.UtcNow;

            if ((schema.RelativeMinDate.HasValue && diff < schema.RelativeMinDate) || (schema.RelativeMaxDate.HasValue && diff > schema.RelativeMaxDate))
            {
                errors.Add((name, Errors.JsonSchema.DateOutOfRange(JsonUtility.GetSourceInfo(scalar), name, dateString)));
            }
        }

        private void ValidateDeprecated(JsonSchema schema, string name, JToken token, List<(string name, Error)> errors)
        {
            if (IsStrictHaveValue(token) && schema.ReplacedBy != null)
            {
                errors.Add((name, Errors.JsonSchema.AttributeDeprecated(JsonUtility.GetSourceInfo(token), name, schema.ReplacedBy)));
            }
        }

        private void ValidateEnumDependencies(
            EnumDependenciesSchema? enumDependencies,
            string dependentFieldNameWithIndex,
            string dependentFieldName,
            JToken? dependentFieldRawValue,
            JToken? dependentFieldValue,
            JObject map,
            List<(string name, Error)> errors)
        {
            if (enumDependencies == null)
            {
                return;
            }

            foreach (var (fieldNameWithIndex, allowList) in enumDependencies)
            {
                var (fieldName, fieldIndex) = GetFieldNameAndIndex(fieldNameWithIndex);
                if (map.TryGetValue(fieldName, out var fieldRawValue))
                {
                    var fieldValue = fieldRawValue is JArray array ? (fieldIndex < array.Count ? array[fieldIndex] : null) : fieldRawValue;
                    if (fieldValue is null)
                    {
                        continue;
                    }

                    if (!IsStrictHaveValue(fieldValue))
                    {
                        return;
                    }

                    if (allowList.TryGetValue(fieldValue, out var nextEnumDependencies))
                    {
                        ValidateEnumDependencies(nextEnumDependencies, fieldNameWithIndex, fieldName, fieldRawValue, fieldValue, map, errors);
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(dependentFieldNameWithIndex))
                        {
                            errors.Add((fieldName, Errors.JsonSchema.InvalidValue(
                                JsonUtility.GetSourceInfo(fieldValue),
                                fieldRawValue.Type == JTokenType.Array ? fieldNameWithIndex : fieldName,
                                fieldValue)));
                        }
                        else
                        {
                            errors.Add((dependentFieldName, Errors.JsonSchema.InvalidPairedAttribute(
                                JsonUtility.GetSourceInfo(fieldValue),
                                fieldRawValue.Type == JTokenType.Array ? fieldNameWithIndex : fieldName,
                                fieldValue,
                                dependentFieldRawValue?.Type == JTokenType.Array ? dependentFieldNameWithIndex : dependentFieldName,
                                dependentFieldValue)));
                        }
                    }
                }
            }
        }

        // For string type: name = 'topic', output = ('topic', 0) or name = 'topic[0]', output = ('topic', 0)
        // For array type: name = 'topic[0]', output = ('topic', 0) or name = 'topic[1]', output = ('topic', 1) or ...
        private (string, int) GetFieldNameAndIndex(string name)
        {
            if (name.Contains('[') && name.Contains(']'))
            {
                var match = Regex.Match(name, @"\[\d+\]$");

                if (match.Success)
                {
                    if (int.TryParse(match.Value.Substring(1, match.Value.Length - 2), out var index))
                    {
                        return (name.Substring(0, name.Length - match.Value.Length), index);
                    }
                }
            }

            return (name, 0);
        }

        private Error GetError(JsonSchema schema, string name, Error error)
        {
            if (_forceError)
            {
                error = error.WithLevel(ErrorLevel.Error);
            }

            if (!string.IsNullOrEmpty(name) &&
                schema.CustomErrors.TryGetValue(name, out var attributeCustomErrors) &&
                attributeCustomErrors.TryGetValue(error.Code, out var customError))
            {
                return error.WithCustomError(customError);
            }

            return error;
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
