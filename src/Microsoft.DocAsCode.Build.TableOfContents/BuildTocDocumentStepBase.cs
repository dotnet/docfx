// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.TableOfContents
{
    using System.Collections.Generic;
    using System.Collections.Immutable;

    using Microsoft.DocAsCode.Build.Common;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.Plugins;

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

        #region Virtual methods

        public virtual void ReportUidDependency(FileModel model, IHostService host, TocItemViewModel item)
        {
            if (item.TopicUid != null)
            {
                host.ReportDependencyFrom(model, item.TopicUid, DependencyItemSourceType.Uid, DependencyTypeName.Metadata);
            }
            if (item.Items != null && item.Items.Count > 0)
            {
                foreach (var i in item.Items)
                {
                    ReportUidDependency(model, host, i);
                }
            }
        }

        #endregion

        #region Private methods

        private void BuildCore(TocItemViewModel item, FileModel model, IHostService hostService, string includedFrom = null)
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
                var path = UriUtility.GetPath(link);
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
}
