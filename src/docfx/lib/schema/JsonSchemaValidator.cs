// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Globalization;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build;

internal class JsonSchemaValidator
{
    private readonly bool _forceError;
    private readonly JsonSchema _schema;
    private readonly MicrosoftGraphAccessor? _microsoftGraphAccessor;
    private readonly MonikerProvider? _monikerProvider;
    private readonly CustomRuleProvider? _customRuleProvider;

    private readonly Scoped<ListBuilder<(JsonSchema schema, string key, string moniker, JToken value, SourceInfo? source)>> _metadataBuilder = new();

    private static readonly ThreadLocal<FilePath?> s_filePath = new();

    public JsonSchema Schema => _schema;

    public JsonSchemaValidator(
        JsonSchema schema,
        MicrosoftGraphAccessor? microsoftGraphAccessor = null,
        MonikerProvider? monikerProvider = null,
        bool forceError = false,
        CustomRuleProvider? customRuleProvider = null)
    {
        _schema = schema;
        _forceError = forceError;
        _microsoftGraphAccessor = microsoftGraphAccessor;
        _monikerProvider = monikerProvider;
        _customRuleProvider = customRuleProvider;
    }

    public List<Error> Validate(JToken token, FilePath filePath, JsonSchemaMap? schemaMap = null)
    {
        try
        {
            if (filePath != null)
            {
                s_filePath.Value = filePath;
            }
            return Validate(_schema, token, schemaMap);
        }
        finally
        {
            s_filePath.Value = null;
        }
    }

    public List<Error> PostValidate()
    {
        var errors = new List<Error>();
        PostValidateDocsetUnique(errors);
        return errors.Select(e => GetError(_schema, e)).ToList();
    }

    private List<Error> Validate(JsonSchema schema, JToken token, JsonSchemaMap? schemaMap)
    {
        var errors = new List<Error>();
        Validate(schema, "", token, errors, schemaMap);
        return errors.Select(error => GetError(_schema, error)).ToList();
    }

    private void Validate(JsonSchema schema, string propertyPath, JToken token, List<Error> errors, JsonSchemaMap? schemaMap)
    {
        schema = schema.SchemaResolver.ResolveSchema(schema) ?? schema;

        if (!ValidateType(schema, propertyPath, token, errors))
        {
            return;
        }

        ValidateBooleanSchema(schema, propertyPath, token, errors);
        ValidateDeprecated(schema, propertyPath, token, errors);
        ValidateConst(schema, propertyPath, token, errors);
        ValidateEnum(schema, propertyPath, token, errors);

        switch (token)
        {
            case JValue scalar:
                ValidateScalar(schema, propertyPath, scalar, errors);
                break;

            case JArray array:
                ValidateArray(schema, propertyPath, array, errors, schemaMap);
                break;

            case JObject map:
                ValidateObject(schema, propertyPath, map, errors, schemaMap);
                break;
        }

        ValidateAnyOf(schema, propertyPath, token, errors, schemaMap);
        ValidateAllOf(schema, propertyPath, token, errors, schemaMap);
        ValidateOneOf(schema, propertyPath, token, errors, schemaMap);
        ValidateIfThenElse(schema, propertyPath, token, errors, schemaMap);
        ValidateNot(schema, propertyPath, token, errors);
    }

    private static bool ValidateType(JsonSchema schema, string propertyPath, JToken token, List<Error> errors)
    {
        if (schema.Type != null)
        {
            if (!schema.Type.Any(schemaType => TypeMatches(schemaType, token)))
            {
                errors.Add(Errors.JsonSchema.UnexpectedType(
                    JsonUtility.GetSourceInfo(token), string.Join(", ", schema.Type), token.Type.ToString(), propertyPath));
                return false;
            }
        }
        return true;
    }

    private void ValidateScalar(JsonSchema schema, string propertyPath, JValue scalar, List<Error> errors)
    {
        switch (scalar.Value)
        {
            case string str:
                ValidateString(schema, propertyPath, scalar, str, errors);
                break;

            case double:
            case float:
            case long:
                ValidateNumber(schema, propertyPath, scalar, Convert.ToDouble(scalar.Value), errors);
                break;
        }
    }

