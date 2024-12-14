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

[Export(nameof(TocDocumentProcessor), typeof(IDocumentBuildStep))]
class BuildTocDocument : BaseDocumentBuildStep
{
    public override string Name => nameof(BuildTocDocument);

    public override int BuildOrder => 0;

    /// <summary>
    /// 1. Expand the TOC reference
    /// 2. Resolve homepage
    /// </summary>
    public override IEnumerable<FileModel> Prebuild(ImmutableList<FileModel> models, IHostService host)
    {
        return TocHelper.ResolveToc(models);
    }

    public override void Build(FileModel model, IHostService host)
    {
        var toc = (TocItemViewModel)model.Content;
        TocRestructureUtility.Restructure(toc, host.TableOfContentRestructions);
        BuildCore(toc, model);
        // todo : metadata.
    }

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
