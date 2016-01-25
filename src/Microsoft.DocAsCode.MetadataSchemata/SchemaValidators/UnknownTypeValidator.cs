// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MetadataSchemata.SchemaValidators
{
    using Newtonsoft.Json.Linq;

    public class UnknownTypeValidator : IUnknownMetadataValidator
    {
        public ValidationResult Validate(string name, JToken value)
        {
            switch (value.Type)
            {
                case JTokenType.Array:
                    return ValidateArray(name, (JArray)value);
                case JTokenType.Integer:
                case JTokenType.Float:
                case JTokenType.String:
                case JTokenType.Boolean:
                case JTokenType.Null:
                    return ValidationResult.Success;
                case JTokenType.Object:
                default:
                    return ValidationResult.Fail(ValidationErrorCodes.UnknownMetadata.UnexpectedType, "Invalid type for property {name}.", name);
            }
        }

        private static ValidationResult ValidateArray(string name, JArray array)
        {
            JTokenType itemType = JTokenType.None;
            int index = 0;
            foreach (var item in array)
            {
                switch (item.Type)
                {
                    case JTokenType.Integer:
                    case JTokenType.Float:
                    case JTokenType.String:
                    case JTokenType.Boolean:
                        if (index == 0)
                        {
                            itemType = item.Type;
                        }
                        else if (itemType != item.Type)
                        {
                            return ValidationResult.Fail(ValidationErrorCodes.UnknownMetadata.UnexpectedType, "Invalid type for property {name}[{index}].", $"{name}[{index}]");
                        }
                        break;
                    default:
                        return ValidationResult.Fail(ValidationErrorCodes.UnknownMetadata.UnexpectedType, "Invalid type for property {name}[{index}].", $"{name}[{index}]");
                }
                index++;
            }
            return ValidationResult.Success;
        }
    }
}
