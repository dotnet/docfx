namespace Microsoft.DocAsCode.Metadata.SchemaValidators
{
    using System.Text.RegularExpressions;

    using Newtonsoft.Json.Linq;

    public class UnknownNamingValidator : IUnknownMetadataValidator
    {
        private static readonly Regex Regex = new Regex("^[a-z][a-z0-9_]*$", RegexOptions.Compiled);

        public ValidationResult Validate(string name, JToken value)
        {
            if (Regex.IsMatch(name))
            {
                return ValidationResult.Success;
            }
            else
            {
                return ValidationResult.Fail(ValidationErrorCodes.UnknownMetadata.BadNaming, "Bad naming for unknown metadata {name}.", name);
            }
        }
    }
}
