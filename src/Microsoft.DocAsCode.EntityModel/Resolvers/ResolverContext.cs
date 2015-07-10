namespace Microsoft.DocAsCode.EntityModel
{
    using System.Collections.Generic;

    public class ResolverContext
    {
        public string ApiFolder { get; set; }

        public bool PreserveRawInlineComments { get; set; }

        public Dictionary<string, ReferenceItem> References { get; set; }

        public List<ExternalReferencePackageReader> ExternalReferences { get; set; }
    }
}
