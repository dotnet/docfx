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

        public DocumentBuildContext(string buildOutputFolder)
            : this(buildOutputFolder, Enumerable.Empty<FileAndType>(), ImmutableArray<string>.Empty, ImmutableArray<string>.Empty, 1, Directory.GetCurrentDirectory(), string.Empty, null, null) { }

        public DocumentBuildContext(string buildOutputFolder, IEnumerable<FileAndType> allSourceFiles, ImmutableArray<string> externalReferencePackages, ImmutableArray<string> xrefMaps, int maxParallelism, string baseFolder)
            : this(buildOutputFolder, allSourceFiles, externalReferencePackages, xrefMaps, maxParallelism, baseFolder, string.Empty, null, null) { }

        public DocumentBuildContext(string buildOutputFolder, IEnumerable<FileAndType> allSourceFiles, ImmutableArray<string> externalReferencePackages, ImmutableArray<string> xrefMaps, int maxParallelism, string baseFolder, string versionName, ApplyTemplateSettings applyTemplateSetting, string rootTocPath)
            : this(buildOutputFolder, allSourceFiles, externalReferencePackages, xrefMaps, maxParallelism, baseFolder, versionName, applyTemplateSetting, rootTocPath, null, ImmutableArray<string>.Empty) { }

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
            ImmutableArray<string> xrefserviceUrls)
        {
            BuildOutputFolder = buildOutputFolder;
            VersionName = versionName;
            ApplyTemplateSettings = applyTemplateSetting;
            AllSourceFiles = GetAllSourceFiles(allSourceFiles);
            ExternalReferencePackages = externalReferencePackages;
            XRefMapUrls = xrefMaps;
            XRefServiceUrls = xrefserviceUrls;
            MaxParallelism = maxParallelism;
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

        public string VersionName { get; }

        public string VersionFolder { get; }

        public ApplyTemplateSettings ApplyTemplateSettings { get; set; }

        public ImmutableArray<string> ExternalReferencePackages { get; }

        public ImmutableArray<string> XRefMapUrls { get; }

        public ImmutableArray<string> XRefServiceUrls { get; }

        public ImmutableDictionary<string, FileAndType> AllSourceFiles { get; }

        public int MaxParallelism { get; }

        public ConcurrentDictionary<string, string> FileMap { get; } = new ConcurrentDictionary<string, string>(FilePathComparer.OSPlatformSensitiveStringComparer);

        public ConcurrentDictionary<string, XRefSpec> XRefSpecMap { get; } = new ConcurrentDictionary<string, XRefSpec>();

        public ConcurrentDictionary<string, HashSet<string>> TocMap { get; } = new ConcurrentDictionary<string, HashSet<string>>(FilePathComparer.OSPlatformSensitiveStringComparer);

        public HashSet<string> XRef { get; } = new HashSet<string>();

        public string RootTocPath { get; }

        public IMarkdownService MarkdownService { get; set; }

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
            // remove internal xref.
            var uidList =
                (from uid in XRef
                 where !XRefSpecMap.ContainsKey(uid)
                 select uid)
                .Concat(
                 from spec in ExternalXRefSpec.Values
                 where spec.Href == null
                 select spec.Uid)
                .ToList();

            if (uidList.Count > 0)
            {
                uidList = ResolveByXRefMaps(uidList, ExternalXRefSpec);
            }
            if (uidList.Count > 0)
            {
                uidList = ResolveByExternalReferencePackages(uidList, ExternalXRefSpec);
            }
            if (uidList.Count > 0)
            {
                uidList = ResolveByXRefServiceAsync(uidList, ExternalXRefSpec).Result;
            }

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
            if (XRefServiceUrls == null || XRefServiceUrls.Length == 0)
            {
                Logger.LogWarning($"You haven't provide an xrefservice item in docfx.json or command options!");
                return uidList;
            }
            string requestUrl = XRefServiceUrls[0];
            var list = new List<string>();
            int pieceSize = 1000;
            using (var client = new HttpClient())
            {   
                try
                {
                    client.BaseAddress = new Uri(requestUrl);
                }
                catch (UriFormatException e)
                {
                    Logger.LogWarning($"Ignore invalid url: {requestUrl}." + e.Message);
                    return uidList;
                }
                catch (ArgumentException e)
                {
                    Logger.LogWarning($"Ignore invalid url: {requestUrl}." + e.Message);
                    return uidList;
                }
                
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                for (int i = 0; i < uidList.Count; i += pieceSize)
                {
                    List<string> smallPiece;
                    if (i + pieceSize < uidList.Count)
                    {
                        smallPiece = uidList.GetRange(i, pieceSize);
                    }
                    else
                    {
                        smallPiece = uidList.GetRange(i, uidList.Count - i);
                    }

                    StringContent content = new StringContent(JsonUtility.Serialize(smallPiece), System.Text.Encoding.UTF8, "application/json");
                    HttpResponseMessage response = null;
                    try
                    {
                        response = await client.PostAsync("", content);
                    }
                    catch (HttpRequestException e)
                    {
                        Logger.LogWarning(e.InnerException.Message + "\n" + smallPiece.Count + " uids being resolved failed, for example including " + smallPiece[0]);
                        list.AddRange(smallPiece);
                        continue;
                    }
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var data = await response.Content.ReadAsStreamAsync();
                        List<XRefSpec> xsList;
                        using (var sr = new StreamReader(data))
                        {
                            xsList = JsonUtility.Deserialize<List<XRefSpec>>(sr);
                        }
                        for (int j = 0; j < xsList.Count; j++)
                        {
                            if (xsList[j] == null)
                            {
                                list.Add(smallPiece[j]);
                            }
                            else
                            {
                                externalXRefSpec.AddOrUpdate(smallPiece[j], xsList[j], (_, old) => old + xsList[j]);
                            }
                        }
                    }
                    else
                    {
                        list.AddRange(smallPiece);
                        Logger.LogWarning($"Request on {requestUrl} failed." + smallPiece.Count + " uids being resolved failed, for example including " + smallPiece[0]);
                    }
                }
            }
            Logger.LogInfo($"{uidList.Count - list.Count} external references found in {requestUrl} configured in docfx.json");
            return list;
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
            Logger.LogInfo($"{uidList.Count - list.Count} external references found in {XRefMapUrls.Length} xref maps.");
            return list;
        }

        private List<XRefMap> LoadXRefMaps()
        {
            using (var client = new HttpClient())
            {
                Logger.LogInfo($"Downloading xref maps from:{Environment.NewLine}{string.Join(Environment.NewLine, XRefMapUrls)}");
                var mapTasks = (from url in XRefMapUrls
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
                new HashSet<string>(FilePathComparer.OSPlatformSensitiveComparer) { tocFileKey },
                (k, v) => { v.Add(tocFileKey); return v; });
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
