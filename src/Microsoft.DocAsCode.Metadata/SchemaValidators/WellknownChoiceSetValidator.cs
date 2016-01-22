namespace Microsoft.DocAsCode.Metadata.SchemaValidators
{
    using System;
    using Newtonsoft.Json.Linq;

    public class WellknownChoiceSetValidator : IWellknownMetadataValidator
    {
        public ValidationResult Validate(string path, IMetadataDefinition definition, JToken value)
        {
            if (definition.ChoiceSet == null)
            {
                return ValidationResult.Success;
            }
            if (definition.IsMultiValued)
            {
                var array = value as JArray;
                if (array != null)
                {
                    int index = 0;
                    foreach (var item in array)
                    {
                        var vr = ValidateOne($"{path}[{index}]", definition, item as JValue);
                        if (vr != ValidationResult.Success)
                        {
                            return vr;
                        }
                        index++;
                    }
                }
                return ValidationResult.Success;
            }
            return ValidateOne(path, definition, value as JValue);
        }

        private ValidationResult ValidateOne(string path, IMetadataDefinition definition, JValue value)
        {
            if (definition.ChoiceSet.Contains(value))
            {
                return ValidationResult.Success;
            }
            return ValidationResult.Fail(ValidationErrorCodes.WellknownMetadata.UndefinedValue, $"Bad metadata: Value {value.ToString()} is undefined for {path}.", path);
        }
    }
}
