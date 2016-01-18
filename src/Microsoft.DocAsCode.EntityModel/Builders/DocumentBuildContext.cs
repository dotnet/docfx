namespace Microsoft.DocAsCode.EntityModel.Builders
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.EntityModel.ViewModels;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Utility;

    public sealed class DocumentBuildContext : IDocumentBuildContext
    {
        public DocumentBuildContext(string buildOutputFolder) : this(buildOutputFolder, Enumerable.Empty<FileAndType>(), ImmutableArray<string>.Empty, null) { }

        public DocumentBuildContext(string buildOutputFolder, IEnumerable<FileAndType> allSourceFiles, ImmutableArray<string> externalReferencePackages, TemplateCollection templateCollection)
        {
            BuildOutputFolder = buildOutputFolder;
            AllSourceFiles = allSourceFiles.ToImmutableDictionary(ft => ((RelativePath)ft.File).GetPathFromWorkingFolder(), FilePathComparer.OSPlatformSensitiveStringComparer);
            TemplateCollection = templateCollection;
            ExternalReferencePackages = externalReferencePackages;
        }

        public string BuildOutputFolder { get; }

        public TemplateCollection TemplateCollection { get; }

        public ImmutableArray<string> ExternalReferencePackages { get; }

        public ImmutableDictionary<string, FileAndType> AllSourceFiles { get; }

        public Dictionary<string, string> FileMap { get; private set; } = new Dictionary<string, string>(FilePathComparer.OSPlatformSensitiveStringComparer);

        public Dictionary<string, string> UidMap { get; private set; } = new Dictionary<string, string>();

        public Dictionary<string, XRefSpec> XRefSpecMap { get; private set; } = new Dictionary<string, XRefSpec>();

        public Dictionary<string, HashSet<string>> TocMap { get; private set; } = new Dictionary<string, HashSet<string>>(FilePathComparer.OSPlatformSensitiveStringComparer);

        public List<ManifestItem> Manifest { get; private set; } = new List<ManifestItem>();

        public Dictionary<string, HashSet<string>> XRef { get; } = new Dictionary<string, HashSet<string>>();

        public Dictionary<string, XRefSpec> ExternalXRefSpec { get; private set; } = new Dictionary<string, XRefSpec>();

        public void SetExternalXRefSpec()
        {
            var result = new Dictionary<string, XRefSpec>();

            // remove internal xref.
            var xref = XRef.Where(s => !UidMap.ContainsKey(s.Key)).ToDictionary(s => s.Key, s => s.Value);

            if (xref.Count == 0)
            {
                return;
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
                missingUids.AddRange(xref.Take(100));
            }
            if (missingUids.Count > 0)
            {
                var uidLines = string.Join(Environment.NewLine + "\t", missingUids.Select(s => "@" + s.Key + " in files \"" + string.Join(",", s.Value.Select(p => p.ToDisplayPath())) + "\""));
                if (missingUids.Count < 100)
                {
                    Logger.LogWarning($"Missing following definitions of cross-reference:{Environment.NewLine}\t{uidLines}");
                }
                else
                {
                    Logger.LogWarning($"Too many missing definitions of cross-reference, following is top 100:{Environment.NewLine}\t{uidLines}");
                }
            }
            ExternalXRefSpec = result;
        }

        public string GetFilePath(string key)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            string filePath;
            if (FileMap.TryGetValue(key, out filePath))
            {
                return filePath;
            }

            return null;
        }

        // TODO: use this method instead of directly accessing FileMap
        public void SetFilePath(string key, string filePath)
        {
            FileMap[key] = filePath;
        }

        // TODO: use this method instead of directly accessing UidMap
        public void SetFileKeyWithUid(string uid, string fileKey)
        {
            UidMap[uid] = fileKey;
        }

        public string GetFileKeyFromUid(string uid)
        {
            if (string.IsNullOrEmpty(uid)) throw new ArgumentNullException(nameof(uid));

            string key;
            if (UidMap.TryGetValue(uid, out key))
            {
                return key;
            }

            return null;
        }

        public IImmutableList<string> GetTocFileKeySet(string key)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            HashSet<string> sets;
            if (TocMap.TryGetValue(key, out sets))
            {
                return sets.ToImmutableArray();
            }

            return null;
        }

        public void RegisterToc(string tocFileKey, string fileKey)
        {
            if (string.IsNullOrEmpty(fileKey)) throw new ArgumentNullException(nameof(fileKey));
            if (string.IsNullOrEmpty(tocFileKey)) throw new ArgumentNullException(nameof(tocFileKey));
            HashSet<string> sets;
            if (TocMap.TryGetValue(fileKey, out sets))
            {
                sets.Add(tocFileKey);
            }
            else
            {
                TocMap[fileKey] = new HashSet<string>(FilePathComparer.OSPlatformSensitiveComparer) { tocFileKey };
            }
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
