// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Docfx.Common;
using Docfx.DataContracts.Common;
using Docfx.Plugins;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using Constants = Docfx.DataContracts.Common.Constants;

namespace Docfx.Build.TableOfContents;

public static class TocHelper
{
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
            switch (fileType)
            {
                case TocFileType.Markdown:
                    return new()
                    {
                        Items = MarkdownTocReader.LoadToc(EnvironmentContext.FileAbstractLayer.ReadAllText(file), file)
                    };
                case TocFileType.Yaml:
                    {
                        var yaml = EnvironmentContext.FileAbstractLayer.ReadAllText(file);
                        return DeserializeYamlToc(yaml);
                    }
                default:
                    throw new NotSupportedException($"{file} is not a valid TOC file, supported TOC files should be either \"{Constants.TableOfContents.MarkdownTocFileName}\" or \"{Constants.TableOfContents.YamlTocFileName}\".");
            }
        }
        catch (Exception e)
        {
            var message = $"{file} is not a valid TOC File: {e}";
            Logger.LogError(message, code: ErrorCodes.Toc.InvalidTocFile);
            throw new DocumentException(message, e);
        }
    }

    private static TocItemViewModel DeserializeYamlToc(string yaml)
    {
        // Parse yaml content to determine TOC type (`List<TocItemViewModel>` or TocItemViewModel).
        var parser = new Parser(new Scanner(new StringReader(yaml), skipComments: true));
        bool isListItems = parser.TryConsume<StreamStart>(out var _)
                        && parser.TryConsume<DocumentStart>(out var _)
                        && parser.TryConsume<SequenceStart>(out var _);

        return isListItems
            ? new TocItemViewModel { Items = YamlUtility.Deserialize<List<TocItemViewModel>>(new StringReader(yaml)) }
            : YamlUtility.Deserialize<TocItemViewModel>(new StringReader(yaml));
    }
}
