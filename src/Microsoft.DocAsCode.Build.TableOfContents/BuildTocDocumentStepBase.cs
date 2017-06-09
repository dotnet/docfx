// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.TableOfContents
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;

    using Microsoft.DocAsCode.Build.Common;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.Plugins;

    public abstract class BuildTocDocumentStepBase : BaseDocumentBuildStep
    {
        #region IDocumentBuildStep

        /// <summary>
        /// 1. Expand the TOC reference
        /// 2. Resolve homepage
        /// </summary>
        /// <param name="models"></param>
        /// <param name="host"></param>
        /// <returns></returns>
        public override IEnumerable<FileModel> Prebuild(ImmutableList<FileModel> models, IHostService host)
        {
            var tocCache = new Dictionary<string, TocItemInfo>(FilePathComparer.OSPlatformSensitiveStringComparer);
            foreach (var model in models)
            {
                if (!tocCache.ContainsKey(model.OriginalFileAndType.FullPath))
                {
                    tocCache[model.OriginalFileAndType.FullPath] = new TocItemInfo(model.OriginalFileAndType, (TocItemViewModel)model.Content);
                }
            }
            var tocResolver = new TocResolver(host, tocCache);
            foreach (var key in tocCache.Keys.ToList())
            {
                tocCache[key] = tocResolver.Resolve(key);
            }

            return PreBuildSelectModels(models, host, tocCache.ToImmutableDictionary());
        }

        public override void Build(FileModel model, IHostService host)
        {
            var toc = (TocItemViewModel)model.Content;
            TocRestructureUtility.Restructure(toc, host.TableOfContentRestructions);
            BuildCore(toc, model, host);
            ReportDependency(model, host);
            model.Content = toc;
            // todo : metadata.
        }

        #endregion

        #region Abstract methods

        public abstract IEnumerable<FileModel> PreBuildSelectModels(ImmutableList<FileModel> models, IHostService host, ImmutableDictionary<string, TocItemInfo> tocCache);

        public abstract void ReportDependency(FileModel model, IHostService host);

        #endregion

        #region Private methods

        private void BuildCore(TocItemViewModel item, FileModel model, IHostService hostService)
        {
            if (item == null)
            {
                return;
            }

            var linkToUids = new HashSet<string>();
            var linkToFiles = new HashSet<string>();
            if (Utility.IsSupportedRelativeHref(item.Href))
            {
                linkToFiles.Add(ParseFile(item.Href));
            }

            if (Utility.IsSupportedRelativeHref(item.Homepage))
            {
                linkToFiles.Add(ParseFile(item.Homepage));
            }

            if (!string.IsNullOrEmpty(item.TopicUid))
            {
                linkToUids.Add(item.TopicUid);
            }

            model.LinkToUids = model.LinkToUids.Union(linkToUids);
            model.LinkToFiles = model.LinkToFiles.Union(linkToFiles);

            if (item.Items != null)
            {
                foreach (var i in item.Items)
                {
                    BuildCore(i, model, hostService);
                }
            }
        }

        private static string ParseFile(string link)
        {
            var queryIndex = link.IndexOfAny(new[] { '?', '#' });
            return queryIndex == -1 ? link : link.Remove(queryIndex);
        }

        #endregion
    }
}
