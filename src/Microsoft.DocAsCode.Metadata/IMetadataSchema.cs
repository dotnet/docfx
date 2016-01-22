namespace Microsoft.DocAsCode.Metadata
{
    using System.Collections.Generic;
    using System.IO;

    public interface IMetadataSchema
    {
        IReadOnlyDictionary<string, IMetadataDefinition> Definitions { get; }
        ValidationResults ValidateSchema();
        ValidationResults ValidateMetadata(string metadata);
    }
}
