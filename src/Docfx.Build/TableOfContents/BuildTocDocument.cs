// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Composition;
using System.Globalization;
using System.Web;
using Docfx.Build.Common;
using Docfx.Common;
using Docfx.DataContracts.Common;
using Docfx.Plugins;

namespace Docfx.Build.TableOfContents;

[Export(nameof(TocDocumentProcessor), typeof(IDocumentBuildStep))]
class BuildTocDocument : BaseDocumentBuildStep
{
    private static readonly TextInfo TextInfo = new CultureInfo("en-US", false).TextInfo;

    public override string Name => nameof(BuildTocDocument);

    public override int BuildOrder => 0;

    /// <summary>
    /// 1. Expand the TOC reference
    /// 2. Resolve homepage
    /// </summary>
    public override IEnumerable<FileModel> Prebuild(ImmutableList<FileModel> models, IHostService host)
    {
        if (!models.Any())
        {
            return TocHelper.ResolveToc(models.ToImmutableList());
        }

        // Compute set of folders that contain TOC files
        var tocFolders = models
            .Select(m => NormalizePath(Path.GetDirectoryName(m.File)))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Strip the ~/ prefix from source file paths to match model.File paths
        var sourceFiles = host.SourceFiles.Keys
            .Select(StripTildePrefix)
            .ToList();

        // Auto-populate TOC items BEFORE resolving (so they get normalized too)
        foreach (var model in models)
        {
            var toc = (TocItemViewModel)model.Content;
            if (toc.Auto == true)
            {
                var tocFolder = NormalizePath(Path.GetDirectoryName(model.File));
                PopulateAutoToc(toc, tocFolder, tocFolder, sourceFiles, tocFolders);
            }
        }

        return TocHelper.ResolveToc(models);
    }

    public override void Build(FileModel model, IHostService host)
    {
        var toc = (TocItemViewModel)model.Content;

        TocRestructureUtility.Restructure(toc, host.TableOfContentRestructions);
        BuildCore(toc, model);
    }

    private static void PopulateAutoToc(
        TocItemViewModel toc,
        string tocRootFolder,
        string folder,
        List<string> allFiles,
        HashSet<string> tocFolders)
    {
        toc.Items ??= [];

        // Get files directly in this folder (excluding TOC files)
        var filesInFolder = allFiles
            .Where(f => NormalizePath(Path.GetDirectoryName(f)) == folder)
            .Where(f => !IsTocFile(f))
            .OrderBy(f => f);

        foreach (var file in filesInFolder)
        {
            var fileName = Path.GetFileName(file);
            // Skip if already in TOC (strip ~/ prefix for comparison)
            var existingItem = toc.Items.FirstOrDefault(i => i.Href != null &&
                (StripTildePrefix(i.Href).Equals(file, StringComparison.OrdinalIgnoreCase) ||
                 StripTildePrefix(i.Href).Equals(fileName, StringComparison.OrdinalIgnoreCase) ||
                 Path.GetFileName(StripTildePrefix(i.Href)).Equals(fileName, StringComparison.OrdinalIgnoreCase)));
            if (existingItem != null)
            {
                continue;
            }

            // Compute href relative to the TOC file's folder
            var href = GetRelativeHref(file, tocRootFolder);
            toc.Items.Add(new TocItemViewModel
            {
                Name = StandardizeName(Path.GetFileNameWithoutExtension(file)),
                Href = href
            });
        }

        // Find immediate subfolders that don't have their own TOC
        var subfolders = allFiles
            .Select(f => NormalizePath(Path.GetDirectoryName(f)))
            .Where(d => IsDirectChild(d, folder))
            .Where(d => !tocFolders.Contains(d))
            .Distinct()
            .OrderBy(d => d);

        foreach (var subfolder in subfolders)
        {
            var subfolderName = Path.GetFileName(subfolder);

            // Skip if already in TOC
            if (toc.Items.Any(i => i.Name?.Equals(subfolderName, StringComparison.OrdinalIgnoreCase) == true))
            {
                continue;
            }

            var subItem = new TocItemViewModel
            {
                Name = StandardizeName(subfolderName)
            };

            // Recursively populate the subfolder (keep the same tocRootFolder)
            PopulateAutoToc(subItem, tocRootFolder, subfolder, allFiles, tocFolders);

            // Only add if it has content
            if (subItem.Items?.Count > 0)
            {
                toc.Items.Add(subItem);
            }
        }
    }

