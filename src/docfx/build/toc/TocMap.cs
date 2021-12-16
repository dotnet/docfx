// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build;

/// <summary>
/// The mappings between toc and document
/// </summary>
internal class TocMap
{
    private readonly Config _config;
    private readonly Input _input;
    private readonly ErrorBuilder _errors;
    private readonly BuildScope _buildScope;
    private readonly TocLoader _tocLoader;
    private readonly TocParser _tocParser;
    private readonly DocumentProvider _documentProvider;
    private readonly DependencyMapBuilder _dependencyMapBuilder;
    private readonly ContentValidator _contentValidator;
    private readonly PublishUrlMap _publishUrlMap;

    private readonly Watch<(FilePath[] tocs, Dictionary<FilePath, FilePath[]> docToTocs, List<FilePath> servicePages)> _tocs;

    public TocMap(
        Config config,
        ErrorBuilder errors,
        Input input,
        BuildScope buildScope,
        DependencyMapBuilder dependencyMapBuilder,
        TocParser tocParser,
        TocLoader tocLoader,
        DocumentProvider documentProvider,
        ContentValidator contentValidator,
        PublishUrlMap publishUrlMap)
    {
        _config = config;
        _errors = errors;
        _input = input;
        _buildScope = buildScope;
        _tocParser = tocParser;
        _tocLoader = tocLoader;
        _documentProvider = documentProvider;
        _dependencyMapBuilder = dependencyMapBuilder;
        _contentValidator = contentValidator;
        _publishUrlMap = publishUrlMap;
        _tocs = new(BuildTocMap);
    }

    public IEnumerable<FilePath> GetFiles()
    {
        return _tocs.Value.tocs.Concat(_tocs.Value.servicePages);
    }

    /// <summary>
    /// Find the toc relative path to document
    /// </summary>
    /// <param name="file">Document</param>
    /// <returns>The toc relative path</returns>
    public string? FindTocRelativePath(FilePath file)
    {
        var nearestToc = FindNearestToc(file);
        if (nearestToc is null)
        {
            return null;
        }

        _dependencyMapBuilder.AddDependencyItem(file, nearestToc, DependencyType.Metadata);
        return UrlUtility.GetRelativeUrl(_documentProvider.GetSiteUrl(file), _documentProvider.GetSiteUrl(nearestToc));
    }

    /// <summary>
    /// Return the nearest toc relative to the current file
    /// "near" means less subdirectory count
    /// when subdirectory counts are same, "near" means less parent directory count
    /// e.g. "../../a/TOC.md" is nearer than "b/c/TOC.md".
    /// when the file is not referenced, return only toc in the same or higher folder level.
    /// </summary>
    internal FilePath? FindNearestToc(FilePath file)
    {
        var (toc, hasReferencedTocs) = FindNearestToc(
            file,
            _tocs.Value.tocs,
            _tocs.Value.docToTocs,
            file => file.Path);

        _contentValidator.ValidateTocMissing(file, hasReferencedTocs);
        return toc;
    }

    /// <summary>
    /// Compare two toc candidate relative to target file.
    /// Return negative if x is closer than y, positive if x is farer than y, 0 if x equals y.
    /// 1. sub nearest(based on file path)
    /// 2. parent nearest(based on file path)
    /// 3. sub-name lexicographical nearest
    /// </summary>
    internal static (T? toc, bool hasReferencedTocs) FindNearestToc<T>(
        T file, IEnumerable<T> tocs, Dictionary<T, T[]> documentsToTocs, Func<T, string> getPath) where T : class, IComparable<T>
    {
        var hasReferencedTocs = false;
        var filteredTocs = (hasReferencedTocs = documentsToTocs.TryGetValue(file, out var referencedTocFiles)) ? referencedTocFiles : tocs;

        var tocCandidates = from toc in filteredTocs
                            let dirInfo = GetRelativeDirectoryInfo(getPath(file), getPath(toc))
                            where hasReferencedTocs || dirInfo.subDirectoryCount == 0 /*due breadcrumb toc*/
                            select (subCount: dirInfo.subDirectoryCount, parentCount: dirInfo.parentDirectoryCount, toc);

        return (tocCandidates.DefaultIfEmpty().Aggregate((minCandidate, nextCandidate) =>
        {
            var result = minCandidate.subCount - nextCandidate.subCount;
            if (result == 0)
            {
                result = minCandidate.parentCount - nextCandidate.parentCount;
            }
            if (result == 0)
            {
                result = minCandidate.toc.CompareTo(nextCandidate.toc);
            }
            return result <= 0 ? minCandidate : nextCandidate;
        }).toc, hasReferencedTocs);
    }

    private static (int subDirectoryCount, int parentDirectoryCount) GetRelativeDirectoryInfo(string pathA, string pathB)
    {
        var relativePath = PathUtility.NormalizeFile(Path.GetDirectoryName(PathUtility.GetRelativePathToFile(pathA, pathB)) ?? "");
        if (string.IsNullOrEmpty(relativePath))
        {
            return default;
        }

        // todo: perf optimization, don't split '/' here again.
        var relativePathParts = relativePath.Split('/').Where(path => !string.IsNullOrWhiteSpace(path));
        var parentDirectoryCount = 0;
        var subDirectoryCount = 0;

        foreach (var part in relativePathParts)
        {
            switch (part)
            {
                case "..":
                    parentDirectoryCount++;
                    break;
                default:
                    break;
            }
        }

        subDirectoryCount = relativePathParts.Count() - parentDirectoryCount;
        return (subDirectoryCount, parentDirectoryCount);
    }

