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
            if ("toc.md".Equals(Path.GetFileName(file.File), StringComparison.OrdinalIgnoreCase))
            {
                toc = MarkdownTocReader.LoadToc(File.ReadAllText(Path.Combine(file.BaseDir, file.File)), file.File);
            }
            else if ("toc.yml".Equals(Path.GetFileName(file.File), StringComparison.OrdinalIgnoreCase))
            {
                toc = YamlUtility.Deserialize<TocViewModel>(Path.Combine(file.BaseDir, file.File));
            }
            if (toc == null)
            {
                throw new NotSupportedException();
            }

            // todo : metadata.
            return new FileModel(file, toc)
            {
                Uids = new[] { file.File }.ToImmutableArray(),
            };
        }

        public SaveResult Save(FileModel model)
        {
            var toc = (TocViewModel)model.Content;
            var path = (RelativePath)model.OriginalFileAndType.File;
            var tocMap = GetTocMap(null, toc, path);

            foreach (var item in toc)
            {
                UpdateRelativePath(item, (RelativePath)model.File);
            }

            YamlUtility.Serialize(Path.Combine(model.BaseDir, model.File), toc);
            return new SaveResult
            {
                DocumentType = "Toc",
                ModelFile = model.File,
                TocMap = tocMap.ToImmutableDictionary()
            };
        }

        private void UpdateRelativePath(TocItemViewModel item, RelativePath file)
        {
            if (PathUtility.IsRelativePath(item.Href)) item.Href = (RelativePath)"~/" + ((RelativePath)item.Href).BasedOn(file);
            if (item.Items != null && item.Items.Count > 0)
            {
                foreach (var i in item.Items)
                {
                    UpdateRelativePath(i, file);
                }
            }
        }

        private Dictionary<string, HashSet<string>> GetTocMap(Dictionary<string, HashSet<string>> tocMap, IList<TocItemViewModel> toc, RelativePath modelPath)
        {
            if (tocMap == null) tocMap = new Dictionary<string, HashSet<string>>(FilePathComparer.OSPlatformSensitiveComparer);
            foreach(var item in toc)
            {
                if (PathUtility.IsRelativePath(item.Href))
                {
                    var path = (RelativePath)"~/" + ((RelativePath)item.Href).BasedOn(modelPath);
                    var tocPath = modelPath;
                    HashSet<string> value;
                    if (tocMap.TryGetValue(path, out value))
                    {
                        value.Add(tocPath);
                    }
                    else
                    {
                        tocMap[path] = new HashSet<string>(FilePathComparer.OSPlatformSensitiveComparer) { tocPath };
                    }
                }
                if (item.Items != null && item.Items.Count > 0)
                {
                    GetTocMap(tocMap, item.Items, modelPath);
                }
            }
            if (tocMap.Count == 0) return null;
            return tocMap;
        }

        public IEnumerable<FileModel> Prebuild(ImmutableArray<FileModel> models, IHostService host)
        {
            return models;
        }

        public void Build(FileModel model, IHostService host)
        {
            model.File = Path.ChangeExtension(model.File, ".yml");
            // todo : metadata.
        }

        public IEnumerable<FileModel> Postbuild(ImmutableArray<FileModel> models, IHostService host)
        {
            return models;
        }
    }
}
