// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Composition;
using System.Web;
using Docfx.Build.Common;
using Docfx.Common;
using Docfx.DataContracts.Common;
using Docfx.Plugins;

namespace Docfx.Build.TableOfContents;

[Export(typeof(IDocumentProcessor))]
class TocDocumentProcessor : DisposableDocumentProcessor
{
    private static readonly char[] QueryStringOrAnchor = ['#', '?'];

    public override string Name => nameof(TocDocumentProcessor);

    [ImportMany(nameof(TocDocumentProcessor))]
    public override IEnumerable<IDocumentBuildStep> BuildSteps { get; set; }

    public override ProcessingPriority GetProcessingPriority(FileAndType file)
    {
        if (file.Type == DocumentType.Article && Utility.IsSupportedFile(file.File))
        {
            return ProcessingPriority.High;
        }

        return ProcessingPriority.NotSupported;
    }

    public override FileModel Load(FileAndType file, ImmutableDictionary<string, object> metadata)
    {
        var filePath = file.FullPath;
        var toc = TocHelper.LoadSingleToc(filePath);

        var displayLocalPath = PathUtility.MakeRelativePath(EnvironmentContext.BaseDirectory, file.FullPath);

        // Apply metadata to TOC
        foreach (var (key, value) in metadata.OrderBy(item => item.Key))
        {
            toc.Metadata[key] = value;
        }

        return new FileModel(file, toc)
        {
            LocalPathFromRoot = displayLocalPath
        };
    }

    public override SaveResult Save(FileModel model)
    {
        return new SaveResult
        {
            DocumentType = Constants.DocumentType.Toc,
            FileWithoutExtension = Path.ChangeExtension(model.File, null),
            LinkToFiles = model.LinkToFiles.ToImmutableArray(),
            LinkToUids = model.LinkToUids,
            FileLinkSources = model.FileLinkSources,
            UidLinkSources = model.UidLinkSources,
        };
    }

    public override void UpdateHref(FileModel model, IDocumentBuildContext context)
    {
        var toc = (TocItemViewModel)model.Content;
        UpdateTocItemHref(toc, model, context);

        RegisterTocToContext(toc, model, context);
        model.Content = toc;
    }

    private void UpdateTocItemHref(TocItemViewModel toc, FileModel model, IDocumentBuildContext context, string includedFrom = null)
    {
        if (toc.IsHrefUpdated) return;

        ResolveUid(toc, model, context, includedFrom);

        // Have to register TocMap after uid is resolved
        RegisterTocMapToContext(toc, model, context);

        toc.Homepage = ResolveHref(toc.Homepage, toc.OriginalHomepage, model, context, nameof(toc.Homepage));
        toc.OriginalHomepage = null;
        toc.Href = ResolveHref(toc.Href, toc.OriginalHref, model, context, nameof(toc.Href));
        toc.OriginalHref = null;
        toc.TocHref = ResolveHref(toc.TocHref, toc.OriginalTocHref, model, context, nameof(toc.TocHref));
        toc.OriginalTocHref = null;
        toc.TopicHref = ResolveHref(toc.TopicHref, toc.OriginalTopicHref, model, context, nameof(toc.TopicHref));
        toc.OriginalTopicHref = null;

        includedFrom = toc.IncludedFrom ?? includedFrom;
        if (toc.Items is { Count: > 0 })
        {
            foreach (var item in toc.Items)
            {
                UpdateTocItemHref(item, model, context, includedFrom);
            }
        }

        toc.IsHrefUpdated = true;
    }

    private static void ResolveUid(TocItemViewModel item, FileModel model, IDocumentBuildContext context, string includedFrom)
    {
        if (item.TopicUid != null)
        {
            var xref = GetXrefFromUid(item.TopicUid, model, context, includedFrom);
            if (xref != null)
            {
                item.Href = item.TopicHref = xref.Href;
                if (string.IsNullOrEmpty(item.Name))
                {
                    item.Name = xref.Name;
                }
            }
        }
    }

    private static XRefSpec GetXrefFromUid(string uid, FileModel model, IDocumentBuildContext context, string includedFrom)
    {
        var xref = context.GetXrefSpec(uid);
        if (xref == null)
        {
            Logger.LogWarning(
                $"Unable to find file with uid \"{uid}\" referenced by TOC file \"{includedFrom ?? model.LocalPathFromRoot}\"",
                code: WarningCodes.Build.UidNotFound,
                file: includedFrom);
        }
        return xref;
    }

    private static string ResolveHref(string pathToFile, string originalPathToFile, FileModel model, IDocumentBuildContext context, string propertyName)
    {
        if (!Utility.IsSupportedRelativeHref(pathToFile))
        {
            return pathToFile;
        }

        var index = pathToFile.IndexOfAny(QueryStringOrAnchor);
        if (index == 0)
        {
            var message = $"Invalid toc link for {propertyName}: {originalPathToFile}.";
            Logger.LogError(message, code: ErrorCodes.Toc.InvalidTocLink);
            throw new DocumentException(message);
        }

        var path = UriUtility.GetPath(pathToFile);
        var segments = UriUtility.GetQueryStringAndFragment(pathToFile);

        var fli = new FileLinkInfo(model.LocalPathFromRoot, model.File, path, context);
        var href = context.HrefGenerator?.GenerateHref(fli);

        // Check href is modified by HrefGenerator or not.
        if (href != null && href != fli.Href)
        {
            return UriUtility.MergeHref(href, segments);
        }

        if (fli.ToFileInDest == null)
        {
            // original path to file can be null for files generated by docfx in PreBuild
            var displayFilePath = string.IsNullOrEmpty(originalPathToFile) ? pathToFile : originalPathToFile;
            Logger.LogInfo($"Unable to find file \"{displayFilePath}\" for {propertyName} referenced by TOC file \"{model.LocalPathFromRoot}\"");
            return originalPathToFile;
        }

        // fragment and query in original href takes precedence over the one from hrefGenerator
        return fli.Href + segments;
    }

    private void RegisterTocToContext(TocItemViewModel toc, FileModel model, IDocumentBuildContext context)
    {
        var key = model.Key;

        // Add current folder to the toc mapping, e.g. `a/` maps to `a/toc`
        var directory = ((RelativePath)key).GetPathFromWorkingFolder().GetDirectoryPath();
        context.RegisterToc(key, directory);
        context.RegisterTocInfo(new() { TocFileKey = key, Order = toc.Order ?? 0 });
    }

    private void RegisterTocMapToContext(TocItemViewModel item, FileModel model, IDocumentBuildContext context)
    {
        var key = model.Key;
        // If tocHref is set, href is originally RelativeFolder type, and href is set to the homepage of TocHref,
        // So in this case, TocHref should be used to in TocMap
        // TODO: what if user wants to set TocHref?
        var tocHref = item.TocHref;
        var tocHrefType = Utility.GetHrefType(tocHref);
        if (tocHrefType == HrefType.MarkdownTocFile || tocHrefType == HrefType.YamlTocFile)
        {
            context.RegisterToc(key, HttpUtility.UrlDecode(UriUtility.GetPath(tocHref)));
        }
        else
        {
            var href = item.Href; // Should be original href from working folder starting with ~
            if (Utility.IsSupportedRelativeHref(href))
            {
                context.RegisterToc(key, HttpUtility.UrlDecode(UriUtility.GetPath(href)));
            }
        }
    }
}
