﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.Plugins
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Composition;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Utility;

    [Export(typeof(IDocumentProcessor))]
    public class ResourceDocumentProcessor : DisposableDocumentProcessor
    {
        [ImportMany]
        public IEnumerable<IResourceFileConfig> Configs { get; set; }

        public override string Name => nameof(ResourceDocumentProcessor);

        [ImportMany(nameof(ResourceDocumentProcessor))]
        public override IEnumerable<IDocumentBuildStep> BuildSteps { get; set; }

        public override ProcessingPriority GetProcessingPriority(FileAndType file)
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

        public override FileModel Load(FileAndType file, ImmutableDictionary<string, object> metadata)
        {
            string uid = null;
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
                        if (item.Key == "uid")
                        {
                            uid = item.Value as string;
                        }
                    }
                }
            }
            if (content == null)
            {
                content = metadata.ToDictionary(p => p.Key, p => p.Value);
            }

            var filePath = Path.Combine(file.BaseDir, file.File);
            var repoDetail = GitUtility.GetGitDetail(filePath);

            return new FileModel(file, content)
            {
                Uids = string.IsNullOrEmpty(uid) ? ImmutableArray<string>.Empty : ImmutableArray<string>.Empty.Add(uid),
                LocalPathFromRepoRoot = repoDetail?.RelativePath
            };
        }

        public override SaveResult Save(FileModel model)
        {
            var result = new SaveResult
            {
                DocumentType = "Resource",
                ResourceFile = model.File,
            };
            if (model.Content != null)
            {
                var modelFile = model.File.TrimEnd('.') + ".json";
                result.ModelFile = modelFile;
            }
            return result;
        }
    }
}
