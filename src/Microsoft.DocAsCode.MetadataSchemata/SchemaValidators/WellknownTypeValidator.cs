// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MetadataSchemata.SchemaValidators
{
    using Newtonsoft.Json.Linq;

    public class WellknownTypeValidator : IWellknownMetadataValidator
    {
        public ValidationResult Validate(string path, IMetadataDefinition definition, JToken value)
        {
            switch (definition.Type)
            {
                case "string":
                    if (definition.IsMultiValued)
                    {
                        return ValidateMultiValue(path, definition, value, JTokenType.String);
                    }
                    return ValidateSimpleValue(path, definition, value, JTokenType.String);
                case "integer":
                    if (definition.IsMultiValued)
                    {
                        return ValidateMultiValue(path, definition, value, JTokenType.String);
                    }
                    return ValidateSimpleValue(path, definition, value, JTokenType.Integer);
                case "float":
                    if (definition.IsMultiValued)
                    {
                        return ValidateMultiValue(path, definition, value, JTokenType.String);
                    }
                    return ValidateSimpleValue(path, definition, value, JTokenType.Float);
                case "boolean":
                    if (definition.IsMultiValued)
                    {
                        return ValidateMultiValue(path, definition, value, JTokenType.String);
                    }
                    return ValidateSimpleValue(path, definition, value, JTokenType.Boolean);
                default:
                    return ValidationResult.Fail(ValidationErrorCodes.Schema.BadSchema, $"Bad schema: unknown type '{definition.Type}' for property {path}.", path);
            }
        }

        private ValidationResult ValidateMultiValue(string path, IMetadataDefinition definition, JToken token, JTokenType expectedType)
        {
            if (token.Type == JTokenType.Array)
            {
                var array = (JArray)token;
                int index = 0;
                foreach (var item in array)
                {
                    if (item.Type != expectedType)
                    {
                        return ValidationResult.Fail(ValidationErrorCodes.WellknownMetadata.UnexpectedItemType, $"Bad metadata: unexpected type '{token.Type.ToString()}' for property {path}[{index}], expected type '{expectedType.ToString()}'.", path);
                    }
                }
                return ValidationResult.Success;
            }
            else if (token.Type == JTokenType.Null)
            {
                if (definition.IsRequired)
                {
                    return ValidationResult.Fail(ValidationErrorCodes.WellknownMetadata.FieldRequired, $"Bad metadata: property {path} is required.", path);
                }
                return ValidationResult.Success;
            }
            return ValidationResult.Fail(ValidationErrorCodes.WellknownMetadata.UnexpectedType, $"Bad metadata: unexpected type '{token.Type.ToString()}' for property {path}, expected type '{nameof(JTokenType.Array)}'.", path);
        }

        private ValidationResult ValidateSimpleValue(string path, IMetadataDefinition definition, JToken token, JTokenType expectedType)
        {
            if (token.Type == expectedType)
            {
                return ValidationResult.Success;
            }
            if (token.Type == JTokenType.Null)
            {
                if (definition.IsRequired)
                {
                    return ValidationResult.Fail(ValidationErrorCodes.WellknownMetadata.FieldRequired, $"Bad metadata: property {path} is required.", path);
                }
                return ValidationResult.Success;
            }
            return ValidationResult.Fail(ValidationErrorCodes.WellknownMetadata.UnexpectedType, $"Bad metadata: unexpected type '{token.Type.ToString()}' for property {path}, expected type '{expectedType.ToString()}'.", path);
        }
    }
}