    private (FilePath[] tocs, Dictionary<FilePath, FilePath[]> docToTocs, List<FilePath> servicePages) BuildTocMap()
    {
        using var scope = Progress.Start("Loading TOC");

        var allTocFiles = new ConcurrentHashSet<FilePath>();
        var allTocs = new List<(FilePath file, HashSet<FilePath> docs, HashSet<FilePath> tocs, bool shouldBuildFile)>();
        var includedTocs = new HashSet<FilePath>();
        var allServicePages = new List<FilePath>();

        // Parse and split TOC
        ParallelUtility.ForEach(
            scope,
            _errors,
            _buildScope.GetFiles(ContentType.Toc),
            file => SplitToc(file, _tocParser.Parse(file, _errors), allTocFiles));

        // Load TOC
        ParallelUtility.ForEach(scope, _errors, allTocFiles, file =>
        {
            var (_, docsList, tocsList, servicePages) = _tocLoader.Load(file);
            var docs = docsList.ToHashSet();
            var tocs = tocsList.ToHashSet();
            var shouldBuildFile = tocs.Any(toc => toc.Origin != FileOrigin.Fallback);

            lock (allTocs)
            {
                allTocs.Add((file, docs, tocs, shouldBuildFile));
                allServicePages.AddRange(servicePages);
                includedTocs.AddRange(tocs);
            }
        });

        var tocToTocs = (
            from item in allTocs
            where !includedTocs.Contains(item.file)
            select item).ToDictionary(g => g.file, g => (g.tocs, g.shouldBuildFile));

        var docToTocs = (
            from item in allTocs
            where !includedTocs.Contains(item.file)
            from doc in item.docs
            group item.file by doc).ToDictionary(g => g.Key, g => g.Distinct().ToArray());

        docToTocs.TrimExcess();

        var tocFiles = _publishUrlMap.ResolveUrlConflicts(scope, tocToTocs.Keys.Where(ShouldBuildFile));

        RemoveInvalidServicePage();

        return (tocFiles, docToTocs, allServicePages);

        bool ShouldBuildFile(FilePath file)
        {
            if (file.Origin != FileOrigin.Fallback)
            {
                return true;
            }

            if (!tocToTocs.TryGetValue(file, out var value))
            {
                return false;
            }

            // if A toc includes B toc and only B toc is localized, then A need to be included and built
            return value.shouldBuildFile;
        }

        void RemoveInvalidServicePage()
        {
            for (var i = 0; i < allServicePages.Count; i++)
            {
                var servicePage = allServicePages[i];
                var url = _documentProvider.GetSiteUrl(servicePage);
                var files = _publishUrlMap.GetFilesByUrl(url);
                if (files.Any())
                {
                    _errors.Add(Errors.UrlPath.PublishUrlConflict(url, files.Concat(new[] { servicePage }), null, null));
                    allServicePages.RemoveAt(i--);
                }
            }
        }
    }

    private void SplitToc(FilePath file, TocNode toc, ConcurrentHashSet<FilePath> result)
    {
        if (!_config.SplitTOC.Contains(file.Path) || toc.Items.Count <= 0)
        {
            result.TryAdd(file);
            return;
        }

        var newToc = new TocNode(toc);

        foreach (var item in toc.Items)
        {
            var child = item.Value;
            if (child.Items.Count == 0)
            {
                newToc.Items.Add(item);
                continue;
            }

            var newNode = SplitTocNode(child);
            var newNodeToken = JsonUtility.ToJObject(newNode);
            var name = newNodeToken.TryGetValue<JValue>("name", out var splitByValue) ? splitByValue.ToString() : null;
            if (string.IsNullOrEmpty(name))
            {
                newToc.Items.Add(item);
                continue;
            }

            var newNodeFilePath = new PathString(Path.Combine(Path.GetDirectoryName(file.Path) ?? "", $"_splitted/{name}/TOC.yml"));
            var newNodeFile = FilePath.Generated(newNodeFilePath);

            _input.AddGeneratedContent(newNodeFile, new JArray { newNodeToken }, null);
            result.TryAdd(newNodeFile);

            var newChild = new TocNode(child)
            {
                Href = child.Href.With($"_splitted/{name}/"),
            };

            newToc.Items.Add(new SourceInfo<TocNode>(newChild, item.Source));
        }

        var newTocFilePath = new PathString(Path.ChangeExtension(file.Path, ".yml"));
        var newTocFile = FilePath.Generated(newTocFilePath);
        _input.AddGeneratedContent(newTocFile, JsonUtility.ToJObject(newToc), null);
        result.TryAdd(newTocFile);
    }

    private TocNode SplitTocNode(TocNode node)
    {
        var newNode = new TocNode(node)
        {
            TopicHref = node.TopicHref.With(FixHref(node.TopicHref)),
            TocHref = node.TopicHref.With(FixHref(node.TocHref)),
            Href = node.TopicHref.With(FixHref(node.Href)),
        };

        foreach (var item in node.Items)
        {
            newNode.Items.Add(item.With(SplitTocNode(item)));
        }

        return newNode;

        static string? FixHref(string? href)
        {
            return href != null && UrlUtility.GetLinkType(href) == LinkType.RelativePath ? Path.Combine("../../", href) : href;
        }
    }
}
