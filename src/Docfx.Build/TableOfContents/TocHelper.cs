// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

using Docfx.Common;
using Docfx.DataContracts.Common;
using Docfx.Plugins;

namespace Docfx.Build.TableOfContents;

public static class TocHelper
{
    private static readonly YamlDeserializerWithFallback _deserializer =
        YamlDeserializerWithFallback.Create<List<TocItemViewModel>>()
        .WithFallback<TocItemViewModel>();

    internal static List<FileModel> ResolveToc(ImmutableList<FileModel> models)
    {
        var tocCache = new Dictionary<string, TocItemInfo>(FilePathComparer.OSPlatformSensitiveStringComparer);
        var nonReferencedTocModels = new List<FileModel>();

        foreach (var model in models)
        {
            tocCache[model.OriginalFileAndType.FullPath] = new TocItemInfo(model.OriginalFileAndType, (TocItemViewModel)model.Content);
        }
        var tocResolver = new TocResolver(tocCache);
        foreach (var key in tocCache.Keys.ToList())
        {
            tocCache[key] = tocResolver.Resolve(key);
        }

        foreach (var model in models)
        {
            // If the TOC file is referenced by other TOC, decrease the score
            var tocItemInfo = tocCache[model.OriginalFileAndType.FullPath];
            if (tocItemInfo.IsReferenceToc && tocItemInfo.Content.Order is null)
                tocItemInfo.Content.Order = 100;

            model.Content = tocItemInfo.Content;
            nonReferencedTocModels.Add(model);
        }

        return nonReferencedTocModels;
    }

    public static TocItemViewModel LoadSingleToc(string file)
    {
        ArgumentException.ThrowIfNullOrEmpty(file);

        if (!EnvironmentContext.FileAbstractLayer.Exists(file))
        {
            throw new FileNotFoundException($"File {file} does not exist.", file);
        }

        var fileType = Utility.GetTocFileType(file);
        try
        {
            if (fileType == TocFileType.Markdown)
            {
                return new()
                {
                    Items = MarkdownTocReader.LoadToc(EnvironmentContext.FileAbstractLayer.ReadAllText(file), file)
                };
            }
            else if (fileType == TocFileType.Yaml)
            {
                return _deserializer.Deserialize(file) switch
                {
                    List<TocItemViewModel> vm => new() { Items = vm },
                    TocItemViewModel root => root,
                    _ => throw new NotSupportedException($"{file} is not a valid TOC file."),
                };
            }
        }
        catch (Exception e)
        {
            var message = $"{file} is not a valid TOC File: {e}";
            Logger.LogError(message, code: ErrorCodes.Toc.InvalidTocFile);
            throw new DocumentException(message, e);
        }

        throw new NotSupportedException($"{file} is not a valid TOC file, supported TOC files should be either \"{Constants.TableOfContents.MarkdownTocFileName}\" or \"{Constants.TableOfContents.YamlTocFileName}\".");
    }

    private static (bool, TocItemViewModel) TryGetOrCreateToc(Dictionary<string, TocItemViewModel> pathToToc, string currentFolderPath, HashSet<string> virtualTocPaths)
    {
        bool folderHasToc = false;
        TocItemViewModel tocItem;
        if (pathToToc.TryGetValue(currentFolderPath, out tocItem))
        {
            folderHasToc = true;
        }
        else
        {
            var idx = currentFolderPath.LastIndexOf('/');
            if (idx != -1)
            {
                tocItem = new TocItemViewModel
                {
                    Name = currentFolderPath.Substring(idx + 1),
                    Auto = true
                };
                pathToToc[currentFolderPath] = tocItem;
                virtualTocPaths.Add(currentFolderPath);

            }
            else
            {
                tocItem = new TocItemViewModel();
            }
        }
        return (folderHasToc, tocItem);
    }

    private static void LinkToParentToc(Dictionary<string, TocItemViewModel> pathToToc, string currentFolderPath, TocItemViewModel tocItem, HashSet<string> virtualTocPaths, bool folderHasToc)
    {
        int idx = currentFolderPath.LastIndexOf('/');
        if (idx != -1 && !currentFolderPath.EndsWith(".."))
        {
            // This is an existing behavior, href: ~/foldername/ doesnot work, but href: ./foldername/ does.
            //var folderToProcessSanitized = currentFolderPath.Replace("~", ".") + "/";
            var parentTocFolder = currentFolderPath.Substring(0, idx);
            TocItemViewModel parentToc;
            while (!pathToToc.TryGetValue(parentTocFolder, out parentToc))
            {
                idx = parentTocFolder.LastIndexOf('/');
                parentTocFolder = currentFolderPath.Substring(0, idx);
            }

            
            if (parentToc != null)
            {
                var folderToProcessSanitized = currentFolderPath.Replace(parentTocFolder, ".") + "/";
                if (parentToc.Items == null)
                {
                    parentToc.Items = new List<TocItemViewModel>();
                }

                // Only link to parent toc if the auto is enabled.
                if (!folderHasToc &&
                    parentToc.Auto.HasValue &&
                    parentToc.Auto.Value)
                {
                    parentToc.Items.Add(tocItem);
                }
                else if (folderHasToc &&
                    parentToc.Auto.HasValue &&
                    parentToc.Auto.Value &&
                    !virtualTocPaths.Contains(currentFolderPath) &&
                    !parentToc.Items.Any(i => i.Href != null && Path.GetRelativePath(i.Href.Replace('~', '.'), folderToProcessSanitized) == "."))
                {
                    var tocToLinkFrom = new TocItemViewModel();
                    tocToLinkFrom.Name = Path.GetFileNameWithoutExtension(currentFolderPath);
                    tocToLinkFrom.Href = folderToProcessSanitized;
                    parentToc.Items.Add(tocToLinkFrom);
                }
            }
        }
    }

    internal static void PopulateToc(FileModel model, IEnumerable<string> sourceFiles, Dictionary<string, TocItemViewModel> pathToToc)
    {
        var toc = ((TocItemViewModel)model.Content);
        var tocFileName = model.Key.Split('/').Last();
        var folderPathForModel = Path.GetDirectoryName(model.Key).Replace("\\", "/");

        var fileNames = sourceFiles
            .Where(s => !Path.GetRelativePath(folderPathForModel, s).Contains("..") && !s.EndsWith(tocFileName))
            .Select(p => p.Replace("\\", "/"))
            .OrderBy(f => f.Split('/').Count());

        var virtualTocs = new HashSet<string>();
        foreach (var filePath in fileNames)
        {
            var folderToProcess = Path.GetDirectoryName(filePath).Replace("\\", "/");

            // If the folder has a toc available use it.
            var (folderHasToc, tocToProcess) = TryGetOrCreateToc(pathToToc, folderToProcess, virtualTocs);

            // Link the toc we are processing, back to a parent.
            // Look for a toc one level up until we find the root toc.
            LinkToParentToc(pathToToc, folderToProcess, tocToProcess, virtualTocs, folderHasToc);

            // If the toc we currently processed didnot have auto enabled.
            // There is no need to populate the toc, just return.
            if (tocToProcess.Auto.HasValue && !tocToProcess.Auto.Value)
            {
                continue;
            }

            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);

            if (tocToProcess.Items == null)
            {
                tocToProcess.Items = new List<TocItemViewModel>();
            }

            if (!(tocToProcess.Items.Where(i => i.Href.Equals(filePath) || i.Href.Equals(Path.GetFileName(filePath)))).Any())
            {
                var item = new TocItemViewModel();
                item.Name = fileNameWithoutExtension;
                item.Href = filePath;
                tocToProcess.Items.Add(item);
            }
        }
    }
}
