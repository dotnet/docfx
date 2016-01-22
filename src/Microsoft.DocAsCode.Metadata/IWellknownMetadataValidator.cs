namespace Microsoft.DocAsCode.Metadata
{
    using Newtonsoft.Json.Linq;

    public interface IWellknownMetadataValidator
    {
        ValidationResult Validate(string path, IMetadataDefinition definition, JToken value);
    }
}
