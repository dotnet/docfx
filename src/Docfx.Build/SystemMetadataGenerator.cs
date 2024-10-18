// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Common;
using Docfx.Plugins;

using CommonConstants = Docfx.DataContracts.Common.Constants;

namespace Docfx.Build.Engine;

internal sealed class SystemMetadataGenerator
{
    private readonly IDocumentBuildContext _context;
    private readonly Dictionary<string, FileInfo> _toc;

    public SystemMetadataGenerator(IDocumentBuildContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        _context = context;

        // Order toc files by the output folder depth
        _toc = context.GetTocInfo()
            .Select(s => new FileInfo(s.Order, s.TocFileKey, context.GetFilePath(s.TocFileKey)))
            .Where(s => s.RelativePath != null)
            .OrderBy(s => s.RelativePath.SubdirectoryCount)
            .ToDictionary(s => s.Key);
    }

    public SystemMetadata Generate(InternalManifestItem item)
    {
        var attrs = new SystemMetadata();
        var key = GetFileKey(item.Key);
        attrs.Key = ((RelativePath)key).RemoveWorkingFolder();
        var file = (RelativePath)(item.FileWithoutExtension + item.Extension);

        attrs.Rel = RelativePath.Empty.MakeRelativeTo(file);
        var fileWithoutWorkingFolder = file.RemoveWorkingFolder();
        attrs.Path = fileWithoutWorkingFolder;

        if (!string.IsNullOrEmpty(_context.VersionName))
        {
            attrs.Version = _context.VersionName;
            attrs.VersionPath = _context.VersionFolder;
        }

        // 1. Root Toc is specified by RootTocKey, or by default in the top directory of output folder
        if (!string.IsNullOrEmpty(_context.RootTocPath))
        {
            attrs.NavKey = _context.RootTocPath;
            var rootTocPath = ((RelativePath)_context.RootTocPath).RemoveWorkingFolder();
            attrs.NavPath = rootTocPath;
            attrs.NavRel = rootTocPath.MakeRelativeTo(file);
        }
        else
        {
            GetRootTocFromOutputRoot(attrs, file);
        }

        if (item.DocumentType == CommonConstants.DocumentType.Toc)
        {
            // when item is toc, its toc is always itself
            attrs.TocPath = item.FileWithoutExtension + item.Extension;
            attrs.TocRel = Path.GetFileName(item.FileWithoutExtension) + item.Extension;
            attrs.TocKey = item.Key;
            return attrs;
        }

        // 2. The algorithm of toc current article belongs to:
        //    a. If toc can be found in TocMap, return that toc
        //    b. Elsewise, get the nearest toc, **nearest** means nearest toc in **OUTPUT** folder
        var parentTocFiles = _context.GetTocFileKeySet(key)?.Select(s => _toc[s]);
        var parentToc = GetNearestToc(parentTocFiles, file) ?? GetDefaultToc(key);

        if (parentToc != null)
        {
            var parentTocPath = parentToc.RelativePath.RemoveWorkingFolder();
            attrs.TocPath = parentTocPath;
            var tocRelativePath = parentTocPath.MakeRelativeTo(file);
            attrs.TocRel = tocRelativePath;
            attrs.TocKey = parentToc.Key;
            Logger.LogDiagnostic($"TOC file {parentTocPath} is found for {item.LocalPathFromRoot}.");
        }
        else
        {
            Logger.LogDiagnostic($"TOC file for {item.LocalPathFromRoot} is not found.");
        }

        return attrs;
    }

    private void GetRootTocFromOutputRoot(SystemMetadata attrs, RelativePath file)
    {
        var rootToc = _toc.Values.FirstOrDefault();
        if (rootToc != null)
        {
            var rootTocPath = rootToc.RelativePath.RemoveWorkingFolder();
            if (rootTocPath.SubdirectoryCount == 0)
            {
                attrs.NavPath = rootTocPath;
                var rootTocRelativePath = rootTocPath.MakeRelativeTo(file);
                attrs.NavRel = rootTocRelativePath;
                attrs.NavKey = rootToc.Key;
                Logger.LogDiagnostic($"Root TOC file {rootTocPath} is found.");
            }
            else
            {
                Logger.LogDiagnostic(
                    $"Root TOC file from output folder is not found, the toppest TOC file is {rootTocPath}");
            }
        }
    }

    private FileInfo GetDefaultToc(string fileKey)
    {
        var outputPath = (RelativePath)_context.GetFilePath(fileKey);

        // MakeRelativeTo calculates how to get file "s" from "outputPath"
        // The standard for being the toc of current file is: Relative directory is empty or ".."s only
        var parentTocs = _toc.Values
            .Select(s => new { rel = s.RelativePath.MakeRelativeTo(outputPath), info = s })
            .Where(s => s.rel.SubdirectoryCount == 0)
            .OrderBy(s => s.info.Order)
            .ThenBy(s => s.rel.ParentDirectoryCount)
            .Select(s => s.info);

        return parentTocs.FirstOrDefault();
    }

    /// <summary>
    /// return the nearest toc relative to the current file
    /// "near" means less subdirectory count
    /// when subdirectory counts are same, "near" means less parent directory count
    /// e.g. "../../a/TOC.md" is nearer than "b/c/TOC.md"
    /// </summary>
    private static FileInfo GetNearestToc(IEnumerable<FileInfo> tocFiles, RelativePath file)
    {
        if (tocFiles == null)
        {
            return null;
        }
        return (from toc in tocFiles
                where toc.RelativePath != null
                let relativePath = toc.RelativePath.RemoveWorkingFolder() - file
                orderby toc.Order, relativePath.SubdirectoryCount, relativePath.ParentDirectoryCount, toc.FilePath, toc.Key
                select toc).FirstOrDefault();
    }

    private static string GetFileKey(string key)
    {
        if (key.StartsWith(RelativePath.NormalizedWorkingFolder, StringComparison.Ordinal)) return key;
        return RelativePath.NormalizedWorkingFolder + key;
    }

    class FileInfo
    {
        public int Order { get; }

        public string Key { get; }

        public string FilePath { get; }

        public RelativePath RelativePath { get; }

        public FileInfo(int order, string key, string filePath)
        {
            Order = order;
            Key = key;
            FilePath = filePath;
            RelativePath = (RelativePath)filePath;
        }
    }
}
