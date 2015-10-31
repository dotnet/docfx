// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.Plugins
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Composition;
    using System.IO;

    using Microsoft.DocAsCode.EntityModel.ViewModels;
    using Microsoft.DocAsCode.Plugins;
    using Utility;

    [Export(typeof(IDocumentProcessor))]
    public class TocDocumentProcessor : IDocumentProcessor
    {
        public string Name => nameof(TocDocumentProcessor);

        public ProcessingPriority GetProcessingPriority(FileAndType file)
        {
            if (file.Type == DocumentType.Article)
            {
                if ("toc.md".Equals(Path.GetFileName(file.File), StringComparison.OrdinalIgnoreCase))
                {
                    return ProcessingPriority.High;
                }
                if ("toc.yml".Equals(Path.GetFileName(file.File), StringComparison.OrdinalIgnoreCase))
                {
                    return ProcessingPriority.High;
                }
            }
            return ProcessingPriority.NotSupportted;
        }

        public FileModel Load(FileAndType file, ImmutableDictionary<string, object> metadata)
        {
            var filePath = Path.Combine(file.BaseDir, file.File);
            TocViewModel toc = LoadSingleToc(filePath);

            var repoDetail = GitUtility.GetGitDetail(filePath);

            // todo : metadata.
            return new FileModel(file, toc)
            {
                Uids = new[] { file.File }.ToImmutableArray(),
                LocalPathFromRepoRoot = repoDetail?.RelativePath
            };
        }

        public SaveResult Save(FileModel model)
        {
            var toc = (TocViewModel)model.Content;
            var path = (RelativePath)model.OriginalFileAndType.File;

            YamlUtility.Serialize(Path.Combine(model.BaseDir, model.File), toc);
            return new SaveResult
            {
                DocumentType = "Toc",
                ModelFile = model.File,
                TocMap = model.Properties.TocMap,
                LinkToFiles = model.Properties.LinkToFiles
            };
        }

        public IEnumerable<FileModel> Prebuild(ImmutableList<FileModel> models, IHostService host)
        {
            return models;
        }

        public void Build(FileModel model, IHostService host)
        {
            model.File = Path.ChangeExtension(model.File, ".yml");
            var toc = (TocViewModel)model.Content;
            HashSet<string> links = new HashSet<string>();
            Dictionary<string, HashSet<string>> tocMap = new Dictionary<string, HashSet<string>>();
            UpdateRelativePathAndAddTocMap(toc, model, links, tocMap, host);
            model.Properties.LinkToFiles = links.ToImmutableArray();
            model.Properties.TocMap = tocMap.ToImmutableDictionary();
            // todo : metadata.
        }

        public IEnumerable<FileModel> Postbuild(ImmutableList<FileModel> models, IHostService host)
        {
            return models;
        }

        private void UpdateRelativePathAndAddTocMap(TocViewModel toc, FileModel model, HashSet<string> links, Dictionary<string, HashSet<string>> tocMap, IHostService hostService)
        {
            if (toc == null) return;
            var file = model.File;
            var originalFile = model.OriginalFileAndType.File;
            foreach (var item in toc)
            {
                if (PathUtility.IsRelativePath(item.Href))
                {
                    // Special handle for folder ends with '/'
                    FileAndType originalTocFile = null;

                    string fileName = Path.GetFileName(item.Href);
                    if (string.IsNullOrEmpty(fileName))
                    {
                        var href = item.Href + "toc.yml";
                        var absHref = ((RelativePath)href).BasedOn((RelativePath)file);
                        var tocPath = (RelativePath)"~/" + absHref;
                        if (!hostService.SourceFiles.TryGetValue(tocPath, out originalTocFile))
                        {
                            href = item.Href + "toc.md";
                            absHref = ((RelativePath)href).BasedOn((RelativePath)file);
                            tocPath = (RelativePath)"~/" + absHref;
                            if (!hostService.SourceFiles.TryGetValue(tocPath, out originalTocFile))
                            {
                                var error = $"Unable to find either toc.yml or toc.md inside {item.Href}";
                                Logger.LogError(error, file: model.LocalPathFromRepoRoot);
                                throw new DocumentException(error);
                            }
                        }

                        Logger.LogInfo($"TOC file {href} inside {item.Href} is used", file: model.LocalPathFromRepoRoot);
                        item.Href = href;
                    }

                    // Add toc.yml to tocMap before change item.Href to home page
                    item.Href = (RelativePath)"~/" + ((RelativePath)item.Href).BasedOn((RelativePath)file);
                    HashSet<string> value;
                    if (tocMap.TryGetValue(item.Href, out value))
                    {
                        value.Add(originalFile);
                    }
                    else
                    {
                        tocMap[item.Href] = new HashSet<string>(FilePathComparer.OSPlatformSensitiveComparer) { originalFile };
                    }
                    links.Add(item.Href);

                    SetHomepage(item, originalTocFile, model);
                }

                UpdateRelativePathAndAddTocMap(item.Items, model, links, tocMap, hostService);
            }
        }

        private void SetHomepage(TocItemViewModel item, FileAndType originalTocFile, FileModel model)
        {
            if (!string.IsNullOrEmpty(item.Homepage))
            {
                if (PathUtility.IsRelativePath(item.Homepage))
                {
                    item.Href = (RelativePath)"~/" + ((RelativePath)item.Homepage).BasedOn((RelativePath)model.File);
                }
                else
                {
                    item.Href = item.Homepage;
                }

                Logger.LogInfo($"Homepage {item.Homepage} is used.", file: model.LocalPathFromRepoRoot);
            }
            else if (originalTocFile != null)
            {
                var subTocPath = Path.Combine(originalTocFile.BaseDir, originalTocFile.File);
                var subToc = LoadSingleToc(subTocPath);
                var href = GetDefaultHomepage(subToc);

                if (href == null)
                {
                    var error = $"Unable to get default page for {item.Href}: no item containing relative file link is defined inside TOC {subTocPath}";
                    Logger.LogError(error, file: model.LocalPathFromRepoRoot);
                    throw new DocumentException(error);
                }

                item.Href = (RelativePath)item.Href + (RelativePath)href;
            }
        }

        private string GetDefaultHomepage(TocViewModel toc)
        {
            foreach (var item in toc)
            {
                var href = TreeIterator.PreorderFirstOrDefault(item, s => s.Items, s => IsValidHomepageLink(s.Href));
                if (href != null)
                {
                    return href.Href;
                }
            }
            return null;
        }

        /// <summary>
        /// Valid homepage href should:
        /// 1. relative file path
        /// 2. refer to a file
        /// 3. folder is not supported
        /// </summary>
        /// <param name="href"></param>
        /// <returns></returns>
        private bool IsValidHomepageLink(string href) {
            return PathUtility.IsRelativePath(href) && !string.IsNullOrEmpty(Path.GetFileName(href));
        }

        private TocViewModel LoadSingleToc(string filePath)
        {
            if ("toc.md".Equals(Path.GetFileName(filePath), StringComparison.OrdinalIgnoreCase))
            {
                return MarkdownTocReader.LoadToc(File.ReadAllText(filePath), filePath);
            }
            else if ("toc.yml".Equals(Path.GetFileName(filePath), StringComparison.OrdinalIgnoreCase))
            {
                return YamlUtility.Deserialize<TocViewModel>(filePath);
            }

            throw new NotSupportedException($"{filePath} is not a valid TOC file, supported toc files could be \"toc.md\" or \"toc.yml\".");
        }
    }
}