    private void ValidateArray(JsonSchema schema, string propertyPath, JArray array, List<Error> errors, JsonSchemaMap? schemaMap)
    {
        if (schema.MaxItems.HasValue && array.Count > schema.MaxItems.Value)
        {
            errors.Add(Errors.JsonSchema.ArrayLengthInvalid(JsonUtility.GetSourceInfo(array), propertyPath, $"<= {schema.MaxItems}"));
        }

        if (schema.MinItems.HasValue && array.Count < schema.MinItems.Value)
        {
            errors.Add(Errors.JsonSchema.ArrayLengthInvalid(JsonUtility.GetSourceInfo(array), propertyPath, $">= {schema.MinItems}"));
        }

        ValidateItems(schema, propertyPath, array, errors, schemaMap);
        ValidateMinItemsWhen(schema, propertyPath, array, errors);
        ValidateMaxItemsWhen(schema, propertyPath, array, errors);

        if (schema.UniqueItems && array.Distinct(JsonUtility.DeepEqualsComparer).Count() != array.Count)
        {
            errors.Add(Errors.JsonSchema.ArrayNotUnique(JsonUtility.GetSourceInfo(array), propertyPath));
        }

        if (schema.Contains != null && !array.Any(item => SchemaMatches(schema.Contains, item)))
        {
            errors.Add(Errors.JsonSchema.ArrayContainsFailed(JsonUtility.GetSourceInfo(array), propertyPath));
        }
    }

    private void ValidateItems(JsonSchema schema, string propertyPath, JArray array, List<Error> errors, JsonSchemaMap? schemaMap)
    {
        var (allItems, eachItem) = schema.Items;

        if (allItems != null)
        {
            foreach (var item in array)
            {
                Validate(allItems, propertyPath, item, errors, schemaMap);
            }
        }
        else if (eachItem != null)
        {
            for (var i = 0; i < array.Count; i++)
            {
                if (i < eachItem.Length)
                {
                    Validate(eachItem[i], propertyPath, array[i], errors, schemaMap);
                }
                else if (schema.AdditionalItems == JsonSchema.FalseSchema)
                {
                    errors.Add(Errors.JsonSchema.ArrayLengthInvalid(JsonUtility.GetSourceInfo(array), propertyPath, $"<= {eachItem.Length}"));
                    break;
                }
                else if (schema.AdditionalItems != null && schema.AdditionalItems != JsonSchema.FalseSchema)
                {
                    Validate(schema.AdditionalItems, propertyPath, array[i], errors, schemaMap);
                }
            }
        }
    }

    private void ValidateObject(JsonSchema schema, string propertyPath, JObject map, List<Error> errors, JsonSchemaMap? schemaMap)
    {
        ValidateRequired(schema, propertyPath, map, errors);
        ValidateStrictRequired(schema, propertyPath, map, errors);
        ValidateDependentSchemas(schema, propertyPath, map, errors, schemaMap);
        ValidateEither(schema, propertyPath, map, errors);
        ValidatePrecludes(schema, propertyPath, map, errors);
        ValidateEnumDependencies(schema.EnumDependencies, propertyPath, "", "", null, null, map, errors);
        ValidateDocsetUnique(schema, propertyPath, map);
        ValidateProperties(schema, propertyPath, map, errors, schemaMap);
    }

