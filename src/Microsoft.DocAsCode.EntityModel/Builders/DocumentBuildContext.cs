namespace Microsoft.DocAsCode.EntityModel.Builders
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode.EntityModel.ViewModels;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Utility;

    public sealed class DocumentBuildContext
    {
        public DocumentBuildContext(string buildOutputFolder) : this(buildOutputFolder, Enumerable.Empty<FileAndType>(), ImmutableArray<string>.Empty) { }

        public DocumentBuildContext(string buildOutputFolder, IEnumerable<FileAndType> allSourceFiles, ImmutableArray<string> externalReferencePackages)
        {
            BuildOutputFolder = buildOutputFolder;
            AllSourceFiles = allSourceFiles.Select(ft => (string)(HostService.RootSymbol + (RelativePath)ft.File)).ToImmutableHashSet(FilePathComparer.OSPlatformSensitiveStringComparer);
            ExternalReferencePackages = externalReferencePackages;
        }

        public string BuildOutputFolder { get; }

        public ImmutableArray<string> ExternalReferencePackages { get; }

        public ImmutableHashSet<string> AllSourceFiles { get; }

        public Dictionary<string, string> FileMap { get; private set; } = new Dictionary<string, string>(FilePathComparer.OSPlatformSensitiveStringComparer);

        public Dictionary<string, string> UidMap { get; private set; } = new Dictionary<string, string>();

        public Dictionary<string, XRefSpec> XRefSpecMap { get; private set; } = new Dictionary<string, XRefSpec>();
        
        public Dictionary<string, HashSet<string>> TocMap { get; private set; } = new Dictionary<string, HashSet<string>>(FilePathComparer.OSPlatformSensitiveStringComparer);

        public Dictionary<string, string> XRefMap { get; private set; } = null;

        public List<ManifestItem> Manifest { get; private set; } = new List<ManifestItem>();

        public HashSet<string> XRef { get; } = new HashSet<string>();

        public void SerializeTo(string outputBaseDir)
        {
            YamlUtility.Serialize(Path.Combine(outputBaseDir, ".docfx.manifest"), Manifest);
            YamlUtility.Serialize(Path.Combine(outputBaseDir, ".docfx.filemap"), FileMap);
            if (XRefMap == null)
            {
                XRefMap = GetXRef();
            }

            YamlUtility.Serialize(Path.Combine(outputBaseDir, ".docfx.xref"), XRefMap);
            YamlUtility.Serialize(Path.Combine(outputBaseDir, ".docfx.xrefspec"), XRefSpecMap.Values);
            YamlUtility.Serialize(Path.Combine(outputBaseDir, ".docfx.toc"), TocMap);
        }

        public static DocumentBuildContext DeserializeFrom(string outputBaseDir)
        {
            var context = new DocumentBuildContext(outputBaseDir);
            context.Manifest = YamlUtility.Deserialize<List<ManifestItem>>(Path.Combine(outputBaseDir, ".docfx.manifest"));
            context.FileMap = new Dictionary<string, string>(YamlUtility.Deserialize<Dictionary<string, string>>(Path.Combine(outputBaseDir, ".docfx.filemap")), FilePathComparer.OSPlatformSensitiveStringComparer);
            context.XRefMap = YamlUtility.Deserialize<Dictionary<string, string>>(Path.Combine(outputBaseDir, ".docfx.xref"));
            context.XRefSpecMap = YamlUtility.Deserialize<List<XRefSpec>>(Path.Combine(outputBaseDir, ".docfx.xrefspec")).ToDictionary(x => x.Uid, x => x.ToReadOnly());
            context.TocMap = new Dictionary<string, HashSet<string>>(YamlUtility.Deserialize<Dictionary<string, HashSet<string>>>(Path.Combine(outputBaseDir, ".docfx.toc")), FilePathComparer.OSPlatformSensitiveStringComparer);
            return context;
        }

        private Dictionary<string, string> GetXRef()
        {
            var common = UidMap.Keys.Intersect(XRef).ToList();
            var result = new Dictionary<string, string>(XRef.Count);
            foreach (var uid in common)
            {
                result[uid] = UidMap[uid];
            }
            XRef.ExceptWith(common);
            List<string> missingUids = new List<string>();
            if (XRef.Count > 0)
            {
                if (ExternalReferencePackages.Length > 0)
                {
                    var externalReferences = (from reader in
                                                  from package in ExternalReferencePackages.AsParallel()
                                                  select ExternalReferencePackageReader.CreateNoThrow(package)
                                              where reader != null
                                              select reader).ToList();

                    foreach (var uid in XRef)
                    {
                        var href = GetExternalReference(externalReferences, uid);
                        if (href != null)
                        {
                            result[uid] = href;
                        }
                        else
                        {
                            if (missingUids.Count < 100)
                            {
                                missingUids.Add(uid);
                            }
                        }
                    }
                }
                else
                {
                    missingUids.AddRange(XRef.Take(100));
                }
            }
            if (missingUids.Count > 0)
            {
                var uidLines = string.Join(Environment.NewLine + "\t", missingUids);
                if (missingUids.Count < 100)
                {
                    Logger.LogWarning($"Missing following definitions of xref:{Environment.NewLine}{uidLines}.");
                }
                else
                {
                    Logger.LogWarning($"Too many missing definitions of xref, following is top 100:{Environment.NewLine}{uidLines}.");
                }
            }
            return result;
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
