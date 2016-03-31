// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.TableOfContents
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Composition;
    using System.Linq;

    using Microsoft.DocAsCode.Build.Common;
    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Utility;

    [Export(nameof(TocDocumentProcessor), typeof(IDocumentBuildStep))]
    public class BuildTocDocument : BaseDocumentBuildStep
    {
        public override string Name => nameof(BuildTocDocument);

        public override int BuildOrder => 0;

        /// <summary>
        /// 1. Expand the TOC reference
        /// 2. Resolve homepage
        /// </summary>
        /// <param name="models"></param>
        /// <param name="host"></param>
        /// <returns></returns>
        public override IEnumerable<FileModel> Prebuild(ImmutableList<FileModel> models, IHostService host)
        {
            var tocModelCache = new Dictionary<FileAndType, TocItemInfo>();
            foreach (var model in models)
            {
                if (!tocModelCache.ContainsKey(model.OriginalFileAndType))
                {
                    tocModelCache[model.OriginalFileAndType] = new TocItemInfo(model.OriginalFileAndType, (TocItemViewModel)model.Content);
                }
            }
            var tocResolver = new TocResolver(host, tocModelCache);
            foreach (var key in tocModelCache.Keys.ToList())
            {
                tocModelCache[key] = tocResolver.Resolve(key);
            }

            foreach (var model in models)
            {
                var wrapper = tocModelCache[model.OriginalFileAndType];

                // If the TOC file is referenced by other TOC, remove it from the collection
                if (!wrapper.IsReferenceToc)
                {
                    model.Content = wrapper.Content;
                    yield return model;
                }
            }
        }

        public override void Build(FileModel model, IHostService host)
        {
            var toc = (TocItemViewModel)model.Content;
            BuildCore(toc, model, host);

            // todo : metadata.
        }

        private void BuildCore(TocItemViewModel item, FileModel model, IHostService hostService)
        {
            if (item == null)
            {
                return;
            }

            var linkToUids = new HashSet<string>();
            var linkToFiles = new HashSet<string>();
            if (PathUtility.IsRelativePath(item.Href))
            {
                linkToFiles.Add(item.Href);
            }

            if (PathUtility.IsRelativePath(item.Homepage))
            {
                linkToFiles.Add(item.Homepage);
            }

            if (!string.IsNullOrEmpty(item.Uid))
            {
                linkToUids.Add(item.Uid);
            }

            if (!string.IsNullOrEmpty(item.HomepageUid))
            {
                linkToUids.Add(item.HomepageUid);
            }

            ((HashSet<string>)model.Properties.LinkToUids).UnionWith(linkToUids);
            ((HashSet<string>)model.Properties.LinkToFiles).UnionWith(linkToFiles);

            if (item.Items != null)
            {
                foreach (var i in item.Items)
                {
                    BuildCore(i, model, hostService);
                }
            }
        }
    }
}
