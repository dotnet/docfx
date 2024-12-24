// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Web;
using Docfx.Common;
using Docfx.DataContracts.Common;
using Docfx.Plugins;

namespace Docfx.Build.TableOfContents;

class TocResolver
{
    private readonly Dictionary<string, TocItemInfo> _collection;
    private readonly Dictionary<FileAndType, TocItemInfo> _notInProjectTocCache = [];

    public TocResolver(Dictionary<string, TocItemInfo> collection)
    {
        _collection = collection;
    }

    public TocItemInfo Resolve(string file)
    {
        return ResolveItem(_collection[file], new Stack<FileAndType>());
    }

    private TocItemInfo ResolveItem(TocItemInfo wrapper, Stack<FileAndType> stack)
    {
        using (new LoggerFileScope(wrapper.File.File))
        {
            return ResolveItemCore(wrapper, stack);
        }
    }

    private TocItemInfo ResolveItemCore(TocItemInfo wrapper, Stack<FileAndType> stack)
    {
        if (wrapper.IsResolved)
        {
            return wrapper;
        }

        var file = wrapper.File;
        if (stack.Contains(file))
        {
            var message = $"Circular reference to {file.FullPath} is found in {stack.Peek().FullPath}";
            Logger.LogError(message, code: ErrorCodes.Toc.CircularTocInclusion);
            throw new DocumentException(message);
        }

        if (wrapper.Content == null)
        {
            Logger.LogWarning("Empty TOC item node found.", code: WarningCodes.Build.EmptyTocItemNode);
            return null;
        }

        var item = wrapper.Content;

        // HomepageUid and Uid is deprecated, unified to TopicUid
        if (string.IsNullOrEmpty(item.TopicUid))
        {
            if (!string.IsNullOrEmpty(item.Uid))
            {
                item.TopicUid = item.Uid;
                item.Uid = null;
            }
            else if (!string.IsNullOrEmpty(item.HomepageUid))
            {
                item.TopicUid = item.HomepageUid;
                Logger.LogWarning($"HomepageUid is deprecated in TOC. Please use topicUid to specify uid {item.Homepage}");
                item.HomepageUid = null;
            }
        }
        // Homepage is deprecated, unified to TopicHref
        if (!string.IsNullOrEmpty(item.Homepage))
        {
            if (string.IsNullOrEmpty(item.TopicHref))
            {
                item.TopicHref = item.Homepage;
            }
            else
            {
                Logger.LogWarning($"Homepage is deprecated in TOC. Homepage {item.Homepage} is overwritten with topicHref {item.TopicHref}");
            }
        }
        // validate href
        ValidateHref(item);

        // TocHref supports 2 forms: absolute path and local toc file.
        // When TocHref is set, using TocHref as Href in output, and using Href as Homepage in output
        var tocHrefType = Utility.GetHrefType(item.TocHref);

        // check whether toc exists
        TocItemInfo tocFileModel = null;
        if (!string.IsNullOrEmpty(item.TocHref) && (tocHrefType == HrefType.MarkdownTocFile || tocHrefType == HrefType.YamlTocFile))
        {
            var tocFilePath = (RelativePath)file.File + (RelativePath)item.TocHref;
            var tocFile = file.ChangeFile(tocFilePath);
            if (!_collection.TryGetValue(tocFile.FullPath, out tocFileModel))
            {
                var message = $"Unable to find {item.TocHref}. Make sure the file is included in config file docfx.json!";
                Logger.LogWarning(message);
            }
        }

        if (!string.IsNullOrEmpty(item.TocHref))
        {
            if (!string.IsNullOrEmpty(item.Homepage))
            {
                var message = $"TopicHref should be used to specify the homepage for {item.TocHref} when tocHref is used.";
                Logger.LogError(message, code: ErrorCodes.Build.TopicHrefNotset);
                throw new DocumentException(message);
            }
            if (tocHrefType == HrefType.RelativeFile || tocHrefType == HrefType.RelativeFolder)
            {
                var message = $"TocHref {item.TocHref} only supports absolute path or local toc file.";
                Logger.LogError(message, code: ErrorCodes.Build.UnsupportedTocHrefType);
                throw new DocumentException(message);
            }
        }

        var hrefType = Utility.GetHrefType(item.Href);
        switch (hrefType)
        {
            case HrefType.AbsolutePath:
            case HrefType.RelativeFile:
                if (item.Items is { Count: > 0 })
                {
                    item.Items = new List<TocItemViewModel>(from i in item.Items
                                                            select ResolveItem(new TocItemInfo(file, i), stack) into r
                                                            where r != null
                                                            select r.Content);
                    if (string.IsNullOrEmpty(item.TopicHref) && string.IsNullOrEmpty(item.TopicUid))
                    {
                        var defaultItem = GetDefaultHomepageItem(item);
                        if (defaultItem != null)
                        {
                            item.AggregatedHref = defaultItem.TopicHref;
                            item.AggregatedUid = defaultItem.TopicUid;
                        }
                    }
                }
                if (string.IsNullOrEmpty(item.TopicHref))
                {
                    // Get homepage from TocHref if href/topicHref is null or empty
                    if (string.IsNullOrEmpty(item.Href) && string.IsNullOrEmpty(item.TopicUid) && tocFileModel != null)
                    {
                        stack.Push(file);
                        var resolved = ResolveItem(tocFileModel, stack).Content;
                        stack.Pop();
                        item.Href = resolved.TopicHref ?? resolved.AggregatedHref;
                        item.TopicUid = resolved.TopicUid ?? resolved.AggregatedUid;
                    }
                    // Use TopicHref in output model
                    item.TopicHref = item.Href;
                }
                break;
            case HrefType.RelativeFolder:
                {
                    if (tocFileModel != null)
                    {
                        Logger.LogWarning($"Href {item.Href} is overwritten by tocHref {item.TocHref}");
                    }
                    else
                    {
                        var relativeFolder = (RelativePath)file.File + (RelativePath)item.Href;
                        var tocFilePath = relativeFolder + (RelativePath)Constants.TableOfContents.YamlTocFileName;

                        var tocFile = file.ChangeFile(tocFilePath);

                        // First, try finding toc.yml under the relative folder
                        // Second, try finding toc.md under the relative folder
                        if (!_collection.TryGetValue(tocFile.FullPath, out tocFileModel))
                        {
                            tocFilePath = relativeFolder + (RelativePath)Constants.TableOfContents.MarkdownTocFileName;
                            tocFile = file.ChangeFile(tocFilePath);
                            if (!_collection.TryGetValue(tocFile.FullPath, out tocFileModel))
                            {
                                var message =
                                    $"Unable to find either {Constants.TableOfContents.YamlTocFileName} or {Constants.TableOfContents.MarkdownTocFileName} inside {item.Href}. Make sure the file is included in config file docfx.json!";
                                Logger.LogWarning(message);
                                break;
                            }
                        }

                        item.TocHref = tocFilePath - (RelativePath)file.File;
                    }

                    // Get homepage from TocHref if TopicHref/TopicUid is not specified
                    if (string.IsNullOrEmpty(item.TopicHref) && string.IsNullOrEmpty(item.TopicUid))
                    {
                        stack.Push(file);
                        var resolved = ResolveItem(tocFileModel, stack).Content;
                        stack.Pop();
                        item.Href = item.TopicHref = resolved.TopicHref ?? resolved.AggregatedHref;
                        item.TopicUid = resolved.TopicUid ?? resolved.AggregatedUid;
                    }
                    else
                    {
                        item.Href = item.TopicHref;
                    }

                    if (string.IsNullOrEmpty(item.Name) && !string.IsNullOrEmpty(item.TocHref))
                    {
                        item.Name = GetTocItemName(item.TocHref);
                    }

                    if (item.Items != null)
                    {
                        for (int i = 0; i < item.Items.Count; i++)
                        {
                            item.Items[i] = ResolveItem(new TocItemInfo(file, item.Items[i]), stack).Content;
                        }
                    }
                }
                break;
            case HrefType.MarkdownTocFile:
            case HrefType.YamlTocFile:
                {
                    if (string.IsNullOrEmpty(item.Name) && !string.IsNullOrEmpty(item.Href))
                    {
                        item.Name = GetTocItemName(item.Href);
                    }

                    item.IncludedFrom = item.Href;

                    var href = (RelativePath)item.Href;
                    var tocFilePath = (RelativePath)file.File + href;
                    var tocFile = file.ChangeFile(tocFilePath);
                    stack.Push(file);
                    var referencedToc = GetReferencedToc(tocFile, stack);
                    stack.Pop();
                    // For referenced toc, content from referenced toc is expanded as the items of current toc item,
                    // Href is reset to the homepage of current toc item
                    item.Href = item.TopicHref;

                    var referencedTocClone = referencedToc?.Items?.ConvertAll(s => s.Clone());

                    // For [reference](a/toc.md), and toc.md contains not-exist.md, the included not-exist.md should be resolved to a/not-exist.md
                    item.Items = UpdateOriginalHref(referencedTocClone, href);
                }
                break;
            default:
                break;
        }

        var relativeToFile = (RelativePath)file.File;

        item.OriginalHref = item.Href;
        item.OriginalTocHref = item.TocHref;
        item.OriginalTopicHref = item.TopicHref;
        item.OriginalHomepage = item.Homepage;
        item.Href = NormalizeHref(item.Href, relativeToFile);
        item.TocHref = NormalizeHref(item.TocHref, relativeToFile);
        item.TopicHref = NormalizeHref(item.TopicHref, relativeToFile);
        item.Homepage = NormalizeHref(item.Homepage, relativeToFile);
        item.IncludedFrom = NormalizeHref(item.IncludedFrom, relativeToFile);

        wrapper.IsResolved = true;

        // for backward compatibility
        if (item.Href == null && item.Homepage == null)
        {
            item.Href = item.TocHref;
            item.Homepage = item.TopicHref;
        }

        return wrapper;
    }

