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
        public const string ManifestFileName = ".docfx.manifest";
        public const string FileMapFileName = ".docfx.filemap";
        public const string ExternalXRefSpecFileName = ".docfx.xref.external";
        public const string InternalXRefSpecFileName = ".docfx.xref.internal";
        public const string TocFileName = ".docfx.toc";

        public DocumentBuildContext(string buildOutputFolder) : this(buildOutputFolder, Enumerable.Empty<FileAndType>(), ImmutableArray<string>.Empty) { }

        public DocumentBuildContext(string buildOutputFolder, IEnumerable<FileAndType> allSourceFiles, ImmutableArray<string> externalReferencePackages)
        {
            BuildOutputFolder = buildOutputFolder;
            AllSourceFiles = allSourceFiles.ToImmutableDictionary(ft => ((RelativePath)ft.File).GetPathFromWorkingFolder(), FilePathComparer.OSPlatformSensitiveStringComparer);
            ExternalReferencePackages = externalReferencePackages;
        }

        public string BuildOutputFolder { get; }

        public ImmutableArray<string> ExternalReferencePackages { get; }

        public ImmutableDictionary<string, FileAndType> AllSourceFiles { get; }

        public Dictionary<string, string> FileMap { get; private set; } = new Dictionary<string, string>(FilePathComparer.OSPlatformSensitiveStringComparer);

        public Dictionary<string, string> UidMap { get; private set; } = new Dictionary<string, string>();

        public Dictionary<string, XRefSpec> XRefSpecMap { get; private set; } = new Dictionary<string, XRefSpec>();

        public Dictionary<string, HashSet<string>> TocMap { get; private set; } = new Dictionary<string, HashSet<string>>(FilePathComparer.OSPlatformSensitiveStringComparer);

        public Dictionary<string, XRefSpec> ExternalXRefSpec { get; private set; }

        public List<ManifestItem> Manifest { get; private set; } = new List<ManifestItem>();

        public Dictionary<string, HashSet<string>> XRef { get; } = new Dictionary<string, HashSet<string>>();

        public void SerializeTo(string outputBaseDir)
        {
            YamlUtility.Serialize(Path.Combine(outputBaseDir, ManifestFileName), Manifest);
            YamlUtility.Serialize(Path.Combine(outputBaseDir, FileMapFileName), FileMap);
            if (ExternalXRefSpec == null)
            {
                ExternalXRefSpec = GetExternalXRefSpec();
            }

            YamlUtility.Serialize(Path.Combine(outputBaseDir, ExternalXRefSpecFileName), ExternalXRefSpec.Values);
            YamlUtility.Serialize(Path.Combine(outputBaseDir, InternalXRefSpecFileName), XRefSpecMap.Values);
            YamlUtility.Serialize(Path.Combine(outputBaseDir, TocFileName), TocMap);
        }

        public static DocumentBuildContext DeserializeFrom(string outputBaseDir)
        {
            var context = new DocumentBuildContext(outputBaseDir);
            context.Manifest = YamlUtility.Deserialize<List<ManifestItem>>(Path.Combine(outputBaseDir, ManifestFileName));
            context.FileMap = new Dictionary<string, string>(YamlUtility.Deserialize<Dictionary<string, string>>(Path.Combine(outputBaseDir, FileMapFileName)), FilePathComparer.OSPlatformSensitiveStringComparer);
            context.ExternalXRefSpec = YamlUtility.Deserialize<List<XRefSpec>>(Path.Combine(outputBaseDir, ExternalXRefSpecFileName)).ToDictionary(x => x.Uid, x => x.ToReadOnly());
            context.XRefSpecMap = YamlUtility.Deserialize<List<XRefSpec>>(Path.Combine(outputBaseDir, InternalXRefSpecFileName)).ToDictionary(x => x.Uid, x => x.ToReadOnly());
            context.TocMap = new Dictionary<string, HashSet<string>>(YamlUtility.Deserialize<Dictionary<string, HashSet<string>>>(Path.Combine(outputBaseDir, TocFileName)), FilePathComparer.OSPlatformSensitiveStringComparer);
            return context;
        }

        private Dictionary<string, XRefSpec> GetExternalXRefSpec()
        {
            var result = new Dictionary<string, XRefSpec>();

            // remove internal xref.
            var xref = XRef.Where(s => !UidMap.ContainsKey(s.Key)).ToDictionary(s => s.Key, s => s.Value);

            if (xref.Count == 0)
            {
                return result;
            }

            var missingUids = new List<KeyValuePair<string, HashSet<string>>>();
            if (ExternalReferencePackages.Length > 0)
            {
                using (var externalReferences = new ExternalReferencePackageCollection(ExternalReferencePackages))
                {
                    foreach (var uid in xref.Keys)
                    {
                        var spec = GetExternalReference(externalReferences, uid);
                        if (spec != null)
                        {
                            result[uid] = spec;
                        }
                        else
                        {
                            if (missingUids.Count < 100)
                            {
                                missingUids.Add(new KeyValuePair<string, HashSet<string>>(uid, xref[uid]));
                            }
                        }
                    }
                }
            }
            else
            {
                missingUids.AddRange(XRef.Take(100));
            }
            if (missingUids.Count > 0)
            {
                var uidLines = string.Join(Environment.NewLine + "\t", missingUids.Select(s => s.Key + " in files \"" + string.Join(",", s.Value) + "\""));
                if (missingUids.Count < 100)
                {
                    Logger.LogWarning($"Missing following definitions of xref:{Environment.NewLine}\t{uidLines}.");
                }
                else
                {
                    Logger.LogWarning($"Too many missing definitions of xref, following is top 100:{Environment.NewLine}{uidLines}.");
                }
            }
            return result;
        }

        private static XRefSpec GetExternalReference(ExternalReferencePackageCollection externalReferences, string uid)
        {
            ReferenceViewModel vm;
            if (!externalReferences.TryGetReference(uid, out vm))
            {
                return null;
            }
            using (var sw = new StringWriter())
            {
                YamlUtility.Serialize(sw, vm);
                using (var sr = new StringReader(sw.ToString()))
                {
                    return YamlUtility.Deserialize<XRefSpec>(sr);
                }
            }
        }
    }
}
