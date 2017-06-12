// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.TableOfContents
{
    using System.Collections.Generic;

    using Microsoft.DocAsCode.Build.Common;
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
            model.Content = toc;
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
