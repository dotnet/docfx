// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.Plugins
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Composition;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode.Plugins;

    [Export(typeof(IDocumentProcessor))]
    public class ResourceDocumentProcessor : IDocumentProcessor
    {
        [ImportMany]
        public IEnumerable<IResourceFileConfig> Configs { get; set; }

        public ProcessingPriority GetProcessingPriority(FileAndType file)
        {
            if (file.Type == DocumentType.Resource)
            {
                return ProcessingPriority.Normal;
            }
            if (file.Type == DocumentType.Article)
            {
                foreach (var config in Configs)
                {
                    if (config.IsResourceFile(Path.GetExtension(file.File)))
                    {
                        return ProcessingPriority.Lowest;
                    }
                }
            }
            return ProcessingPriority.NotSupportted;
        }

        public FileModel Load(FileAndType file, ImmutableDictionary<string, object> metadata)
        {
            Dictionary<string, object> content = null;
            var metafile = Path.Combine(file.BaseDir, file.File.TrimEnd('.') + ".meta");
            if (File.Exists(metafile))
            {
                content = YamlUtility.Deserialize<Dictionary<string, object>>(metafile);
                if (content != null)
                {
                    foreach (var item in metadata)
                    {
                        if (!content.ContainsKey(item.Key))
                        {
                            content[item.Key] = item.Value;
                        }
                    }
                }
            }
            if (content == null)
            {
                content = metadata.ToDictionary(p => p.Key, p => p.Value);
            }
            return new FileModel(file, content);
        }

        public SaveResult Save(FileModel model)
        {
            if (model.FileAndType != model.OriginalFileAndType)
            {
                var targetFile = Path.Combine(model.BaseDir, model.File);
                Directory.CreateDirectory(Path.GetDirectoryName(targetFile));
                File.Copy(
                    Path.Combine(model.OriginalFileAndType.BaseDir, model.OriginalFileAndType.File),
                    targetFile,
                    true);
            }
            var result = new SaveResult
            {
                DocumentType = "Resource",
                ResourceFile = model.File,
            };
            if (model.Content != null)
            {
                var modelFile = model.File.TrimEnd('.') + ".yml";
                YamlUtility.Serialize(Path.Combine(model.BaseDir, modelFile), model.Content);
                result.ModelFile = modelFile;
            }
            return result;
        }

        public IEnumerable<FileModel> Prebuild(ImmutableArray<FileModel> models, IHostService host)
        {
            return models;
        }

        public void Build(FileModel model, IHostService host)
        {
            if (model.Type != DocumentType.Article && model.Type != DocumentType.Resource)
            {
                throw new NotSupportedException();
            }
            // todo : metadata.
        }

        public IEnumerable<FileModel> Postbuild(ImmutableArray<FileModel> models, IHostService host)
        {
            return models;
        }
    }
}
