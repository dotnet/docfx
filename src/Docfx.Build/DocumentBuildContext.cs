// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Immutable;

using Docfx.Common;
using Docfx.DataContracts.Common;
using Docfx.Exceptions;
using Docfx.Plugins;

namespace Docfx.Build.Engine;

public sealed class DocumentBuildContext : IDocumentBuildContext
{
    private readonly ConcurrentDictionary<string, TocInfo> _tableOfContents = new(FilePathComparer.OSPlatformSensitiveStringComparer);
    private readonly Task<IXRefContainerReader> _reader;

    public DocumentBuildContext(DocumentBuildParameters parameters, CancellationToken cancellationToken)
    {
        BuildOutputFolder = Path.Combine(Path.GetFullPath(EnvironmentContext.BaseDirectory), parameters.OutputBaseDir);
        VersionName = parameters.VersionName;
        ApplyTemplateSettings = parameters.ApplyTemplateSettings;
        HrefGenerator = parameters.ApplyTemplateSettings?.HrefGenerator;
        AllSourceFiles = GetAllSourceFiles(parameters.Files.EnumerateFiles());
        GroupInfo = parameters.GroupInfo;
        MaxParallelism = parameters.MaxParallelism;

        if (parameters.XRefMaps.Length > 0)
        {
            // Note: `_reader` task is processed asyncronously and await is called later. So OperationCancellationException is not thrown by this lines.
            _reader = new XRefCollection(
                from u in parameters.XRefMaps
                select new Uri(u, UriKind.RelativeOrAbsolute)).GetReaderAsync(parameters.Files.DefaultBaseDir, parameters.MarkdownEngineParameters?.FallbackFolders, cancellationToken);
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
            if (!versionDir.EndsWith('/'))
            {
                versionDir += "/";
            }
        }
        VersionFolder = versionDir;
        CancellationToken = cancellationToken;
    }

    #region Constructors that used by test code.

    internal DocumentBuildContext(string buildOutputFolder)
        : this(buildOutputFolder, [], [], [], 1, Directory.GetCurrentDirectory(), string.Empty, null, null) { }

    private DocumentBuildContext(string buildOutputFolder, IEnumerable<FileAndType> allSourceFiles, ImmutableArray<string> externalReferencePackages, ImmutableArray<string> xrefMaps, int maxParallelism, string baseFolder, string versionName, ApplyTemplateSettings applyTemplateSetting, string rootTocPath)
        : this(buildOutputFolder, allSourceFiles, externalReferencePackages, xrefMaps, maxParallelism, baseFolder, versionName, applyTemplateSetting, rootTocPath, null, null) { }

