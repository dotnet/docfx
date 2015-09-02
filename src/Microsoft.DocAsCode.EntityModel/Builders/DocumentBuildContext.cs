namespace Microsoft.DocAsCode.EntityModel.Builders
{
    using System.Collections.Generic;

    internal sealed class DocumentBuildContext
    {
        public DocumentBuildContext(string buildOutputFolder)
        {
            BuildOutputFolder = buildOutputFolder;
        }

        public string BuildOutputFolder { get; }

        public Dictionary<string, string> FileMap { get; } = new Dictionary<string, string>();

        public Dictionary<string, string> UidMap { get; } = new Dictionary<string, string>();

        public List<ManifestItem> Manifest { get; } = new List<ManifestItem>();
    }
}