    /// <summary>
    /// Computes the relative href from a file path relative to the TOC file's folder.
    /// </summary>
    private static string GetRelativeHref(string filePath, string tocFolder)
    {
        // If TOC is at root (empty folder), use the full path
        if (string.IsNullOrEmpty(tocFolder))
        {
            return filePath;
        }

        // If file is in the same folder as TOC, use just the filename
        var fileFolder = NormalizePath(Path.GetDirectoryName(filePath));
        if (fileFolder.Equals(tocFolder, StringComparison.OrdinalIgnoreCase))
        {
            return Path.GetFileName(filePath);
        }

        // File is in a subfolder relative to TOC, compute relative path
        if (filePath.StartsWith(tocFolder + "/", StringComparison.OrdinalIgnoreCase))
        {
            return filePath[(tocFolder.Length + 1)..];
        }

        // Fallback to just the filename
        return Path.GetFileName(filePath);
    }

    private static bool IsDirectChild(string potentialChild, string parent)
    {
        if (string.IsNullOrEmpty(potentialChild))
            return false;

        // Handle root folder case (parent is empty string)
        if (string.IsNullOrEmpty(parent))
        {
            // Direct child of root should not contain any '/'
            return !potentialChild.Contains('/');
        }

        if (!potentialChild.StartsWith(parent + "/", StringComparison.OrdinalIgnoreCase))
            return false;

        // Check there's no additional '/' after the parent path
        var remainder = potentialChild[(parent.Length + 1)..];
        return !remainder.Contains('/');
    }

    private static bool IsTocFile(string file)
    {
        var fileName = Path.GetFileName(file);
        return fileName.Equals("toc.yml", StringComparison.OrdinalIgnoreCase) ||
               fileName.Equals("toc.md", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizePath(string path)
        => path?.Replace("\\", "/") ?? string.Empty;

    private static string StripTildePrefix(string path)
        => path?.StartsWith("~/") == true ? path[2..] : path;

    private static string StandardizeName(string name)
        => TextInfo.ToTitleCase(HttpUtility.UrlDecode(name)).Replace('-', ' ');

    private static void BuildCore(TocItemViewModel item, FileModel model, string includedFrom = null)
    {
        if (item == null)
        {
            return;
        }

        var linkToUids = new HashSet<string>();
        var linkToFiles = new HashSet<string>();
        var uidLinkSources = new Dictionary<string, ImmutableList<LinkSourceInfo>>();
        var fileLinkSources = new Dictionary<string, ImmutableList<LinkSourceInfo>>();

        if (Utility.IsSupportedRelativeHref(item.Href))
        {
            UpdateDependencies(linkToFiles, fileLinkSources, item.Href);
        }
        if (Utility.IsSupportedRelativeHref(item.Homepage))
        {
            UpdateDependencies(linkToFiles, fileLinkSources, item.Homepage);
        }
        if (!string.IsNullOrEmpty(item.TopicUid))
        {
            UpdateDependencies(linkToUids, uidLinkSources, item.TopicUid);
        }

        model.LinkToUids = model.LinkToUids.Union(linkToUids);
        model.LinkToFiles = model.LinkToFiles.Union(linkToFiles);
        model.UidLinkSources = model.UidLinkSources.Merge(uidLinkSources);
        model.FileLinkSources = model.FileLinkSources.Merge(fileLinkSources);

        includedFrom = item.IncludedFrom ?? includedFrom;
        if (item.Items != null)
        {
            foreach (var i in item.Items)
            {
                BuildCore(i, model, includedFrom);
            }
        }

        void UpdateDependencies(HashSet<string> linkTos, Dictionary<string, ImmutableList<LinkSourceInfo>> linkSources, string link)
        {
            var path = HttpUtility.UrlDecode(UriUtility.GetPath(link));
            var anchor = UriUtility.GetFragment(link);
            linkTos.Add(path);
            AddOrUpdate(linkSources, path, GetLinkSourceInfo(path, anchor, model.File, includedFrom));
        }
    }

    private static void AddOrUpdate(Dictionary<string, ImmutableList<LinkSourceInfo>> dict, string path, LinkSourceInfo source)
        => dict[path] = dict.TryGetValue(path, out var sources) ? sources.Add(source) : [source];

    private static LinkSourceInfo GetLinkSourceInfo(string path, string anchor, string source, string includedFrom)
    {
        return new LinkSourceInfo
        {
            SourceFile = includedFrom ?? source,
            Anchor = anchor,
            Target = path,
        };
    }
}
