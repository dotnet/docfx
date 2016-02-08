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

        public Dictionary<string, XRefSpec> XRefSpecMap { get; private set; } = new Dictionary<string, XRefSpec>();

        public Dictionary<string, HashSet<string>> TocMap { get; private set; } = new Dictionary<string, HashSet<string>>(FilePathComparer.OSPlatformSensitiveStringComparer);

        public HashSet<string> XRef { get; } = new HashSet<string>();

        public Dictionary<string, XRefSpec> ExternalXRefSpec { get; private set; } = new Dictionary<string, XRefSpec>();

        public void SetExternalXRefSpec()
        {
            var result = new Dictionary<string, XRefSpec>();

            // remove internal xref.
            var xref = XRef.Where(s => !XRefSpecMap.ContainsKey(s)).ToList();

            if (xref.Count == 0)
            {
                return;
            }

            if (ExternalReferencePackages.Length > 0)
            {
                using (var externalReferences = new ExternalReferencePackageCollection(ExternalReferencePackages))
                {
                    foreach (var uid in xref)
                    {
                        var spec = GetExternalReference(externalReferences, uid);
                        if (spec != null)
                        {
                            result[uid] = spec;
                        }
                    }
                }

                Logger.LogInfo($"{result.Count} external references found in {ExternalReferencePackages.Length} packages.");
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
        public void RegisterInternalXrefSpec(XRefSpec xrefSpec)
        {
            if (xrefSpec == null) throw new ArgumentNullException(nameof(xrefSpec));
            if (string.IsNullOrEmpty(xrefSpec.Href)) throw new ArgumentException("Href for xref spec must contain value");
            if (!PathUtility.IsRelativePath(xrefSpec.Href)) throw new ArgumentException("Only relative href path is supported");
            XRefSpecMap[xrefSpec.Uid] = xrefSpec;
        }

        public XRefSpec GetXrefSpec(string uid)
        {
            if (string.IsNullOrEmpty(uid)) throw new ArgumentNullException(nameof(uid));

            XRefSpec xref;
            if (XRefSpecMap.TryGetValue(uid, out xref))
            {
                return xref;
            }

            if (ExternalXRefSpec.TryGetValue(uid, out xref))
            {
                return xref;
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