    private void ValidateProperties(JsonSchema schema, string propertyPath, JObject map, List<Error> errors, JsonSchemaMap? schemaMap)
    {
        if (schema.MaxProperties.HasValue && map.Count > schema.MaxProperties.Value)
        {
            errors.Add(Errors.JsonSchema.PropertyCountInvalid(JsonUtility.GetSourceInfo(map), propertyPath, $"<= {schema.MaxProperties}"));
        }

        if (schema.MinProperties.HasValue && map.Count < schema.MinProperties.Value)
        {
            errors.Add(Errors.JsonSchema.PropertyCountInvalid(JsonUtility.GetSourceInfo(map), propertyPath, $">= {schema.MinProperties}"));
        }

        foreach (var (key, value) in map)
        {
            if (value is null)
            {
                continue;
            }

            var currentPropertyPath = JsonUtility.AddToPropertyPath(propertyPath, key);

            if (schema.PropertyNames != null)
            {
                var propertyName = new JValue(key);
                JsonUtility.SetSourceInfo(propertyName, JsonUtility.GetKeySourceInfo(value));
                Validate(schema.PropertyNames, currentPropertyPath, propertyName, errors, schemaMap);
            }

            var isAdditionalProperty = true;

            // properties
            if (schema.Properties.TryGetValue(key, out var propertySchema))
            {
                Validate(propertySchema, currentPropertyPath, value, errors, schemaMap);
                isAdditionalProperty = false;
            }

            // patternProperties
            foreach (var (pattern, patternPropertySchema) in schema.PatternProperties)
            {
                if (Regex.IsMatch(key, pattern))
                {
                    Validate(patternPropertySchema, currentPropertyPath, value, errors, schemaMap);
                    isAdditionalProperty = false;
                }
            }

            // additionalProperties
            if (isAdditionalProperty && schema.AdditionalProperties != null)
            {
                if (schema.AdditionalProperties == JsonSchema.FalseSchema)
                {
                    errors.Add(Errors.JsonSchema.UnknownField(JsonUtility.GetSourceInfo(value), currentPropertyPath, value.Type.ToString()));
                }
                else if (schema.AdditionalProperties != JsonSchema.TrueSchema)
                {
                    Validate(schema.AdditionalProperties, propertyPath, value, errors, schemaMap);
                }
            }
        }
    }

    private void ValidateMaxItemsWhen(JsonSchema schema, string propertyPath, JArray array, List<Error> errors)
    {
        foreach (var check in schema.MaxItemsWhen)
        {
            var count = 0;
            for (var i = 0; i < array.Count; i++)
            {
                if (check.Condition != null && SchemaMatches(check.Condition, array[i]))
                {
                    count++;
                }
            }

            if (count > check.Value)
            {
                errors.Add(Errors.JsonSchema.ArrayMaxCheckInvalid(JsonUtility.GetSourceInfo(array), propertyPath, check.Value));
            }
        }
    }

    private void ValidateMinItemsWhen(JsonSchema schema, string propertyPath, JArray array, List<Error> errors)
    {
        foreach (var check in schema.MinItemsWhen)
        {
            var count = 0;
            for (var i = 0; i < array.Count; i++)
            {
                if (check.Condition != null && SchemaMatches(check.Condition, array[i]))
                {
                    count++;
                }
            }

            if (count < check.Value)
            {
                errors.Add(Errors.JsonSchema.ArrayMinCheckInvalid(JsonUtility.GetSourceInfo(array), propertyPath, check.Value));
            }
        }
    }

    private bool SchemaMatches(JsonSchema schema, JToken map)
    {
        return Validate(schema, map, schemaMap: null).Count <= 0;
    }

    private static void ValidateBooleanSchema(JsonSchema schema, string propertyPath, JToken token, List<Error> errors)
    {
        if (schema == JsonSchema.FalseSchema)
        {
            errors.Add(Errors.JsonSchema.BooleanSchemaFailed(JsonUtility.GetSourceInfo(token), propertyPath));
        }
    }