    private TocItemViewModel GetReferencedToc(FileAndType tocFile, Stack<FileAndType> stack)
    {
        if (_collection.TryGetValue(tocFile.FullPath, out TocItemInfo referencedTocFileModel) || _notInProjectTocCache.TryGetValue(tocFile, out referencedTocFileModel))
        {
            referencedTocFileModel = ResolveItem(referencedTocFileModel, stack);
            referencedTocFileModel.IsReferenceToc = true;
            return referencedTocFileModel.Content;
        }
        else
        {
            // It is acceptable that the referenced toc file is not included in docfx.json, as long as it can be found locally
            TocItemViewModel referencedTocItemViewModel;
            try
            {
                referencedTocItemViewModel = TocHelper.LoadSingleToc(tocFile.FullPath);
            }
            catch (FileNotFoundException)
            {
                Logger.LogError($"Referenced TOC file {tocFile.FullPath} does not exist.", code: WarningCodes.Build.InvalidTocInclude);
                return null;
            }

            referencedTocFileModel = new TocItemInfo(tocFile, referencedTocItemViewModel);

            referencedTocFileModel = ResolveItem(referencedTocFileModel, stack);
            _notInProjectTocCache[tocFile] = referencedTocFileModel;
            return referencedTocFileModel.Content;
        }
    }

