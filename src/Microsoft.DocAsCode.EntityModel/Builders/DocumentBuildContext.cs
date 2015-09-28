namespace Microsoft.DocAsCode.EntityModel.Builders
{
    using ViewModels;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;

    public sealed class DocumentBuildContext
    {
        public DocumentBuildContext(string buildOutputFolder): this(buildOutputFolder, ImmutableArray<string>.Empty) { }

        public DocumentBuildContext(string buildOutputFolder, ImmutableArray<string> externalReferencePackages)
        {
            BuildOutputFolder = buildOutputFolder;
            ExternalReferencePackages = externalReferencePackages;
        }

        public string BuildOutputFolder { get; }

        public ImmutableArray<string> ExternalReferencePackages { get; }

        public Dictionary<string, string> FileMap { get; private set; } = new Dictionary<string, string>();

        public Dictionary<string, string> UidMap { get; private set; } = new Dictionary<string, string>();

        public Dictionary<string, string> XRefMap { get; private set; } = null;

        public List<ManifestItem> Manifest { get; private set; } = new List<ManifestItem>();

        public HashSet<string> XRef { get; } = new HashSet<string>();

        public void SerializeTo(string outputBaseDir)
        {
            YamlUtility.Serialize(Path.Combine(outputBaseDir, ".docfx.manifest"), this.Manifest);
            YamlUtility.Serialize(Path.Combine(outputBaseDir, ".docfx.filemap"), this.FileMap);
            if (this.XRefMap == null)
            {
                this.XRefMap = GetXRef();
            }

            YamlUtility.Serialize(Path.Combine(outputBaseDir, ".docfx.xref"), this.XRefMap);
        }

        public static DocumentBuildContext DeserializeFrom(string outputBaseDir)
        {
            var context = new DocumentBuildContext(outputBaseDir, new ImmutableArray<string>());
            context.Manifest = YamlUtility.Deserialize<List<ManifestItem>>(Path.Combine(outputBaseDir, ".docfx.manifest"));
            context.FileMap = YamlUtility.Deserialize<Dictionary<string, string>>(Path.Combine(outputBaseDir, ".docfx.filemap"));
            context.XRefMap = YamlUtility.Deserialize<Dictionary<string, string>>(Path.Combine(outputBaseDir, ".docfx.xref"));
            return context;
        }

        private Dictionary<string, string> GetXRef()
        {
            var common = this.UidMap.Keys.Intersect(this.XRef).ToList();
            var xref = new Dictionary<string, string>(this.XRef.Count);
            foreach (var uid in common)
            {
                xref[uid] = this.UidMap[uid];
            }
            this.XRef.ExceptWith(common);
            if (this.XRef.Count > 0)
            {
                if (this.ExternalReferencePackages.Length > 0)
                {
                        var externalReferences = (from reader in
                                                      from package in this.ExternalReferencePackages.AsParallel()
                                                      select ExternalReferencePackageReader.CreateNoThrow(package)
                                                  where reader != null
                                                  select reader).ToList();

                    foreach (var uid in this.XRef.Except(common))
                    {
                        var href = GetExternalReference(externalReferences, uid);
                        if (href != null)
                        {
                            this.UidMap[uid] = href;
                        }
                        else
                        {
                            // todo : trace xref cannot find.
                        }
                    }
                }
                else
                {
                    // todo : trace xref cannot find.
                }
            }
            return xref;
        }

        private static string GetExternalReference(List<ExternalReferencePackageReader> externalReferences, string uid)
        {
            if (externalReferences != null)
            {
                foreach (var reader in externalReferences)
                {
                    ReferenceViewModel vm;
                    if (reader.TryGetReference(uid, out vm))
                    {
                        return vm.Href;
                    }
                }
            }
            return null;
        }
    }
}
