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
            TocViewModel toc = null;
            var filePath = Path.Combine(file.BaseDir, file.File);
            if ("toc.md".Equals(Path.GetFileName(file.File), StringComparison.OrdinalIgnoreCase))
            {
                toc = MarkdownTocReader.LoadToc(File.ReadAllText(filePath), file.File);
            }
            else if ("toc.yml".Equals(Path.GetFileName(file.File), StringComparison.OrdinalIgnoreCase))
            {
                toc = YamlUtility.Deserialize<TocViewModel>(filePath);
            }
            if (toc == null)
            {
                throw new NotSupportedException();
            }

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

        private void UpdateRelativePathAndAddTocMap(TocViewModel toc, FileModel model, HashSet<string> links, Dictionary<string, HashSet<string>> tocMap, IHostService hostService)
        {
            if (toc == null) return;
            var file = model.File;
            var originalFile = model.OriginalFileAndType.File;
            foreach(var item in toc)
            {
                if (PathUtility.IsRelativePath(item.Href))
                {
                    // Special handle for folder ends with '/'
                    string fileName = Path.GetFileName(item.Href);
                    if (string.IsNullOrEmpty(fileName))
                    {
                        var href = item.Href + "toc.yml";
                        var tocPath = (RelativePath)"~/" + (RelativePath)href;
                        if (!hostService.SourceFiles.Contains(tocPath))
                        {
                            href = item.Href + "toc.md";
                            tocPath = (RelativePath)"~/" + (RelativePath)href;
                            if (!hostService.SourceFiles.Contains(tocPath))
                            {
                                Logger.LogError($"Unable to find either toc.yml or toc.md inside {item.Href}");
                                href = item.Href + "index.md"; // TODO: what if index.html exists?
                            }
                        }

                        item.Href = href;
                    }

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
                }

                UpdateRelativePathAndAddTocMap(item.Items, model, links, tocMap, hostService);
            }
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
    }
}
