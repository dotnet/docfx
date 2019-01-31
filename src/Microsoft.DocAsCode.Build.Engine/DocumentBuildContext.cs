// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;

    using Microsoft.DocAsCode.Build.Engine.Incrementals;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.Exceptions;
    using Microsoft.DocAsCode.Plugins;

    public sealed class DocumentBuildContext : IDocumentBuildContext
    {
        private readonly ConcurrentDictionary<string, TocInfo> _tableOfContents = new ConcurrentDictionary<string, TocInfo>(FilePathComparer.OSPlatformSensitiveStringComparer);
        private readonly Task<IXRefContainerReader> _reader;
        private ImmutableArray<string> _xrefMapUrls { get; }
        private ImmutableArray<string> _xrefServiceUrls { get; }

        public DocumentBuildContext(string buildOutputFolder)
            : this(buildOutputFolder, Enumerable.Empty<FileAndType>(), ImmutableArray<string>.Empty, ImmutableArray<string>.Empty, 1, Directory.GetCurrentDirectory(), string.Empty, null, null) { }

        public DocumentBuildContext(string buildOutputFolder, IEnumerable<FileAndType> allSourceFiles, ImmutableArray<string> externalReferencePackages, ImmutableArray<string> xrefMaps, int maxParallelism, string baseFolder)
            : this(buildOutputFolder, allSourceFiles, externalReferencePackages, xrefMaps, maxParallelism, baseFolder, string.Empty, null, null) { }

        public DocumentBuildContext(string buildOutputFolder, IEnumerable<FileAndType> allSourceFiles, ImmutableArray<string> externalReferencePackages, ImmutableArray<string> xrefMaps, int maxParallelism, string baseFolder, string versionName, ApplyTemplateSettings applyTemplateSetting, string rootTocPath)
            : this(buildOutputFolder, allSourceFiles, externalReferencePackages, xrefMaps, maxParallelism, baseFolder, versionName, applyTemplateSetting, rootTocPath, null, ImmutableArray<string>.Empty) { }

        public DocumentBuildContext(string buildOutputFolder, IEnumerable<FileAndType> allSourceFiles, ImmutableArray<string> externalReferencePackages, ImmutableArray<string> xrefMaps, int maxParallelism, string baseFolder, string versionName, ApplyTemplateSettings applyTemplateSetting, string rootTocPath, string versionFolder, ImmutableArray<string> xrefServiceUrls)
            : this(buildOutputFolder, allSourceFiles, externalReferencePackages, xrefMaps, maxParallelism, baseFolder, versionName, applyTemplateSetting, rootTocPath, null, ImmutableArray<string>.Empty, null, null) { }

        public DocumentBuildContext(DocumentBuildParameters parameters)
        {
            BuildOutputFolder = Path.Combine(Path.GetFullPath(EnvironmentContext.BaseDirectory), parameters.OutputBaseDir);
            VersionName = parameters.VersionName;
            ApplyTemplateSettings = parameters.ApplyTemplateSettings;
            HrefGenerator = parameters.ApplyTemplateSettings?.HrefGenerator;
            AllSourceFiles = GetAllSourceFiles(parameters.Files.EnumerateFiles());
            _xrefMapUrls = parameters.XRefMaps;
            _xrefServiceUrls = parameters.XRefServiceUrls;
            GroupInfo = parameters.GroupInfo;
            XRefTags = parameters.XRefTags;
            MaxParallelism = parameters.MaxParallelism;
            MaxHttpParallelism = parameters.MaxHttpParallelism;

            if (parameters.XRefMaps.Length > 0)
            {
                _reader = new XRefCollection(
                    from u in parameters.XRefMaps
                    select new Uri(u, UriKind.RelativeOrAbsolute)).GetReaderAsync(parameters.Files.DefaultBaseDir);
            }
            RootTocPath = parameters.RootTocPath;

            if (!string.IsNullOrEmpty(parameters.VersionDir) && Path.IsPathRooted(parameters.VersionDir))
            {
                throw new ArgumentException("VersionDir cannot be rooted.", nameof(parameters));
            }
            var versionDir = parameters.VersionDir;
            if (!string.IsNullOrEmpty(versionDir))
            {
                versionDir = versionDir.Replace('\\', '/');
                if (!versionDir.EndsWith("/"))
                {
                    versionDir += "/";
                }
            }
            VersionFolder = versionDir;
        }

        public DocumentBuildContext(
            string buildOutputFolder,
            IEnumerable<FileAndType> allSourceFiles,
            ImmutableArray<string> externalReferencePackages,
            ImmutableArray<string> xrefMaps,
            int maxParallelism,
            string baseFolder,
            string versionName,
            ApplyTemplateSettings applyTemplateSetting,
            string rootTocPath,
            string versionFolder,
            ImmutableArray<string> xrefServiceUrls,
            GroupInfo groupInfo,
            List<string> xrefTags)
        {
            BuildOutputFolder = buildOutputFolder;
            VersionName = versionName;
            ApplyTemplateSettings = applyTemplateSetting;
            HrefGenerator = applyTemplateSetting?.HrefGenerator;
            AllSourceFiles = GetAllSourceFiles(allSourceFiles);
            ExternalReferencePackages = externalReferencePackages;
            _xrefMapUrls = xrefMaps;
            _xrefServiceUrls = xrefServiceUrls;
            GroupInfo = groupInfo;
            XRefTags = xrefTags;
            MaxParallelism = maxParallelism;
            MaxHttpParallelism = maxParallelism * 2;
            if (xrefMaps.Length > 0)
            {
                _reader = new XRefCollection(
                    from u in xrefMaps
                    select new Uri(u, UriKind.RelativeOrAbsolute)).GetReaderAsync(baseFolder);
            }
            RootTocPath = rootTocPath;
            if (!string.IsNullOrEmpty(versionFolder) && Path.IsPathRooted(versionFolder))
            {
                throw new ArgumentException("Path cannot be rooted.", nameof(versionFolder));
            }
            if (!string.IsNullOrEmpty(versionFolder))
            {
                versionFolder = versionFolder.Replace('\\', '/');
                if (!versionFolder.EndsWith("/"))
                {
                    versionFolder += "/";
                }
            }
            VersionFolder = versionFolder;
        }

        public string BuildOutputFolder { get; }

        [Obsolete("use GroupInfo")]
        public string VersionName { get; }

        [Obsolete("use GroupInfo")]
        public string VersionFolder { get; }

        public GroupInfo GroupInfo { get; }

        public List<string> XRefTags { get; }

        public ApplyTemplateSettings ApplyTemplateSettings { get; set; }

        public ImmutableArray<string> ExternalReferencePackages { get; } = ImmutableArray<string>.Empty;

        public ImmutableDictionary<string, FileAndType> AllSourceFiles { get; }

        public int MaxParallelism { get; }

        public int MaxHttpParallelism { get; }

        public ConcurrentDictionary<string, string> FileMap { get; } = new ConcurrentDictionary<string, string>(FilePathComparer.OSPlatformSensitiveStringComparer);

        public ConcurrentDictionary<string, XRefSpec> XRefSpecMap { get; } = new ConcurrentDictionary<string, XRefSpec>();

        public ConcurrentDictionary<string, HashSet<string>> TocMap { get; } = new ConcurrentDictionary<string, HashSet<string>>(FilePathComparer.OSPlatformSensitiveStringComparer);

        public HashSet<string> XRef { get; } = new HashSet<string>();

        public string RootTocPath { get; }

        public IMarkdownService MarkdownService { get; set; }

        public ICustomHrefGenerator HrefGenerator { get; }

        internal IncrementalBuildContext IncrementalBuildContext { get; set; }

        internal ConcurrentBag<ManifestItem> ManifestItems { get; } = new ConcurrentBag<ManifestItem>();

        private ConcurrentDictionary<string, XRefSpec> ExternalXRefSpec { get; } = new ConcurrentDictionary<string, XRefSpec>();

        private ConcurrentDictionary<string, object> UnknownUids { get; } = new ConcurrentDictionary<string, object>();

        public void ReportExternalXRefSpec(XRefSpec spec)
        {
            ExternalXRefSpec.AddOrUpdate(
                spec.Uid,
                spec,
                (uid, old) => old + spec);
        }

        internal void SaveExternalXRefSpec(TextWriter writer)
        {
            JsonUtility.Serialize(writer, ExternalXRefSpec);
        }

        internal void LoadExternalXRefSpec(TextReader reader)
        {
            if (ExternalXRefSpec.Count > 0)
            {
                throw new InvalidOperationException("Cannot load after reporting external xref spec.");
            }
            var dict = JsonUtility.Deserialize<Dictionary<string, XRefSpec>>(reader);
            foreach (var pair in dict)
            {
                ExternalXRefSpec[pair.Key] = pair.Value;
            }
        }

        public void ResolveExternalXRefSpec()
        {
            Task.WaitAll(
                Task.Run(() => ResolveExternalXRefSpecForSpecs()),
                Task.Run(() => ResolveExternalXRefSpecForNoneSpecsAsync()));
        }

        private void ResolveExternalXRefSpecForSpecs()
        {
            foreach (var item in from spec in ExternalXRefSpec.Values
                                 where spec.Href == null && spec.IsSpec
                                 select spec.Uid)
            {
                UnknownUids.TryAdd(item, null);
            }
        }

        public async Task ResolveExternalXRefSpecForNoneSpecsAsync()
        {
            // remove internal xref.
            var uidList =
                (from uid in XRef
                 where !ExternalXRefSpec.ContainsKey(uid) && !XRefSpecMap.ContainsKey(uid)
                 select uid)
                .Concat(
                 from spec in ExternalXRefSpec.Values
                 where spec.Href == null && !spec.IsSpec && !XRefSpecMap.ContainsKey(spec.Uid)
                 select spec.Uid)
                .ToList();

            if (uidList.Count == 0)
            {
                return;
            }
            uidList = ResolveByXRefMaps(uidList, ExternalXRefSpec);
            if (uidList.Count > 0)
            {
                uidList = ResolveByExternalReferencePackages(uidList, ExternalXRefSpec);
            }
            if (uidList.Count > 0)
            {
                uidList = await ResolveByXRefServiceAsync(uidList, ExternalXRefSpec);
            }

            Logger.LogVerbose($"{uidList.Count} uids are unresolved.");

            foreach (var uid in uidList)
            {
                UnknownUids.TryAdd(uid, null);
            }
        }

        private List<string> ResolveByExternalReferencePackages(List<string> uidList, ConcurrentDictionary<string, XRefSpec> externalXRefSpec)
        {
            if (ExternalReferencePackages.Length == 0)
            {
                return uidList;
            }

            var oldSpecCount = externalXRefSpec.Count;
            var list = new List<string>();
            using (var externalReferences = new ExternalReferencePackageCollection(ExternalReferencePackages, MaxParallelism))
            {
                foreach (var uid in uidList)
                {
                    var spec = GetExternalReference(externalReferences, uid);
                    if (spec != null)
                    {
                        externalXRefSpec.AddOrUpdate(uid, spec, (_, old) => old + spec);
                    }
                    else
                    {
                        list.Add(uid);
                    }
                }
            }

            Logger.LogInfo($"{externalXRefSpec.Count - oldSpecCount} external references found in {ExternalReferencePackages.Length} packages.");
            return list;
        }

        private async Task<List<string>> ResolveByXRefServiceAsync(List<string> uidList, ConcurrentDictionary<string, XRefSpec> externalXRefSpec)
        {
            if (_xrefServiceUrls == null || _xrefServiceUrls.Length == 0)
            {
                return uidList;
            }

            var unresolvedUidList = await new XrefServiceResolver(_xrefServiceUrls, MaxHttpParallelism).ResolveAsync(uidList, externalXRefSpec);
            Logger.LogInfo($"{uidList.Count - unresolvedUidList.Count} uids found in {_xrefServiceUrls.Length} xrefservice(s).");
            return unresolvedUidList;
        }

        internal async Task<IList<XRefSpec>> QueryByHttpRequestAsync(HttpClient client, string requestUrl, string uid)
        {
            string url = requestUrl.Replace("{uid}", Uri.EscapeDataString(uid));
            try
            {
                var data = await client.GetStreamAsync(url);
                using (var sr = new StreamReader(data))
                {
                    var xsList = JsonUtility.Deserialize<List<Dictionary<string, object>>>(sr);
                    return xsList.ConvertAll(item =>
                    {
                        var spec = new XRefSpec();
                        foreach (var pair in item)
                        {
                            if (pair.Value is string s)
                            {
                                spec[pair.Key] = s;
                            }
                        }
                        return spec;
                    });
                }
            }
            catch (HttpRequestException e)
            {
                Logger.LogWarning($"Error occurs when resolve {uid} from {requestUrl}.{e.InnerException.Message}");
                return null;
            }
            catch (Newtonsoft.Json.JsonReaderException e)
            {
                Logger.LogWarning($"Response from {requestUrl} is not in valid JSON format.{e.Message}");
                return null;
            }
            catch (Exception e)
            {
                Logger.LogWarning($"Error occurs when resolve {uid} from {requestUrl}.{e.Message}");
                return null;
            }
        }

        private List<string> ResolveByXRefMaps(List<string> uidList, ConcurrentDictionary<string, XRefSpec> externalXRefSpec)
        {
            if (_reader == null)
            {
                return uidList;
            }
            var reader = _reader.Result;
            var list = new List<string>();
            foreach (var uid in uidList)
            {
                var spec = reader.Find(uid);
                if (spec != null)
                {
                    externalXRefSpec.AddOrUpdate(uid, spec, (_, old) => old + spec);
                }
                else
                {
                    list.Add(uid);
                }
            }
            Logger.LogInfo($"{uidList.Count - list.Count} external references found in {_xrefMapUrls.Length} xref maps.");
            return list;
        }

        private List<XRefMap> LoadXRefMaps()
        {
            using (var client = new HttpClient())
            {
                Logger.LogInfo($"Downloading xref maps from:{Environment.NewLine}{string.Join(Environment.NewLine, _xrefMapUrls)}");
                var mapTasks = (from url in _xrefMapUrls
                                select LoadXRefMap(url, client)).ToArray();
                Task.WaitAll(mapTasks);
                return (from t in mapTasks
                        where t.Result != null
                        select t.Result).ToList();
            }
        }

        private async Task<XRefMap> LoadXRefMap(string url, HttpClient client)
        {
            try
            {
                if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uri) &&
                    uri.Scheme != "http" &&
                    uri.Scheme != "https")
                {
                    Logger.LogWarning($"Ignore invalid url: {url}");
                    return null;
                }
                using (var stream = await client.GetStreamAsync(uri))
                using (var sr = new StreamReader(stream))
                {
                    var map = YamlUtility.Deserialize<XRefMap>(sr);
                    map.UpdateHref(uri);
                    Logger.LogVerbose($"Xref map ({url}) downloaded.");
                    return map;
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Unable to download xref map from {url}, detail:{Environment.NewLine}{ex.ToString()}");
                return null;
            }
        }

        public string GetFilePath(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }
            if (key.Length == 0)
            {
                throw new ArgumentException("Key cannot be empty.", nameof(key));
            }
            if (FileMap.TryGetValue(key, out string filePath))
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

        public string UpdateHref(string href)
        {
            if (href == null)
            {
                throw new ArgumentNullException(nameof(href));
            }
            return UpdateHrefCore(href, null);
        }

        public string UpdateHref(string href, RelativePath fromFile)
        {
            if (href == null)
            {
                throw new ArgumentNullException(nameof(href));
            }
            if (fromFile != null && !fromFile.IsFromWorkingFolder())
            {
                throw new ArgumentException("File must be from working folder (i.e. start with '~/').", nameof(fromFile));
            }
            return UpdateHrefCore(href, fromFile);
        }

        private string UpdateHrefCore(string href, RelativePath fromFile)
        {
            if (href.Length == 0)
            {
                return string.Empty;
            }
            var path = UriUtility.GetPath(href);
            if (path.Length == 0)
            {
                return href;
            }
            var qf = UriUtility.GetQueryStringAndFragment(href);
            var rp = RelativePath.TryParse(path);
            if (rp == null || !rp.IsFromWorkingFolder())
            {
                return href;
            }
            if (!FileMap.TryGetValue(rp.UrlDecode(), out string filePath))
            {
                return href;
            }
            string modifiedPath;
            if (fromFile == null)
            {
                modifiedPath = ((RelativePath)filePath).UrlEncode();
            }
            else
            {
                modifiedPath = ((RelativePath)filePath - fromFile).UrlEncode();
            }
            return modifiedPath + qf;
        }

        // TODO: use this method instead of directly accessing UidMap
        public void RegisterInternalXrefSpec(XRefSpec xrefSpec)
        {
            if (xrefSpec == null)
            {
                throw new ArgumentNullException(nameof(xrefSpec));
            }
            if (string.IsNullOrEmpty(xrefSpec.Href))
            {
                throw new ArgumentException("Href for xref spec must contain value");
            }
            if (!PathUtility.IsRelativePath(xrefSpec.Href))
            {
                throw new ArgumentException("Only relative href path is supported");
            }
            XRefSpecMap[xrefSpec.Uid] = xrefSpec;
        }

        public void RegisterInternalXrefSpecBookmark(string uid, string bookmark)
        {
            if (uid == null)
            {
                throw new ArgumentNullException(nameof(uid));
            }
            if (uid.Length == 0)
            {
                throw new ArgumentException("Uid cannot be empty", nameof(uid));
            }
            if (bookmark == null)
            {
                throw new ArgumentNullException(nameof(bookmark));
            }
            if (bookmark.Length == 0)
            {
                return;
            }

            if (XRefSpecMap.TryGetValue(uid, out XRefSpec xref))
            {
                xref.Href = UriUtility.GetNonFragment(xref.Href) + "#" + bookmark;
            }
            else
            {
                throw new DocfxException($"Xref spec with uid {uid} not found. Can't register bookmark {bookmark} to it.");
            }
        }

        public XRefSpec GetXrefSpec(string uid)
        {
            if (string.IsNullOrEmpty(uid))
            {
                throw new ArgumentNullException(nameof(uid));
            }

            if (XRefSpecMap.TryGetValue(uid, out XRefSpec xref))
            {
                return xref;
            }

            if (ExternalXRefSpec.TryGetValue(uid, out xref))
            {
                return xref;
            }

            if (UnknownUids.ContainsKey(uid))
            {
                return null;
            }

            if (_reader != null)
            {
                xref = _reader.Result.Find(uid);
                if (xref != null)
                {
                    return ExternalXRefSpec.AddOrUpdate(uid, xref, (_, old) => old + xref);
                }
            }

            if (ExternalReferencePackages.Length > 0)
            {
                using (var externalReferences = new ExternalReferencePackageCollection(ExternalReferencePackages, MaxParallelism))
                {
                    xref = GetExternalReference(externalReferences, uid);
                }
                if (xref != null)
                {
                    return ExternalXRefSpec.AddOrUpdate(uid, xref, (_, old) => old + xref);
                }
            }

            var uidList = ResolveByXRefServiceAsync(new List<string> { uid }, ExternalXRefSpec).Result;
            if (uidList.Count == 0)
            {
                return ExternalXRefSpec[uid];
            }

            UnknownUids.TryAdd(uid, null);
            return null;
        }

        public IImmutableList<string> GetTocFileKeySet(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException(nameof(key));
            }
            if (TocMap.TryGetValue(key, out HashSet<string> sets))
            {
                return sets.ToImmutableArray();
            }

            return null;
        }

        public void RegisterToc(string tocFileKey, string fileKey)
        {
            if (string.IsNullOrEmpty(fileKey)) throw new ArgumentNullException(nameof(fileKey));
            if (string.IsNullOrEmpty(tocFileKey)) throw new ArgumentNullException(nameof(tocFileKey));

            TocMap.AddOrUpdate(
                fileKey,
                new HashSet<string>(FilePathComparer.OSPlatformSensitiveRelativePathComparer) { tocFileKey },
                (k, v) =>
                {
                    lock (v)
                    {
                        v.Add(tocFileKey);
                    }
                    return v;
                });
        }

        public void RegisterTocInfo(TocInfo toc)
        {
            _tableOfContents[toc.TocFileKey] = toc;
        }

        public IImmutableList<TocInfo> GetTocInfo()
        {
            return _tableOfContents.Values.ToImmutableList();
        }

        private ImmutableDictionary<string, FileAndType> GetAllSourceFiles(IEnumerable<FileAndType> allSourceFiles)
        {
            var dict = new Dictionary<string, FileAndType>(FilePathComparer.OSPlatformSensitiveStringComparer);
            foreach (var item in allSourceFiles)
            {
                var path = (string)((RelativePath)item.File).GetPathFromWorkingFolder();
                if (dict.TryGetValue(path, out FileAndType ft))
                {
                    if (FilePathComparer.OSPlatformSensitiveStringComparer.Equals(ft.BaseDir, item.BaseDir) &&
                        FilePathComparer.OSPlatformSensitiveStringComparer.Equals(ft.File, item.File))
                    {
                        if (ft.Type >= item.Type)
                        {
                            Logger.LogWarning($"Ignored duplicate file {Path.Combine(item.BaseDir, item.File)}.");
                            continue;
                        }
                        else
                        {
                            Logger.LogWarning($"Ignored duplicate file {Path.Combine(ft.BaseDir, ft.File)}.");
                        }
                    }
                    else
                    {
                        if (ft.Type >= item.Type)
                        {
                            Logger.LogWarning($"Ignored conflict file {Path.Combine(item.BaseDir, item.File)} for {path} by {Path.Combine(ft.BaseDir, ft.File)}.");
                            continue;
                        }
                        else
                        {
                            Logger.LogWarning($"Ignored conflict file {Path.Combine(ft.BaseDir, ft.File)} for {path} by {Path.Combine(item.BaseDir, item.File)}.");
                        }
                    }
                }
                dict[path] = item;
            }
            return dict.ToImmutableDictionary(FilePathComparer.OSPlatformSensitiveStringComparer);
        }

        private static XRefSpec GetExternalReference(ExternalReferencePackageCollection externalReferences, string uid)
        {
            if (!externalReferences.TryGetReference(uid, out ReferenceViewModel vm))
            {
                return null;
            }
            return YamlUtility.ConvertTo<XRefSpec>(vm);
        }
    }
}
