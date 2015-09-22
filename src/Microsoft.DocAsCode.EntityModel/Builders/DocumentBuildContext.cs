namespace Microsoft.DocAsCode.EntityModel.Builders
{
    using System.Collections.Generic;
    using System.Collections.Immutable;

    internal sealed class DocumentBuildContext
    {
        public DocumentBuildContext(string buildOutputFolder, ImmutableArray<string> externalReferencePackages)
        {
            BuildOutputFolder = buildOutputFolder;
            ExternalReferencePackages = externalReferencePackages;
        }

        public string BuildOutputFolder { get; }

        public ImmutableArray<string> ExternalReferencePackages { get; }

        public Dictionary<string, string> FileMap { get; } = new Dictionary<string, string>();

        public Dictionary<string, string> UidMap { get; } = new Dictionary<string, string>();

        public List<ManifestItem> Manifest { get; } = new List<ManifestItem>();

        public HashSet<string> XRef { get; } = new HashSet<string>();
    }
}
