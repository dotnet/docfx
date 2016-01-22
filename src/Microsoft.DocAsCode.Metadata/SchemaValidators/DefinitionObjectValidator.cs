namespace Microsoft.DocAsCode.Metadata.SchemaValidators
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public class DefinitionObjectValidator : IUnknownMetadataValidator
    {
        public ValidationResult Validate(string name, JToken value)
        {
            if (value.Type != JTokenType.Object)
            {
                ValidationResult.Fail(ValidationErrorCodes.Schema.UnexpectedType, $"Expected metadata object for property {name}.", name);
            }
            MetadataDefinition md;
            try
            {
                md = value.ToObject<MetadataDefinition>();
            }
            catch (JsonException ex)
            {
                return ValidationResult.Fail(ValidationErrorCodes.Schema.BadSchema, $"Bad metadata definition: {ex.Message}", name);
            }
            switch (md.Type)
            {
                case "string":
                case "integer":
                case "float":
                case "boolean":
                    break;
                default:
                    return ValidationResult.Fail(ValidationErrorCodes.Schema.UnexpectedType, $"Expected metadata object for property {name}.type.", name + ".type");
            }
            if (md.IsQueryable)
            {
                if (string.IsNullOrWhiteSpace(md.QueryName))
                {
                    return ValidationResult.Fail(ValidationErrorCodes.Schema.BadSchema, $"Expected metadata object for property {name}.query_name.", name + ".query_name");
                }
            }
            return ValidationResult.Success;
        }
    }
}
