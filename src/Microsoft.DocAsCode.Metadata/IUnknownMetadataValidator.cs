namespace Microsoft.DocAsCode.Metadata
{
    using Newtonsoft.Json.Linq;

    public interface IUnknownMetadataValidator
    {
        ValidationResult Validate(string name, JToken value);
    }
}
