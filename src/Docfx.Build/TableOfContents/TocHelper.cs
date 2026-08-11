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
}