    private void ValidateString(JsonSchema schema, string propertyPath, JValue scalar, string str, List<Error> errors)
    {
        ValidateDateFormat(schema, propertyPath, scalar, str, errors);
        ValidateMicrosoftAlias(schema, propertyPath, scalar, str, errors);

        if (schema.MaxLength.HasValue || schema.MinLength.HasValue)
        {
            var unicodeLength = str.Count(c => !char.IsLowSurrogate(c));
            if (schema.MaxLength.HasValue && unicodeLength > schema.MaxLength.Value)
            {
                errors.Add(Errors.JsonSchema.StringLengthInvalid(
                    JsonUtility.GetSourceInfo(scalar), propertyPath, "long", unicodeLength, $"<= {schema.MaxLength}"));
            }

            if (schema.MinLength.HasValue && unicodeLength < schema.MinLength.Value)
            {
                errors.Add(
                    Errors.JsonSchema.StringLengthInvalid(
                        JsonUtility.GetSourceInfo(scalar), propertyPath, "short", unicodeLength, $">= {schema.MinLength}"));
            }
        }

        if (schema.Pattern != null && !Regex.IsMatch(str, schema.Pattern))
        {
            errors.Add(Errors.JsonSchema.FormatInvalid(JsonUtility.GetSourceInfo(scalar), str, schema.Pattern, propertyPath));
        }

        switch (schema.Format)
        {
            case JsonSchemaStringFormat.DateTime:
                if (!DateTime.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                {
                    errors.Add(Errors.JsonSchema.FormatInvalid(JsonUtility.GetSourceInfo(scalar), str, JsonSchemaStringFormat.DateTime, propertyPath));
                }
                break;

            case JsonSchemaStringFormat.Date:
                if (!DateTime.TryParseExact(str, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                {
                    errors.Add(Errors.JsonSchema.FormatInvalid(JsonUtility.GetSourceInfo(scalar), str, JsonSchemaStringFormat.Date, propertyPath));
                }
                break;

            case JsonSchemaStringFormat.Time:
                if (!DateTime.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.NoCurrentDateDefault, out var time) || time.Date != default)
                {
                    errors.Add(Errors.JsonSchema.FormatInvalid(JsonUtility.GetSourceInfo(scalar), str, JsonSchemaStringFormat.Time, propertyPath));
                }
                break;
        }
    }

    private static void ValidateNumber(JsonSchema schema, string propertyPath, JValue scalar, double number, List<Error> errors)
    {
        if (schema.Maximum.HasValue && number > schema.Maximum)
        {
            errors.Add(Errors.JsonSchema.NumberInvalid(JsonUtility.GetSourceInfo(scalar), number, $"<= {schema.Maximum}", propertyPath));
        }

        if (schema.Minimum.HasValue && number < schema.Minimum)
        {
            errors.Add(Errors.JsonSchema.NumberInvalid(JsonUtility.GetSourceInfo(scalar), number, $">= {schema.Minimum}", propertyPath));
        }

        if (schema.ExclusiveMaximum.HasValue && number >= schema.ExclusiveMaximum)
        {
            errors.Add(Errors.JsonSchema.NumberInvalid(JsonUtility.GetSourceInfo(scalar), number, $"< {schema.ExclusiveMaximum}", propertyPath));
        }

        if (schema.ExclusiveMinimum.HasValue && number <= schema.ExclusiveMinimum)
        {
            errors.Add(Errors.JsonSchema.NumberInvalid(JsonUtility.GetSourceInfo(scalar), number, $"> {schema.ExclusiveMinimum}", propertyPath));
        }

        if (schema.MultipleOf != 0)
        {
            var n = number / schema.MultipleOf;
            if (Math.Abs(n - Math.Floor(n)) > double.Epsilon)
            {
                errors.Add(Errors.JsonSchema.NumberInvalid(JsonUtility.GetSourceInfo(scalar), number, $"multiple of {schema.MultipleOf}", propertyPath));
            }
        }
    }

    private static void ValidateConst(JsonSchema schema, string propertyPath, JToken token, List<Error> errors)
    {
        if (schema.Const != null && !JsonUtility.DeepEqualsComparer.Equals(schema.Const, token))
        {
            errors.Add(Errors.JsonSchema.InvalidValue(JsonUtility.GetSourceInfo(token), propertyPath, token));
        }
    }

    private static void ValidateEnum(JsonSchema schema, string propertyPath, JToken token, List<Error> errors)
    {
        if (schema.Enum != null)
        {
            if (string.Equals("tasks.azure.resource.type", propertyPath, StringComparison.OrdinalIgnoreCase) && token.Type == JTokenType.String)
            {
                if (!schema.Enum.Any(
                    item => item.Type == JTokenType.String && string.Equals(item.ToString(), token.ToString(), StringComparison.OrdinalIgnoreCase)))
                {
                    errors.Add(Errors.JsonSchema.InvalidValue(JsonUtility.GetSourceInfo(token), propertyPath, token));
                }
            }
            else if (!schema.Enum.Contains(token, JsonUtility.DeepEqualsComparer))
            {
                errors.Add(Errors.JsonSchema.InvalidValue(JsonUtility.GetSourceInfo(token), propertyPath, token));
            }
        }
    }

    private void ValidateDependentSchemas(JsonSchema schema, string propertyPath, JObject map, List<Error> errors, JsonSchemaMap? schemaMap)
    {
        foreach (var (key, (propertyNames, subschema)) in schema.DependentSchemas.Concat(schema.Dependencies))
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
                                JsonUtility.GetSourceInfo(map),
                                JsonUtility.AddToPropertyPath(propertyPath, key),
                                JsonUtility.AddToPropertyPath(propertyPath, otherKey)));
                        }
                    }
                }
                else if (subschema != null)
                {
                    var subschemaErrors = new List<Error>();
                    Validate(subschema, propertyPath, map, subschemaErrors, schemaMap);
                    if (subschemaErrors.Count <= 0)
                    {
                        continue;
                    }

                    errors.Add(Errors.JsonSchema.DependentSchemasFailed(
                        JsonUtility.GetSourceInfo(map[key] ?? map),
                        JsonUtility.AddToPropertyPath(propertyPath, key)));
                }
            }
        }
    }

    private static void ValidateRequired(JsonSchema schema, string propertyPath, JObject map, List<Error> errors)
    {
        foreach (var key in schema.Required)
        {
            if (!map.ContainsKey(key))
            {
                errors.Add(Errors.JsonSchema.MissingAttribute(JsonUtility.GetSourceInfo(map), JsonUtility.AddToPropertyPath(propertyPath, key)));
            }
        }
    }

    private static void ValidateStrictRequired(JsonSchema schema, string propertyPath, JObject map, List<Error> errors)
    {
        foreach (var key in schema.StrictRequired)
        {
            if (!IsStrictContain(map, key))
            {
                errors.Add(Errors.JsonSchema.MissingAttribute(JsonUtility.GetSourceInfo(map), JsonUtility.AddToPropertyPath(propertyPath, key)));
            }
        }
    }

    private static bool IsStrictHaveValue(JToken value)
    {
        return value switch
        {
            JObject => true,
            JArray => true,
            JValue v when v.Value is null => false,
            JValue v when v.Value is string str => !string.IsNullOrWhiteSpace(str),
            JValue => true,
            _ => false,
        };
    }

    private static bool IsStrictContain(JObject map, string key) =>
        map.TryGetValue(key, out var value) && IsStrictHaveValue(value);

    private static void ValidateEither(JsonSchema schema, string propertyPath, JObject map, List<Error> errors)
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
                errors.Add(
                    Errors.JsonSchema.MissingEitherAttribute(JsonUtility.GetSourceInfo(map), keys, JsonUtility.AddToPropertyPath(propertyPath, keys[0])));
            }
        }
    }

    private static void ValidatePrecludes(JsonSchema schema, string propertyPath, JObject map, List<Error> errors)
    {
        foreach (var keys in schema.Precludes)
        {
            var existNum = 0;
            foreach (var key in keys)
            {
                if (IsStrictContain(map, key) && ++existNum > 1)
                {
                    errors.Add(
                        Errors.JsonSchema.PrecludedAttributes(JsonUtility.GetSourceInfo(map), keys, JsonUtility.AddToPropertyPath(propertyPath, keys[0])));
                    break;
                }
            }
        }
    }

    private static void ValidateDateFormat(JsonSchema schema, string propertyPath, JValue scalar, string dateString, List<Error> errors)
    {
        if (!string.IsNullOrEmpty(schema.DateFormat) && !string.IsNullOrWhiteSpace(dateString))
        {
            if (DateTime.TryParseExact(dateString, schema.DateFormat, null, DateTimeStyles.None, out var date))
            {
                ValidateDateRange(schema, propertyPath, scalar, date, dateString, errors);
            }
            else
            {
                errors.Add(Errors.JsonSchema.DateFormatInvalid(JsonUtility.GetSourceInfo(scalar), propertyPath, dateString));
            }
        }
    }

    private void ValidateMicrosoftAlias(JsonSchema schema, string propertyPath, JValue scalar, string alias, List<Error> errors)
    {
        if (schema.MicrosoftAlias != null && !string.IsNullOrWhiteSpace(alias))
        {
            if (Array.IndexOf(schema.MicrosoftAlias.AllowedDLs, alias) == -1)
            {
                if (_microsoftGraphAccessor != null)
                {
                    var error = _microsoftGraphAccessor.ValidateMicrosoftAlias(
                        new SourceInfo<string>(alias, JsonUtility.GetSourceInfo(scalar)), propertyPath);
                    if (error != null)
                    {
                        errors.Add(error);
                    }
                }
            }
        }
    }

    private static void ValidateDateRange(JsonSchema schema, string propertyPath, JValue scalar, DateTime date, string dateString, List<Error> errors)
    {
        var diff = date - DateTime.UtcNow;

        if ((schema.RelativeMinDate.HasValue && diff < schema.RelativeMinDate) || (schema.RelativeMaxDate.HasValue && diff > schema.RelativeMaxDate))
        {
            errors.Add(Errors.JsonSchema.DateOutOfRange(JsonUtility.GetSourceInfo(scalar), propertyPath, dateString));
        }
    }

    private static void ValidateDeprecated(JsonSchema schema, string propertyPath, JToken token, List<Error> errors)
    {
        if (IsStrictHaveValue(token) && schema.ReplacedBy != null)
        {
            errors.Add(Errors.JsonSchema.AttributeDeprecated(JsonUtility.GetSourceInfo(token), propertyPath, schema.ReplacedBy));
        }
    }

    private void ValidateAnyOf(JsonSchema schema, string propertyPath, JToken token, List<Error> errors, JsonSchemaMap? schemaMap)
    {
        if (schema.AnyOf.Length <= 0)
        {
            return;
        }

        List<Error>? bestErrors = null;

        foreach (var subschema in schema.AnyOf)
        {
            var subschemaErrors = new List<Error>();
            var subschemaMap = schemaMap is null ? null : new JsonSchemaMap();
            Validate(subschema, propertyPath, token, subschemaErrors, subschemaMap);

            if (subschemaErrors.Count <= 0)
            {
                if (schemaMap != null && subschemaMap != null)
                {
                    schemaMap.Add(token, subschema);
                    schemaMap.Add(subschemaMap);
                }
                return;
            }

            // Find the subschema with the least errors
            if (bestErrors is null || subschemaErrors.Count < bestErrors.Count)
            {
                bestErrors = subschemaErrors;
            }
        }

        if (bestErrors != null)
        {
            errors.AddRange(bestErrors);
        }
    }

    private void ValidateAllOf(JsonSchema schema, string propertyPath, JToken token, List<Error> errors, JsonSchemaMap? schemaMap)
    {
        foreach (var subschema in schema.AllOf)
        {
            Validate(subschema, propertyPath, token, errors, schemaMap);
        }
    }

    private void ValidateOneOf(JsonSchema schema, string propertyPath, JToken token, List<Error> errors, JsonSchemaMap? schemaMap)
    {
        if (schema.OneOf.Length <= 0)
        {
            return;
        }

        var validCount = 0;
        JsonSchema? bestSchema = null;
        JsonSchemaMap? bestSchemaMap = null;
        List<Error>? bestErrors = null;

        foreach (var subschema in schema.OneOf)
        {
            var subschemaErrors = new List<Error>();
            var subschemaMap = schemaMap is null ? null : new JsonSchemaMap();
            Validate(subschema, propertyPath, token, subschemaErrors, subschemaMap);

            if (subschemaErrors.Count <= 0)
            {
                bestSchema = subschema;
                bestSchemaMap = subschemaMap;
                validCount++;
                continue;
            }

            // Find the subschema with the least errors
            if (bestErrors is null || subschemaErrors.Count < bestErrors.Count)
            {
                bestErrors = subschemaErrors;
            }
        }

        if (validCount != 1)
        {
            if (bestErrors != null)
            {
                errors.AddRange(bestErrors);
            }
            else
            {
                errors.Add(Errors.JsonSchema.OneOfFailed(JsonUtility.GetSourceInfo(token), propertyPath, token));
            }
        }
        else if (schemaMap != null && bestSchemaMap != null && bestSchema != null)
        {
            schemaMap.Add(token, bestSchema);
            schemaMap.Add(bestSchemaMap);
        }
    }

    private void ValidateIfThenElse(JsonSchema schema, string propertyPath, JToken token, List<Error> errors, JsonSchemaMap? schemaMap)
    {
        if (schema.If is null)
        {
            return;
        }

        var ifErrors = new List<Error>();
        Validate(schema.If, propertyPath, token, ifErrors, schemaMap: null);

        if (ifErrors.Count <= 0)
        {
            if (schema.Then != null)
            {
                Validate(schema.Then, propertyPath, token, errors, schemaMap);
                schemaMap?.Add(token, schema.Then);
            }
        }
        else
        {
            if (schema.Else != null)
            {
                Validate(schema.Else, propertyPath, token, errors, schemaMap);
                schemaMap?.Add(token, schema.Else);
            }
        }
    }

    private void ValidateNot(JsonSchema schema, string propertyPath, JToken token, List<Error> errors)
    {
        if (schema.Not is null)
        {
            return;
        }

        var subschemaErrors = new List<Error>();
        Validate(schema.Not, propertyPath, token, subschemaErrors, schemaMap: null);

        if (subschemaErrors.Count > 0)
        {
            return;
        }

        errors.Add(Errors.JsonSchema.NotFailed(JsonUtility.GetSourceInfo(token), propertyPath, token));
    }

    private void ValidateDocsetUnique(JsonSchema schema, string propertyPath, JObject map)
    {
        var monikers = _monikerProvider?.GetFileLevelMonikers(ErrorBuilder.Null, s_filePath.Value!).ToList();
        if (monikers == null || !monikers.Any())
        {
            // Use empty string as default moniker if content versioning not enabled for this docset
            monikers = new[] { "" }.ToList();
        }

        foreach (var docsetUniqueKey in schema.DocsetUnique)
        {
            if (map.TryGetValue(docsetUniqueKey, out var value))
            {
                foreach (var moniker in monikers)
                {
                    if (_schema.Rules.TryGetValue(docsetUniqueKey, out var customRules) &&
                        customRules.TryGetValue("duplicate-attribute", out var customRule) && // code of Errors.DuplicateAttribute
                        _customRuleProvider != null &&
                        s_filePath.Value != null &&
                        !_customRuleProvider.IsEnable(s_filePath.Value, customRule, moniker))
                    {
                        continue;
                    }

                    var key = JsonUtility.AddToPropertyPath(propertyPath, docsetUniqueKey);
                    var sourceInfo = JsonUtility.GetSourceInfo(value);

                    Watcher.Write(() => _metadataBuilder.Value.Add((schema, key, moniker, value, sourceInfo)));
                }
            }
        }
    }

    private void PostValidateDocsetUnique(List<Error> errors)
    {
        var validatedMetadata = _metadataBuilder.Value.AsList();
        var validatedMetadataGroups = validatedMetadata
            .Where(k => IsStrictHaveValue(k.value))
            .GroupBy(
                k => (k.value, (k.key, k.moniker, k.schema)),
                ValueTupleEqualityComparer.Create(JsonUtility.DeepEqualsComparer, EqualityComparer<(string, string, JsonSchema)>.Default));

        foreach (var group in validatedMetadataGroups)
        {
            var (metadataValue, (metadataKey, moniker, _)) = group.Key;

            if (group.Count() > 1)
            {
                var metadataSources = (from g in @group where g.source != null select g.source).ToArray();
                foreach (var file in group)
                {
                    errors.Add(Errors.JsonSchema.DuplicateAttribute(file.source, metadataKey, metadataValue, metadataSources));
                }
            }
        }
    }

    private void ValidateEnumDependencies(
        EnumDependenciesSchema? enumDependencies,
        string propertyPath,
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
                    ValidateEnumDependencies(
                        nextEnumDependencies,
                        propertyPath,
                        fieldNameWithIndex,
                        fieldName,
                        fieldRawValue,
                        fieldValue,
                        map,
                        errors);
                }
                else
                {
                    if (string.IsNullOrEmpty(dependentFieldNameWithIndex))
                    {
                        errors.Add(Errors.JsonSchema.InvalidValue(
                            JsonUtility.GetSourceInfo(fieldValue),
                            JsonUtility.AddToPropertyPath(propertyPath, fieldRawValue.Type == JTokenType.Array ? fieldNameWithIndex : fieldName),
                            fieldValue,
                            JsonUtility.AddToPropertyPath(propertyPath, fieldName)));
                    }
                    else
                    {
                        errors.Add(Errors.JsonSchema.InvalidPairedAttribute(
                            JsonUtility.GetSourceInfo(fieldValue),
                            JsonUtility.AddToPropertyPath(propertyPath, fieldRawValue.Type == JTokenType.Array ? fieldNameWithIndex : fieldName),
                            fieldValue,
                            JsonUtility.AddToPropertyPath(
                                propertyPath, dependentFieldRawValue?.Type == JTokenType.Array ? dependentFieldNameWithIndex : dependentFieldName),
                            dependentFieldValue,
                            JsonUtility.AddToPropertyPath(propertyPath, dependentFieldName)));
                    }
                }
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(dependentFieldNameWithIndex) && allowList.Keys.All(k => IsStrictHaveValue(k)))
                {
                    errors.Add(Errors.JsonSchema.InvalidPairedAttribute(
                        JsonUtility.GetSourceInfo(map),
                        JsonUtility.AddToPropertyPath(propertyPath, fieldName),
                        fieldName,
                        JsonUtility.AddToPropertyPath(
                            propertyPath, dependentFieldRawValue?.Type == JTokenType.Array ? dependentFieldNameWithIndex : dependentFieldName),
                        dependentFieldValue,
                        JsonUtility.AddToPropertyPath(propertyPath, dependentFieldName)));
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
                if (int.TryParse(match.Value[1..^1], out var index))
                {
                    return (name[..^match.Value.Length], index);
                }
            }
        }

        return (name, 0);
    }

    private Error GetError(JsonSchema schema, Error error)
    {
        if (_forceError)
        {
            error = error with { Level = ErrorLevel.Error };
        }

        if (!string.IsNullOrEmpty(error.PropertyPath) &&
            schema.Rules.TryGetValue(error.PropertyPath, out var attributeCustomRules) && // todo remove schema.Rules to CustomRuleProvider
            attributeCustomRules.TryGetValue(error.Code, out var customRule))
        {
            return CustomRuleProvider.ApplyCustomRule(
                error,
                customRule,
                s_filePath.Value == null ? null : _customRuleProvider?.IsEnable(s_filePath.Value, customRule));
        }

        return error;
    }

    private static bool TypeMatches(JsonSchemaType schemaType, JToken token)
    {
        var tokenType = token.Type;

        return schemaType switch
        {
            JsonSchemaType.Array => tokenType == JTokenType.Array,
            JsonSchemaType.Boolean => tokenType == JTokenType.Boolean,
            JsonSchemaType.Integer => tokenType == JTokenType.Integer || (token is JValue value && value.Value is double d && (long)d == d),
            JsonSchemaType.Null => tokenType == JTokenType.Null,
            JsonSchemaType.Number => tokenType == JTokenType.Integer || tokenType == JTokenType.Float,
            JsonSchemaType.Object => tokenType == JTokenType.Object,
            JsonSchemaType.String => tokenType == JTokenType.String,
            _ => true,
        };
    }
}