    private static List<TocItemViewModel> UpdateOriginalHref(List<TocItemViewModel> toc, RelativePath relativePath)
    {
        if (toc == null || relativePath.SubdirectoryCount == 0)
        {
            return toc;
        }

        foreach (var item in toc)
        {
            item.OriginalHomepage = GetRelativePath(item.OriginalHomepage, relativePath);
            item.OriginalHref = GetRelativePath(item.OriginalHref, relativePath);
            item.OriginalTocHref = GetRelativePath(item.OriginalTocHref, relativePath);
            item.OriginalTopicHref = GetRelativePath(item.OriginalTopicHref, relativePath);
            item.Items = UpdateOriginalHref(item.Items, relativePath);
        }

        return toc;
    }

    private static string GetRelativePath(string href, RelativePath rel)
    {
        var type = Utility.GetHrefType(href);
        if (type == HrefType.RelativeFile)
        {
            return rel + (RelativePath)href;
        }
        return href;
    }

    private static string NormalizeHref(string href, RelativePath relativeToFile)
    {
        if (!Utility.IsSupportedRelativeHref(href))
        {
            return href;
        }
        RelativePath relativeToTargetFile;
        try
        {
            relativeToTargetFile = RelativePath.Parse(href);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex.Message, code: WarningCodes.Build.InvalidFileLink);
            return href;
        }

        return (relativeToFile + relativeToTargetFile).GetPathFromWorkingFolder();
    }

    private TocItemViewModel GetDefaultHomepageItem(TocItemViewModel toc)
    {
        if (toc?.Items == null)
        {
            return null;
        }

        foreach (var item in toc.Items)
        {
            var tocItem = TreeIterator.PreorderFirstOrDefault(item, s => s.Items, IsValidHomepageLink);
            if (tocItem != null)
            {
                return tocItem;
            }
        }
        return null;
    }

    /// <summary>
    /// Valid homepage href should:
    /// 1. relative file path
    /// 2. refer to a file
    /// 3. folder is not supported
    /// 4. refer to an `uid`
    /// </summary>
    /// <param name="href"></param>
    /// <returns></returns>
    private bool IsValidHomepageLink(TocItemViewModel tocItem)
    {
        if (!string.IsNullOrEmpty(tocItem.TopicUid))
        {
            return true;
        }

        var hrefType = Utility.GetHrefType(tocItem.Href);
        if (hrefType == HrefType.RelativeFile)
        {
            return true;
        }

        return false;
    }

    private static void ValidateHref(TocItemViewModel item)
    {
        if (item.Href == null)
        {
            return;
        }
        var hrefType = Utility.GetHrefType(item.Href);
        if ((hrefType == HrefType.MarkdownTocFile || hrefType == HrefType.YamlTocFile || hrefType == HrefType.RelativeFolder) &&
            (UriUtility.HasFragment(item.Href) || UriUtility.HasQueryString(item.Href)))
        {
            Logger.LogWarning($"Illegal href: {item.Href}.`#` or `?` aren't allowed when referencing toc file.");
            item.Href = UriUtility.GetPath(item.Href);
        }
    }

    private static string GetTocItemName(string href)
    {
        return HttpUtility.UrlDecode(href.TrimEnd("toc.yml").TrimEnd('/', '\\').Replace('-', ' ').Replace('_', ' '));
    }
}
