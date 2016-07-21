// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.ResourceFiles
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Composition;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode.Build.Common;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.DataContracts.Common;
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
            return ProcessingPriority.NotSupported;
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
                        if (item.Key == Constants.PropertyName.Uid)
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
            string displayLocalPath = null;

            object baseDirectory;
            if (metadata.TryGetValue("baseDirectory", out baseDirectory))
            {
                displayLocalPath = PathUtility.MakeRelativePath((string)baseDirectory, file.FullPath);
            }

            return new FileModel(file, content)
            {
                Uids = string.IsNullOrEmpty(uid) ? ImmutableArray<UidDefinition>.Empty : ImmutableArray<UidDefinition>.Empty.Add(new UidDefinition(uid, displayLocalPath)),
                LocalPathFromRepoRoot = repoDetail?.RelativePath ?? Path.Combine(file.BaseDir, file.File).ToDisplayPath(),
                LocalPathFromRoot = displayLocalPath
            };
        }

        public override SaveResult Save(FileModel model)
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
                // For resources, e.g. image.png, file extension is kept
                result.FileWithoutExtension = model.File;
            }

            return result;
        }
    }
}