    private DocumentBuildContext(
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
        GroupInfo groupInfo)
    {
        BuildOutputFolder = buildOutputFolder;
        VersionName = versionName;
        ApplyTemplateSettings = applyTemplateSetting;
        HrefGenerator = applyTemplateSetting?.HrefGenerator;
        AllSourceFiles = GetAllSourceFiles(allSourceFiles);
        ExternalReferencePackages = externalReferencePackages;
        GroupInfo = groupInfo;
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
            if (!versionFolder.EndsWith('/'))
            {
                versionFolder += "/";
            }
        }
        VersionFolder = versionFolder;
    }
    #endregion

    public string BuildOutputFolder { get; }

    public string VersionName { get; }

    public string VersionFolder { get; }

    public GroupInfo GroupInfo { get; }

    public ApplyTemplateSettings ApplyTemplateSettings { get; set; }

    public ImmutableArray<string> ExternalReferencePackages { get; } = [];

    public ImmutableDictionary<string, FileAndType> AllSourceFiles { get; }

    public int MaxParallelism { get; }

    public ConcurrentDictionary<string, string> FileMap { get; } = new(FilePathComparer.OSPlatformSensitiveStringComparer);

    public ConcurrentDictionary<string, XRefSpec> XRefSpecMap { get; } = new();

    public ConcurrentDictionary<string, HashSet<string>> TocMap { get; } = new(FilePathComparer.OSPlatformSensitiveStringComparer);

    public HashSet<string> XRef { get; } = [];

    public string RootTocPath { get; }

    public IMarkdownService MarkdownService { get; set; }

    public ICustomHrefGenerator HrefGenerator { get; }

    public CancellationToken CancellationToken { get; } = CancellationToken.None;

    internal ConcurrentBag<ManifestItem> ManifestItems { get; } = [];

    private ConcurrentDictionary<string, XRefSpec> ExternalXRefSpec { get; } = new();

    private ConcurrentDictionary<string, object> UnknownUids { get; } = new();

    public void ReportExternalXRefSpec(XRefSpec spec)
    {
        if (string.IsNullOrWhiteSpace(spec.Uid))
        {
            return;
        }

        ExternalXRefSpec.AddOrUpdate(
            spec.Uid,
            spec,
            (uid, old) => old + spec);
    }

    public void ResolveExternalXRefSpec()
    {
        Task.WaitAll([
            Task.Run(ResolveExternalXRefSpecForSpecs),
            Task.Run(ResolveExternalXRefSpecForNoneSpecsAsync)
        ], CancellationToken);
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

    public void ResolveExternalXRefSpecForNoneSpecsAsync()
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
        using (var externalReferences = new ExternalReferencePackageCollection(ExternalReferencePackages, MaxParallelism, CancellationToken))
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
        Logger.LogInfo($"{uidList.Count - list.Count} external references found in xref maps.");
        return list;
    }

    public string GetFilePath(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (key.Length == 0)
        {
            throw new ArgumentException("Key cannot be empty.", nameof(key));
        }
        return FileMap.GetValueOrDefault(key);
    }

    // TODO: use this method instead of directly accessing FileMap
    public void SetFilePath(string key, string filePath)
    {
        FileMap[key] = filePath;
    }

    public string UpdateHref(string href)
    {
        ArgumentNullException.ThrowIfNull(href);

        return UpdateHrefCore(href, null);
    }

    public string UpdateHref(string href, RelativePath fromFile)
    {
        ArgumentNullException.ThrowIfNull(href);

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
        ArgumentNullException.ThrowIfNull(xrefSpec);

        if (string.IsNullOrEmpty(xrefSpec.Href))
        {
            throw new ArgumentException("Href for xref spec must contain value");
        }
        if (!PathUtility.IsRelativePath(xrefSpec.Href))
        {
            throw new ArgumentException("Only relative href path is supported");
        }
        XRefSpecMap.AddOrUpdate(
            xrefSpec.Uid,
            xrefSpec,
            (_, old) =>
            {
                Logger.LogWarning($"Uid({xrefSpec.Uid}) has already been defined in {((RelativePath)old.Href).RemoveWorkingFolder()}.",
                    code: WarningCodes.Build.DuplicateUids);
                return FilePathComparer.OSPlatformSensitiveStringComparer.Compare(old.Href, xrefSpec.Href) > 0 ? xrefSpec : old;
            });
    }

    public void RegisterInternalXrefSpecBookmark(string uid, string bookmark)
    {
        ArgumentNullException.ThrowIfNull(uid);

        if (uid.Length == 0)
        {
            throw new ArgumentException("Uid cannot be empty", nameof(uid));
        }

        ArgumentNullException.ThrowIfNull(bookmark);
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
        ArgumentNullException.ThrowIfNull(uid);

        if (string.IsNullOrWhiteSpace(uid))
        {
            return null;
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
            using (var externalReferences = new ExternalReferencePackageCollection(ExternalReferencePackages, MaxParallelism, CancellationToken))
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
        ArgumentNullException.ThrowIfNull(key);

        if (TocMap.TryGetValue(key, out HashSet<string> sets))
        {
            return sets.ToImmutableArray();
        }

        return null;
    }

    public void RegisterToc(string tocFileKey, string fileKey)
    {
        ArgumentException.ThrowIfNullOrEmpty(fileKey);
        ArgumentException.ThrowIfNullOrEmpty(tocFileKey);

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

    private static ImmutableDictionary<string, FileAndType> GetAllSourceFiles(IEnumerable<FileAndType> allSourceFiles)
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
