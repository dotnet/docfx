// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Common
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;

    public abstract class SplitModelBaseDocumentBuildStep : BaseDocumentBuildStep
    {
        public override IEnumerable<FileModel> Prebuild(ImmutableList<FileModel> models, IHostService host)
        {
            var collection = new List<FileModel>(models);
            var treeMapping = new Dictionary<string, SplittedInfo>();
            foreach (var model in models)
            {
                var result = SplitModelCore(model);
                if (result != null)
                {
                    if (treeMapping.ContainsKey(result.Key))
                    {
                        Logger.LogWarning($"Model with the key {result.Key} already exists. '{model.OriginalFileAndType?.FullPath ?? model.FileAndType.FullPath}' is ignored.");
                    }
                    else
                    {
                        treeMapping.Add(result.Key, result);
                        collection.AddRange(result.Models);
                    }
                }
            }

            var tocRestructions = treeMapping.Select(kv => GenerateTreeItemRestructure(kv.Value));
            host.TableOfContentRestructions = host.TableOfContentRestructions == null ?
                tocRestructions.ToImmutableList() :
                host.TableOfContentRestructions.Concat(tocRestructions).ToImmutableList();

            return collection;
        }

        protected abstract SplittedInfo SplitModelCore(FileModel model);

        protected abstract TreeItemRestructure GenerateTreeItemRestructure(SplittedInfo splittedInfo);

        protected class SplittedInfo
        {
            public string Key { get; }

            public FileAndType OriginalFileAndType { get; }

            public IEnumerable<TreeItem> TreeItems { get; }

            public IEnumerable<FileModel> Models { get; }

            public SplittedInfo(string key, FileAndType originalFileAndType, IEnumerable<TreeItem> treeItems, IEnumerable<FileModel> models)
            {
                Key = key;
                OriginalFileAndType = originalFileAndType;
                TreeItems = treeItems;
                Models = models;
            }
        }
    }
}
