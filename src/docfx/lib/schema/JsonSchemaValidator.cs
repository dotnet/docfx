// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class JsonSchemaValidator
    {
        private readonly bool _forceError;
        private readonly JsonSchema _schema;
        private readonly JsonSchemaDefinition _definitions;
        private readonly MicrosoftGraphAccessor? _microsoftGraphAccessor;
        private readonly JsonSchemaValidatorExtension? _ext;
        private readonly ListBuilder<(JsonSchema schema, string key, JToken value, SourceInfo? source)> _metadataBuilder;
        private static readonly ThreadLocal<FilePath?> t_filePath = new ThreadLocal<FilePath?>();

        public JsonSchema Schema => _schema;

        public JsonSchemaValidator(
            JsonSchema schema,
            MicrosoftGraphAccessor? microsoftGraphAccessor = null,
            bool forceError = false,
            JsonSchemaValidatorExtension? ext = null)
        {
            _schema = schema;
            _forceError = forceError;
            _definitions = new JsonSchemaDefinition(schema);
            _microsoftGraphAccessor = microsoftGraphAccessor;
            _ext = ext;
            _metadataBuilder = new ListBuilder<(JsonSchema schema, string key, JToken value, SourceInfo? source)>();
        }

        public List<Error> Validate(JToken token, FilePath filePath)
        {
            try
            {
                if (filePath != null)
                {
                    t_filePath.Value = filePath;
                }
                return Validate(_schema, token);
            }
            finally
            {
                t_filePath.Value = null;
            }
        }

        public List<Error> PostValidate()
        {
            var errors = new List<Error>();
            PostValidateDocsetUnique(errors);
            return errors.Select(e => GetError(_schema, e)).ToList();
        }

        private List<Error> Validate(JsonSchema schema, JToken token)
        {
            var errors = new List<Error>();
            Validate(schema, "", token, errors);
            return errors.Select(error => GetError(_schema, error)).ToList();
        }

        private void Validate(JsonSchema schema, string jsonPath, JToken token, List<Error> errors)
        {
            schema = _definitions.GetDefinition(schema);

            if (!ValidateType(schema, jsonPath, token, errors))
            {
                return;
            }

            ValidateBooleanSchema(schema, jsonPath, token, errors);
            ValidateDeprecated(schema, jsonPath, token, errors);
            ValidateConst(schema, jsonPath, token, errors);
            ValidateEnum(schema, jsonPath, token, errors);

            switch (token)
            {
                case JValue scalar:
                    ValidateScalar(schema, jsonPath, scalar, errors);
                    break;

                case JArray array:
                    ValidateArray(schema, jsonPath, array, errors);
                    break;

                case JObject map:
                    ValidateObject(schema, jsonPath, map, errors);
                    break;
            }
        }

        private static bool ValidateType(JsonSchema schema, string jsonPath, JToken token, List<Error> errors)
        {
            if (schema.Type != null)
            {
                if (!schema.Type.Any(schemaType => TypeMatches(schemaType, token.Type)))
                {
                    errors.Add(
                        Errors.JsonSchema.UnexpectedType(JsonUtility.GetSourceInfo(token), string.Join(", ", schema.Type), token.Type.ToString(), jsonPath));
                    return false;
                }
            }
            return true;
        }

        private void ValidateScalar(JsonSchema schema, string jsonPath, JValue scalar, List<Error> errors)
        {
            switch (scalar.Value)
            {
                case string str:
                    ValidateString(schema, jsonPath, scalar, str, errors);
                    break;

                case double _:
                case float _:
                case long _:
                    ValidateNumber(schema, jsonPath, scalar, Convert.ToDouble(scalar.Value), errors);
                    break;
            }
        }

        private void ValidateArray(JsonSchema schema, string jsonPath, JArray array, List<Error> errors)
        {
            if (schema.MaxItems.HasValue && array.Count > schema.MaxItems.Value)
            {
                errors.Add(Errors.JsonSchema.ArrayLengthInvalid(JsonUtility.GetSourceInfo(array), jsonPath, $"<= {schema.MaxItems}"));
            }

            if (schema.MinItems.HasValue && array.Count < schema.MinItems.Value)
            {
                errors.Add(Errors.JsonSchema.ArrayLengthInvalid(JsonUtility.GetSourceInfo(array), jsonPath, $">= {schema.MinItems}"));
            }

            ValidateItems(schema, jsonPath, array, errors);

            if (schema.UniqueItems && array.Distinct(JsonUtility.DeepEqualsComparer).Count() != array.Count)
            {
                errors.Add(Errors.JsonSchema.ArrayNotUnique(JsonUtility.GetSourceInfo(array), jsonPath));
            }

            if (schema.Contains != null && !array.Any(item => Validate(schema.Contains, item).Count == 0))
            {
                errors.Add(Errors.JsonSchema.ArrayContainsFailed(JsonUtility.GetSourceInfo(array), jsonPath));
            }
        }

        private void ValidateItems(JsonSchema schema, string jsonPath, JArray array, List<Error> errors)
        {
            var (items, eachItem) = schema.Items;

            if (items != null)
            {
                foreach (var item in array)
                {
                    Validate(items, jsonPath, item, errors);
                }
            }
            else if (eachItem != null)
            {
                for (var i = 0; i < array.Count; i++)
                {
                    if (i < eachItem.Length)
                    {
                        Validate(eachItem[i], jsonPath, array[i], errors);
                    }
                    else if (schema.AdditionalItems == JsonSchema.FalseSchema)
                    {
                        errors.Add(Errors.JsonSchema.ArrayLengthInvalid(JsonUtility.GetSourceInfo(array), jsonPath, $"<= {eachItem.Length}"));
                        break;
                    }
                    else if (schema.AdditionalItems != null && schema.AdditionalItems != JsonSchema.FalseSchema)
                    {
                        Validate(schema.AdditionalItems, jsonPath, array[i], errors);
                    }
                }
            }
        }

        private void ValidateObject(JsonSchema schema, string jsonPath, JObject map, List<Error> errors)
        {
            ValidateRequired(schema, jsonPath, map, errors);
            ValidateStrictRequired(schema, jsonPath, map, errors);
            ValidateDependencies(schema, jsonPath, map, errors);
            ValidateEither(schema, jsonPath, map, errors);
            ValidatePrecludes(schema, jsonPath, map, errors);
            ValidateEnumDependencies(schema.EnumDependencies, "", "", null, null, map, errors);
            ValidateDocsetUnique(schema, map);
            ValidateProperties(schema, jsonPath, map, errors);
        }

        private void ValidateProperties(JsonSchema schema, string jsonPath, JObject map, List<Error> errors)
        {
            if (schema.MaxProperties.HasValue && map.Count > schema.MaxProperties.Value)
            {
                errors.Add(Errors.JsonSchema.PropertyCountInvalid(JsonUtility.GetSourceInfo(map), jsonPath, $"<= {schema.MaxProperties}"));
            }

            if (schema.MinProperties.HasValue && map.Count < schema.MinProperties.Value)
            {
                errors.Add(Errors.JsonSchema.PropertyCountInvalid(JsonUtility.GetSourceInfo(map), jsonPath, $">= {schema.MinProperties}"));
            }

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
                    Validate(schema.PropertyNames, string.IsNullOrWhiteSpace(jsonPath) ? $"{key}" : jsonPath += $".{key}", propertyName, errors);
                }

                var isAdditionalProperty = true;

                // properties
                if (schema.Properties.TryGetValue(key, out var propertySchema))
                {
                    Validate(propertySchema, string.IsNullOrWhiteSpace(jsonPath) ? $"{key}" : jsonPath += $".{key}", value, errors);
                    isAdditionalProperty = false;
                }

                // patternProperties
                foreach (var (pattern, patternPropertySchema) in schema.PatternProperties)
                {
                    if (Regex.IsMatch(key, pattern))
                    {
                        Validate(patternPropertySchema, string.IsNullOrWhiteSpace(jsonPath) ? $"{key}" : jsonPath += $".{key}", value, errors);
                        isAdditionalProperty = false;
                    }
                }

                // additionalProperties
                if (isAdditionalProperty && schema.AdditionalProperties != null)
                {
                    if (schema.AdditionalProperties == JsonSchema.FalseSchema)
                    {
                        errors.Add(Errors.JsonSchema.UnknownField(
                            JsonUtility.GetSourceInfo(value), string.IsNullOrWhiteSpace(jsonPath) ? $"{key}" : jsonPath += $".{key}", value.Type.ToString()));
                    }
                    else if (schema.AdditionalProperties != JsonSchema.TrueSchema)
                    {
                        Validate(schema.AdditionalProperties, string.IsNullOrWhiteSpace(jsonPath) ? $"{key}" : jsonPath += $".{key}", value, errors);
                    }
                }
            }
        }

        private static void ValidateBooleanSchema(JsonSchema schema, string jsonPath, JToken token, List<Error> errors)
        {
            if (schema == JsonSchema.FalseSchema)
            {
                errors.Add(Errors.JsonSchema.BooleanSchemaFailed(JsonUtility.GetSourceInfo(token), jsonPath));
            }
        }

        private void ValidateString(JsonSchema schema, string name, JValue scalar, string str, List<Error> errors)
        {
            ValidateDateFormat(schema, name, scalar, str, errors);
            ValidateMicrosoftAlias(schema, name, scalar, str, errors);

            if (schema.MaxLength.HasValue || schema.MinLength.HasValue)
            {
                var unicodeLength = str.Where(c => !char.IsLowSurrogate(c)).Count();
                if (schema.MaxLength.HasValue && unicodeLength > schema.MaxLength.Value)
                {
                    errors.Add(Errors.JsonSchema.StringLengthInvalid(JsonUtility.GetSourceInfo(scalar), name, "long", unicodeLength, $"<= {schema.MaxLength}"));
                }

                if (schema.MinLength.HasValue && unicodeLength < schema.MinLength.Value)
                {
                    errors.Add(
                        Errors.JsonSchema.StringLengthInvalid(JsonUtility.GetSourceInfo(scalar), name, "short", unicodeLength, $">= {schema.MinLength}"));
                }
            }

            if (schema.Pattern != null && !Regex.IsMatch(str, schema.Pattern))
            {
                errors.Add(Errors.JsonSchema.FormatInvalid(JsonUtility.GetSourceInfo(scalar), str, schema.Pattern, name));
            }

            switch (schema.Format)
            {
                case JsonSchemaStringFormat.DateTime:
                    if (!DateTime.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                    {
                        errors.Add(Errors.JsonSchema.FormatInvalid(JsonUtility.GetSourceInfo(scalar), str, JsonSchemaStringFormat.DateTime, name));
                    }
                    break;

                case JsonSchemaStringFormat.Date:
                    if (!DateTime.TryParseExact(str, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                    {
                        errors.Add(Errors.JsonSchema.FormatInvalid(JsonUtility.GetSourceInfo(scalar), str, JsonSchemaStringFormat.Date, name));
                    }
                    break;

                case JsonSchemaStringFormat.Time:
                    if (!DateTime.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.NoCurrentDateDefault, out var time) || time.Date != default)
                    {
                        errors.Add(Errors.JsonSchema.FormatInvalid(JsonUtility.GetSourceInfo(scalar), str, JsonSchemaStringFormat.Time, name));
                    }
                    break;
            }
        }

        private static void ValidateNumber(JsonSchema schema, string name, JValue scalar, double number, List<Error> errors)
        {
            if (schema.Maximum.HasValue && number > schema.Maximum)
            {
                errors.Add(Errors.JsonSchema.NumberInvalid(JsonUtility.GetSourceInfo(scalar), number, $"<= {schema.Maximum}", name));
            }

            if (schema.Minimum.HasValue && number < schema.Minimum)
            {
                errors.Add(Errors.JsonSchema.NumberInvalid(JsonUtility.GetSourceInfo(scalar), number, $">= {schema.Minimum}", name));
            }

            if (schema.ExclusiveMaximum.HasValue && number >= schema.ExclusiveMaximum)
            {
                errors.Add(Errors.JsonSchema.NumberInvalid(JsonUtility.GetSourceInfo(scalar), number, $"< {schema.ExclusiveMaximum}", name));
            }

            if (schema.ExclusiveMinimum.HasValue && number <= schema.ExclusiveMinimum)
            {
                errors.Add(Errors.JsonSchema.NumberInvalid(JsonUtility.GetSourceInfo(scalar), number, $"> {schema.ExclusiveMinimum}", name));
            }

            if (schema.MultipleOf != 0)
            {
                var n = number / schema.MultipleOf;
                if (Math.Abs(n - Math.Floor(n)) > double.Epsilon)
                {
                    errors.Add(Errors.JsonSchema.NumberInvalid(JsonUtility.GetSourceInfo(scalar), number, $"multiple of {schema.MultipleOf}", name));
                }
            }
        }

        private static void ValidateConst(JsonSchema schema, string jsonPath, JToken token, List<Error> errors)
        {
            if (schema.Const != null && !JsonUtility.DeepEqualsComparer.Equals(schema.Const, token))
            {
                errors.Add(Errors.JsonSchema.InvalidValue(JsonUtility.GetSourceInfo(token), jsonPath, token));
            }
        }

        private static void ValidateEnum(JsonSchema schema, string jsonPath, JToken token, List<Error> errors)
        {
            if (schema.Enum != null && !schema.Enum.Contains(token, JsonUtility.DeepEqualsComparer))
            {
                errors.Add(Errors.JsonSchema.InvalidValue(JsonUtility.GetSourceInfo(token), jsonPath, token));
            }
        }

        private void ValidateDependencies(JsonSchema schema, string jsonPath, JObject map, List<Error> errors)
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
                                errors.Add(Errors.JsonSchema.MissingPairedAttribute(
                                    JsonUtility.GetSourceInfo(map), string.IsNullOrWhiteSpace(jsonPath) ? $"{key}" : jsonPath += $".{key}", otherKey));
                            }
                        }
                    }
                    else if (subschema != null)
                    {
                        Validate(subschema, jsonPath, map, errors);
                    }
                }
            }
        }

        private static void ValidateRequired(JsonSchema schema, string jsonPath, JObject map, List<Error> errors)
        {
            foreach (var key in schema.Required)
            {
                if (!map.ContainsKey(key))
                {
                    errors.Add(Errors.JsonSchema.MissingAttribute(
                        JsonUtility.GetSourceInfo(map), string.IsNullOrWhiteSpace(jsonPath) ? $"{key}" : jsonPath += $".{key}"));
                }
            }
        }

        private static void ValidateStrictRequired(JsonSchema schema, string jsonPath, JObject map, List<Error> errors)
        {
            foreach (var key in schema.StrictRequired)
            {
                if (!IsStrictContain(map, key))
                {
                    errors.Add(Errors.JsonSchema.MissingAttribute(
                        JsonUtility.GetSourceInfo(map), string.IsNullOrWhiteSpace(jsonPath) ? $"{key}" : jsonPath += $".{key}"));
                }
            }
        }

        private static bool IsStrictHaveValue(JToken value)
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

        private static bool IsStrictContain(JObject map, string key) =>
            map.TryGetValue(key, out var value) && IsStrictHaveValue(value);

        private static void ValidateEither(JsonSchema schema, string jsonPath, JObject map, List<Error> errors)
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
                    errors.Add(Errors.JsonSchema.MissingEitherAttribute(
                        JsonUtility.GetSourceInfo(map), keys, string.IsNullOrWhiteSpace(jsonPath) ? $"{keys[0]}" : jsonPath += $".{keys[0]}"));
                }
            }
        }

        private static void ValidatePrecludes(JsonSchema schema, string jsonPath, JObject map, List<Error> errors)
        {
            foreach (var keys in schema.Precludes)
            {
                var existNum = 0;
                foreach (var key in keys)
                {
                    if (IsStrictContain(map, key) && ++existNum > 1)
                    {
                        errors.Add(Errors.JsonSchema.PrecludedAttributes(
                            JsonUtility.GetSourceInfo(map), keys, string.IsNullOrWhiteSpace(jsonPath) ? $"{keys[0]}" : jsonPath += $".{keys[0]}"));
                        break;
                    }
                }
            }
        }

        private static void ValidateDateFormat(JsonSchema schema, string name, JValue scalar, string dateString, List<Error> errors)
        {
            if (!string.IsNullOrEmpty(schema.DateFormat) && !string.IsNullOrWhiteSpace(dateString))
            {
                if (DateTime.TryParseExact(dateString, schema.DateFormat, null, DateTimeStyles.None, out var date))
                {
                    ValidateDateRange(schema, name, scalar, date, dateString, errors);
                }
                else
                {
                    errors.Add(Errors.JsonSchema.DateFormatInvalid(JsonUtility.GetSourceInfo(scalar), name, dateString));
                }
            }
        }

        private void ValidateMicrosoftAlias(JsonSchema schema, string name, JValue scalar, string alias, List<Error> errors)
        {
            if (schema.MicrosoftAlias != null && !string.IsNullOrWhiteSpace(alias))
            {
                if (Array.IndexOf(schema.MicrosoftAlias.AllowedDLs, alias) == -1)
                {
                    if (_microsoftGraphAccessor != null)
                    {
                        var error = _microsoftGraphAccessor.ValidateMicrosoftAlias(new SourceInfo<string>(alias, JsonUtility.GetSourceInfo(scalar)), name);
                        if (error != null)
                        {
                            errors.Add(error);
                        }
                    }
                }
            }
        }

        private static void ValidateDateRange(JsonSchema schema, string name, JValue scalar, DateTime date, string dateString, List<Error> errors)
        {
            var diff = date - DateTime.UtcNow;

            if ((schema.RelativeMinDate.HasValue && diff < schema.RelativeMinDate) || (schema.RelativeMaxDate.HasValue && diff > schema.RelativeMaxDate))
            {
                errors.Add(Errors.JsonSchema.DateOutOfRange(JsonUtility.GetSourceInfo(scalar), name, dateString));
            }
        }

        private static void ValidateDeprecated(JsonSchema schema, string jsonPath, JToken token, List<Error> errors)
        {
            if (IsStrictHaveValue(token) && schema.ReplacedBy != null)
            {
                errors.Add(Errors.JsonSchema.AttributeDeprecated(JsonUtility.GetSourceInfo(token), jsonPath, schema.ReplacedBy));
            }
        }

        private void ValidateDocsetUnique(JsonSchema schema, JObject map)
        {
            foreach (var docsetUniqueKey in schema.DocsetUnique)
            {
                if (map.TryGetValue(docsetUniqueKey, out var value))
                {
                    if (_schema.Rules.TryGetValue(docsetUniqueKey, out var customRules) &&
                        customRules.TryGetValue(Errors.JsonSchema.DuplicateAttributeCode, out var customRule) &&
                        _ext != null &&
                        t_filePath.Value != null &&
                        !_ext.IsEnable(t_filePath.Value, customRule))
                    {
                        continue;
                    }
                    else
                    {
                        _metadataBuilder.Add((schema, docsetUniqueKey, value, JsonUtility.GetSourceInfo(value)));
                    }
                }
            }
        }

        private void PostValidateDocsetUnique(List<Error> errors)
        {
            var validatedMetadata = _metadataBuilder.AsList();
            var validatedMetadataGroups = validatedMetadata
                .Where(k => IsStrictHaveValue(k.value))
                .GroupBy(
                    k => (k.value, (k.key, k.schema)),
                    ValueTupleEqualityComparer.Create(JsonUtility.DeepEqualsComparer, EqualityComparer<(string, JsonSchema)>.Default));

            foreach (var group in validatedMetadataGroups)
            {
                IEnumerable<(JsonSchema schema, string key, JToken value, SourceInfo? source)> items = group;
                var (metadataValue, (metadataKey, _)) = group.Key;

                if (items.Count() > 1)
                {
                    var metadataSources = (from g in items where g.source != null select g.source).ToArray();
                    foreach (var file in items)
                    {
                        errors.Add(Errors.JsonSchema.DuplicateAttribute(file.source, metadataKey, metadataValue, metadataSources));
                    }
                }
            }
        }

        private void ValidateEnumDependencies(
            EnumDependenciesSchema? enumDependencies,
            string dependentFieldNameWithIndex,
            string dependentFieldName,
            JToken? dependentFieldRawValue,
            JToken? dependentFieldValue,
            JObject map,
            List<Error> errors)
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
                            errors.Add(Errors.JsonSchema.InvalidValue(
                                JsonUtility.GetSourceInfo(fieldValue),
                                fieldRawValue.Type == JTokenType.Array ? fieldNameWithIndex : fieldName,
                                fieldValue,
                                fieldName));
                        }
                        else
                        {
                            errors.Add(Errors.JsonSchema.InvalidPairedAttribute(
                                JsonUtility.GetSourceInfo(fieldValue),
                                fieldRawValue.Type == JTokenType.Array ? fieldNameWithIndex : fieldName,
                                fieldValue,
                                dependentFieldRawValue?.Type == JTokenType.Array ? dependentFieldNameWithIndex : dependentFieldName,
                                dependentFieldValue,
                                dependentFieldName));
                        }
                    }
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(dependentFieldNameWithIndex) && allowList.Keys.All(k => IsStrictHaveValue(k)))
                    {
                        errors.Add(Errors.JsonSchema.InvalidPairedAttribute(
                            JsonUtility.GetSourceInfo(map),
                            fieldName,
                            fieldName,
                            dependentFieldRawValue?.Type == JTokenType.Array ? dependentFieldNameWithIndex : dependentFieldName,
                            dependentFieldValue,
                            dependentFieldName));
                    }
                }
            }
        }

        // For string type: name = 'topic', output = ('topic', 0) or name = 'topic[0]', output = ('topic', 0)
        // For array type: name = 'topic[0]', output = ('topic', 0) or name = 'topic[1]', output = ('topic', 1) or ...
        private static (string, int) GetFieldNameAndIndex(string name)
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

        private Error GetError(JsonSchema schema, Error error)
        {
            if (_forceError)
            {
                error = error.WithLevel(ErrorLevel.Error);
            }

            if (!string.IsNullOrEmpty(error.Name) &&
                schema.Rules.TryGetValue(error.Name, out var attributeCustomRules) &&
                attributeCustomRules.TryGetValue(error.Code, out var customRule))
            {
                return error.WithCustomRule(customRule, t_filePath.Value == null ? null : _ext?.IsEnable(t_filePath.Value, customRule));
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
