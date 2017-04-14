// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.TableOfContents
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Composition;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.DocAsCode.Build.Common;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.Plugins;

    [Export(nameof(TocDocumentProcessor), typeof(IDocumentBuildStep))]
    public class BuildTocDocument : BaseDocumentBuildStep, ISupportIncrementalBuildStep
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
            var tocModelCache = new Dictionary<string, TocItemInfo>(FilePathComparer.OSPlatformSensitiveStringComparer);
            foreach (var model in models)
            {
                if (!tocModelCache.ContainsKey(model.OriginalFileAndType.FullPath))
                {
                    tocModelCache[model.OriginalFileAndType.FullPath] = new TocItemInfo(model.OriginalFileAndType, (TocItemViewModel)model.Content);
                }
            }
            var tocResolver = new TocResolver(host, tocModelCache);
            foreach (var key in tocModelCache.Keys.ToList())
            {
                tocModelCache[key] = tocResolver.Resolve(key);
            }

            ReportDependency(models, host, tocModelCache.ToImmutableDictionary(), 8);

            foreach (var model in models)
            {
                var wrapper = tocModelCache[model.OriginalFileAndType.FullPath];

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
            Restructure(toc, host.TableOfContentRestructions);
            BuildCore(toc, model, host);
            ReportUidDependency(host, toc, model);
            model.Content = toc;
            // todo : metadata.
        }

        internal void Restructure(TocItemViewModel toc, IList<TreeItemRestructure> restructures)
        {
            if (restructures == null || restructures.Count == 0)
            {
                return;
            }
            RestructureCore(toc, new List<TocItemViewModel>(), restructures);
        }

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

        private void RestructureCore(TocItemViewModel item, List<TocItemViewModel> items, IList<TreeItemRestructure> restructures)
        {
            if (item.Items != null && item.Items.Count > 0)
            {
                var parentItems = new List<TocItemViewModel>(item.Items);
                foreach (var i in item.Items)
                {
                    RestructureCore(i, parentItems, restructures);
                }

                item.Items = new TocViewModel(parentItems);
            }

            foreach (var restruction in restructures)
            {
                if (Matches(item, restruction))
                {
                    RestructureItem(item, items, restruction);
                }
            }
        }

        private void RestructureItem(TocItemViewModel item, List<TocItemViewModel> items, TreeItemRestructure restruction)
        {
            var index = items.IndexOf(item);
            if (index < 0)
            {
                Logger.LogWarning($"Unable to find {restruction.Key}, it is probably removed or replaced by other restructions.");
                return;
            }

            switch (restruction.ActionType)
            {
                case TreeItemActionType.ReplaceSelf:
                    {
                        if (restruction.RestructuredItems == null || restruction.RestructuredItems.Count == 0)
                        {
                            return;
                        }
                        if (restruction.RestructuredItems.Count > 1)
                        {
                            throw new InvalidOperationException($"{restruction.ActionType} does not allow multiple root nodes.");
                        }

                        var roots = GetRoots(restruction.RestructuredItems);
                        items[index] = roots[0];
                        break;
                    }
                case TreeItemActionType.DeleteSelf:
                    {
                        items.RemoveAt(index);
                        break;
                    }
                case TreeItemActionType.AppendChild:
                    {
                        if (restruction.RestructuredItems == null || restruction.RestructuredItems.Count == 0)
                        {
                            return;
                        }
                        if (item.Items == null)
                        {
                            item.Items = new TocViewModel();
                        }

                        var roots = GetRoots(restruction.RestructuredItems);
                        item.Items.AddRange(roots);
                        break;
                    }
                case TreeItemActionType.PrependChild:
                    {
                        if (restruction.RestructuredItems == null || restruction.RestructuredItems.Count == 0)
                        {
                            return;
                        }
                        if (item.Items == null)
                        {
                            item.Items = new TocViewModel();
                        }

                        var roots = GetRoots(restruction.RestructuredItems);
                        item.Items.InsertRange(0, roots);
                        break;
                    }
                case TreeItemActionType.InsertAfter:
                    {
                        if (restruction.RestructuredItems == null || restruction.RestructuredItems.Count == 0)
                        {
                            return;
                        }
                        var roots = GetRoots(restruction.RestructuredItems);
                        items.InsertRange(index + 1, roots);
                        break;
                    }
                case TreeItemActionType.InsertBefore:
                    {
                        if (restruction.RestructuredItems == null || restruction.RestructuredItems.Count == 0)
                        {
                            return;
                        }
                        var roots = GetRoots(restruction.RestructuredItems);
                        items.InsertRange(index, roots);
                        break;
                    }
                default:
                    break;
            }
        }

        private void ReportDependency(ImmutableList<FileModel> models, IHostService host, ImmutableDictionary<string, TocItemInfo> tocModelCache, int parallelism)
        {
            var nearest = new ConcurrentDictionary<string, Toc>(FilePathComparer.OSPlatformSensitiveStringComparer);
            models.RunAll(model =>
            {
                var wrapper = tocModelCache[model.OriginalFileAndType.FullPath];

                // If the TOC file is referenced by other TOC, not report dependency
                if (wrapper.IsReferenceToc)
                {
                    return;
                }
                var item = wrapper.Content;
                UpdateNearestToc(host, item, model, nearest);
            },
            parallelism);

            // handle not-in-toc items
            UpdateNearestTocForNotInTocItem(models, host, nearest, parallelism);

            foreach (var item in nearest)
            {
                host.ReportDependencyFrom(item.Value.Model, item.Key, DependencyTypeName.Metadata);
            }
        }

        private void ReportUidDependency(IHostService host, TocItemViewModel item, FileModel toc)
        {
            if (item.TopicUid != null)
            {
                host.ReportDependencyFrom(toc, item.TopicUid, DependencyItemSourceType.Uid, DependencyTypeName.Metadata);
            }
            if (item.Items != null && item.Items.Count > 0)
            {
                foreach (var i in item.Items)
                {
                    ReportUidDependency(host, i, toc);
                }
            }
        }

        private void UpdateNearestToc(IHostService host, TocItemViewModel item, FileModel toc, ConcurrentDictionary<string, Toc> nearest)
        {
            var tocHref = item.TocHref;
            var type = Utility.GetHrefType(tocHref);
            if (type == HrefType.MarkdownTocFile || type == HrefType.YamlTocFile)
            {
                UpdateNearestTocCore(host, tocHref, toc, nearest);
            }
            else if (item.TopicUid == null && Utility.IsSupportedRelativeHref(item.Href))
            {
                UpdateNearestTocCore(host, item.Href, toc, nearest);
            }

            if (item.Items != null && item.Items.Count > 0)
            {
                foreach (var i in item.Items)
                {
                    UpdateNearestToc(host, i, toc, nearest);
                }
            }
        }

        private void UpdateNearestTocForNotInTocItem(ImmutableList<FileModel> models, IHostService host, ConcurrentDictionary<string, Toc> nearest, int parallelism)
        {
            var allSourceFiles = host.SourceFiles;
            Parallel.ForEach(
                allSourceFiles.Keys.Except(nearest.Keys, FilePathComparer.OSPlatformSensitiveStringComparer).ToList(),
                new ParallelOptions { MaxDegreeOfParallelism = parallelism },
                item =>
                {
                    var itemOutputFile = GetOutputPath(allSourceFiles[item]);
                    var near = (from m in models
                                let outputFile = GetOutputPath(m.FileAndType)
                                let rel = outputFile.MakeRelativeTo(itemOutputFile)
                                where rel.SubdirectoryCount == 0
                                orderby rel.ParentDirectoryCount
                                select new Toc
                                {
                                    Model = m,
                                    OutputPath = rel,
                                }).FirstOrDefault();
                    if (near != null)
                    {
                        nearest[item] = near;
                    }
                });
        }

        private void UpdateNearestTocCore(IHostService host, string item, FileModel toc, ConcurrentDictionary<string, Toc> nearest)
        {
            var allSourceFiles = host.SourceFiles;
            var tocOutputFile = GetOutputPath(toc.FileAndType);
            FileAndType itemSource;
            if (allSourceFiles.TryGetValue(item, out itemSource))
            {
                var itemOutputFile = GetOutputPath(itemSource);
                var relative = tocOutputFile.RemoveWorkingFolder() - itemOutputFile;
                nearest.AddOrUpdate(
                    item,
                    k => new Toc { Model = toc, OutputPath = relative },
                    (k, v) =>
                    {
                        if (CompareRelativePath(relative, v.OutputPath) < 0)
                        {
                            return new Toc { Model = toc, OutputPath = relative };
                        }
                        return v;
                    });
            }
        }

        private static RelativePath GetOutputPath(FileAndType file)
        {
            if (file.SourceDir != file.DestinationDir)
            {
                return (RelativePath)file.DestinationDir + (((RelativePath)file.File) - (RelativePath)file.SourceDir);
            }
            else
            {
                return (RelativePath)file.File;
            }
        }

        private static int CompareRelativePath(RelativePath a, RelativePath b)
        {
            int res = a.SubdirectoryCount - b.SubdirectoryCount;
            if (res != 0)
            {
                return res;
            }
            return a.ParentDirectoryCount - b.ParentDirectoryCount;
        }

        private bool Matches(TocItemViewModel item, TreeItemRestructure restruction)
        {
            switch (restruction.TypeOfKey)
            {
                case TreeItemKeyType.TopicUid:
                    // make sure TocHref is null so that TopicUid is not the resolved homepage in `href: api/` case
                    return item.TocHref == null && item.TopicUid == restruction.Key;
                case TreeItemKeyType.TopicHref:
                    return item.TocHref == null && FilePathComparer.OSPlatformSensitiveStringComparer.Compare(item.TopicHref, restruction.Key) == 0;
                default:
                    throw new NotSupportedException($"{restruction.TypeOfKey} is not a supported ComparerKeyType");
            }
        }

        private static List<TocItemViewModel> GetRoots(IEnumerable<TreeItem> treeItems)
        {
            return JsonUtility.FromJsonString<List<TocItemViewModel>>(JsonUtility.ToJsonString(treeItems));
        }

        private static string ParseFile(string link)
        {
            var queryIndex = link.IndexOfAny(new[] { '?', '#' });
            return queryIndex == -1 ? link : link.Remove(queryIndex);
        }

        private class Toc
        {
            public FileModel Model { get; set; }

            public RelativePath OutputPath { get; set; }
        }

        #region ISupportIncrementalBuildStep Members

        public bool CanIncrementalBuild(FileAndType fileAndType) => true;

        public string GetIncrementalContextHash() => null;

        public IEnumerable<DependencyType> GetDependencyTypesToRegister() => null;

        #endregion
    }
}
