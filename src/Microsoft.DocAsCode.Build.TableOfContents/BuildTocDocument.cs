// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.TableOfContents
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Composition;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.Plugins;

    [Export(nameof(TocDocumentProcessor), typeof(IDocumentBuildStep))]
    public class BuildTocDocument : BuildTocDocumentStepBase, ISupportIncrementalBuildStep
    {
        #region Override methods

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
            var resolvedModels = TocHelper.Resolve(models, host).ToList();

            ReportPreBuildDependency(resolvedModels, host, 8);

            return resolvedModels;
        }

        public override void Build(FileModel model, IHostService host)
        {
            base.Build(model, host);
            var item = (TocItemViewModel)model.Content;
            ReportUidDependency(model, host, item);
        }

        #endregion

        #region Private methods

        private void ReportPreBuildDependency(List<FileModel> models, IHostService host, int parallelism)
        {
            var nearest = new ConcurrentDictionary<string, Toc>(FilePathComparer.OSPlatformSensitiveStringComparer);
            models.RunAll(model =>
            {
                var item = (TocItemViewModel)model.Content;
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

        private void UpdateNearestToc(IHostService host, TocItemViewModel item, FileModel toc, ConcurrentDictionary<string, Toc> nearest)
        {
            var tocHref = item.TocHref;
            var type = Utility.GetHrefType(tocHref);
            if (type == HrefType.MarkdownTocFile || type == HrefType.YamlTocFile)
            {
                UpdateNearestTocCore(host, UriUtility.GetPath(tocHref), toc, nearest);
            }
            else if (item.TopicUid == null && Utility.IsSupportedRelativeHref(item.Href))
            {
                UpdateNearestTocCore(host, UriUtility.GetPath(item.Href), toc, nearest);
            }

            if (item.Items != null && item.Items.Count > 0)
            {
                foreach (var i in item.Items)
                {
                    UpdateNearestToc(host, i, toc, nearest);
                }
            }
        }

        private void UpdateNearestTocForNotInTocItem(List<FileModel> models, IHostService host, ConcurrentDictionary<string, Toc> nearest, int parallelism)
        {
            var allSourceFiles = host.SourceFiles;
            var tocs = (from m in models
                        select new Toc
                        {
                            Model = m,
                            OutputPath = GetOutputPath(m.FileAndType),
                        }).ToArray();
            Parallel.ForEach(
                allSourceFiles.Keys.Except(nearest.Keys, FilePathComparer.OSPlatformSensitiveStringComparer).ToList(),
                new ParallelOptions { MaxDegreeOfParallelism = parallelism },
                item =>
                {
                    var itemOutputFile = GetOutputPath(allSourceFiles[item]);
                    var near = (from toc in tocs
                                let rel = toc.OutputPath.MakeRelativeTo(itemOutputFile)
                                where rel.SubdirectoryCount == 0
                                orderby rel.ParentDirectoryCount
                                select toc).FirstOrDefault();
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
            if (allSourceFiles.TryGetValue(item, out FileAndType itemSource))
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

        private class Toc
        {
            public FileModel Model { get; set; }

            public RelativePath OutputPath { get; set; }
        }

        #endregion

        #region ISupportIncrementalBuildStep Members

        public bool CanIncrementalBuild(FileAndType fileAndType) => true;

        public string GetIncrementalContextHash() => null;

        public IEnumerable<DependencyType> GetDependencyTypesToRegister() => null;

        #endregion
    }
}
