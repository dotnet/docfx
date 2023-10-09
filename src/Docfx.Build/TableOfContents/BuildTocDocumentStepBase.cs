﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Web;
using Docfx.Build.Common;
using Docfx.Common;
using Docfx.DataContracts.Common;
using Docfx.Plugins;

namespace Docfx.Build.TableOfContents;

public abstract class BuildTocDocumentStepBase : BaseDocumentBuildStep
{
    #region IDocumentBuildStep

    public override void Build(FileModel model, IHostService host)
    {
        var toc = (TocItemViewModel)model.Content;
        TocRestructureUtility.Restructure(toc, host.TableOfContentRestructions);
        BuildCore(toc, model, host);
        // todo : metadata.
    }

    #endregion

    #region Private methods

    private static void BuildCore(TocItemViewModel item, FileModel model, IHostService hostService, string includedFrom = null)
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
                BuildCore(i, model, hostService, includedFrom);
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

    private static string ParseFile(string link)
    {
        var queryIndex = link.IndexOfAny(new[] { '?', '#' });
        return queryIndex == -1 ? link : link.Remove(queryIndex);
    }

    private static void AddOrUpdate(Dictionary<string, ImmutableList<LinkSourceInfo>> dict, string path, LinkSourceInfo source)
        => dict[path] = dict.TryGetValue(path, out var sources) ? sources.Add(source) : ImmutableList.Create(source);

    private static LinkSourceInfo GetLinkSourceInfo(string path, string anchor, string source, string includedFrom)
    {
        return new LinkSourceInfo
        {
            SourceFile = includedFrom ?? source,
            Anchor = anchor,
            Target = path,
        };
    }
    #endregion
}
