// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.RestApi
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Composition;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode.Build.Common;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.DataContracts.RestApi;
    using Microsoft.DocAsCode.Plugins;

    [Export("RestApiDocumentProcessor", typeof(IDocumentBuildStep))]
    public class SplitRestApiToTagsLevel : BaseDocumentBuildStep
    {
        public override string Name => nameof(SplitRestApiToTagsLevel);

        public override int BuildOrder => 1;

        public override IEnumerable<FileModel> Prebuild(ImmutableList<FileModel> models, IHostService host)
        {
            var collection = new List<FileModel>(models);

            var treeMapping = new Dictionary<string, Tuple<FileAndType, IEnumerable<TreeItem>>>();
            foreach (var model in models)
            {
                var result = SplitModelToOperationGroup(model);
                if (result != null)
                {
                    if (treeMapping.ContainsKey(result.Key))
                    {
                        Logger.LogWarning($"Model with the key {result.Key} already exists. '{model.OriginalFileAndType?.FullPath ?? model.FileAndType.FullPath}' is ignored.");
                    }
                    else
                    {
                        treeMapping.Add(result.Key, Tuple.Create(model.OriginalFileAndType, result.TreeItems));
                        collection.AddRange(result.Models);
                    }
                }
            }

            host.TableOfContentRestructions =
                (from item in treeMapping
                 select new TreeItemRestructure
                 {
                     ActionType = TreeItemActionType.AppendChild,
                     Key = item.Key,
                     TypeOfKey = TreeItemKeyType.TopicHref,
                     RestructuredItems = item.Value.Item2.ToImmutableList(),
                     SourceFiles = new FileAndType[] { item.Value.Item1 }.ToImmutableList(),
                 }).ToImmutableList();

            return collection;
        }

        private SplittedResult SplitModelToOperationGroup(FileModel model)
        {
            if (model.Type != DocumentType.Article)
            {
                return null;
            }

            var content = (RestApiRootItemViewModel)model.Content;

            if (content.Tags.Count == 0 || content.Children.Count == 0)
            {
                return null;
            }

            var tagModels = GenerateTagModels(content).ToList();
            if (tagModels.Count == 0)
            {
                return null;
            }

            var treeItems = new List<TreeItem>();
            var splittedModels = new List<FileModel>();
            foreach (var tagModel in tagModels)
            {
                var newModel = GenerateNewFileModel(model, tagModel);
                splittedModels.Add(newModel);
                treeItems.Add(ConvertToTreeItem(tagModel, newModel.Key));
            }

            // Only keep not tagged children in root model
            var groupedUids = splittedModels.SelectMany(m => m.Uids).Select(u => u.Name).ToList();
            content.Tags = new List<RestApiTagViewModel>();
            content.Children = content.Children.Where(child => !groupedUids.Contains(child.Uid)).ToList();
            model.Content = content;

            return new SplittedResult(model.Key, treeItems.OrderBy(s => s.Metadata[Constants.PropertyName.Name]), splittedModels);
        }

        private IEnumerable<RestApiRootItemViewModel> GenerateTagModels(RestApiRootItemViewModel root)
        {
            foreach (var tag in root.Tags)
            {
                var tagChildren = GetChildrenByTag(root, tag.Name).ToList();
                if (tagChildren.Count > 0)
                {
                    yield return new RestApiRootItemViewModel
                    {
                        Uid = tag.Uid,
                        HtmlId = tag.HtmlId,
                        Name = tag.Name,
                        Conceptual = tag.Conceptual,
                        Description = tag.Description,
                        Documentation = tag.Documentation,
                        Children = tagChildren,
                        Tags = new List<RestApiTagViewModel>(),
                        Metadata = MergeMetadata(root.Metadata, tag.Metadata)
                    };
                }
            }
        }

        private IEnumerable<RestApiChildItemViewModel> GetChildrenByTag(RestApiRootItemViewModel root, string tagName)
        {
            // Only group children into first tag, to keep cross reference unique
            return root.Children.Where(child => child.Tags != null && tagName == child.Tags.FirstOrDefault());
        }

        private FileModel GenerateNewFileModel(FileModel model, RestApiRootItemViewModel tagModel)
        {
            var originalFile = model.FileAndType.File;
            var fileExtension = Path.GetExtension(originalFile);

            // When handlering tags in petstore.swagger.json, the tag file path should be petstore/tag.json, to prevent tag name confliction
            var originalFileName = Path.GetFileName(originalFile);
            var subDirectory = originalFileName.Remove(originalFileName.IndexOf('.'));
            var directory = Path.GetDirectoryName(originalFile);
            var filePath = Path.Combine(directory, subDirectory, tagModel.Name + fileExtension).ToNormalizedPath();

            var newFileAndType = new FileAndType(model.FileAndType.BaseDir, filePath, model.FileAndType.Type, model.FileAndType.SourceDir, model.FileAndType.DestinationDir);
            var newKey = "~/" + RelativePath.GetPathWithoutWorkingFolderChar(filePath);
            var newModel = new FileModel(newFileAndType, tagModel, model.OriginalFileAndType, model.Serializer, newKey)
            {
                LocalPathFromRoot = model.LocalPathFromRoot,
                Uids = CalculateUids(tagModel, model.LocalPathFromRoot)
            };

            return newModel;
        }

        private ImmutableArray<UidDefinition> CalculateUids(RestApiRootItemViewModel root, string file)
        {
            return new[] { new UidDefinition(root.Uid, file) }
                   .Concat(from item in root.Children select new UidDefinition(item.Uid, file)).ToImmutableArray();
        }

        private TreeItem ConvertToTreeItem(RestApiRootItemViewModel root, string fileKey)
        {
            return new TreeItem()
            {
                Metadata = new Dictionary<string, object>()
                {
                    [Constants.PropertyName.Name] = root.Name,
                    [Constants.PropertyName.TopicUid] = root.Uid
                }
            };
        }

        private Dictionary<string, object> MergeMetadata(Dictionary<string, object> rootMetadata, Dictionary<string, object> tagMetadata)
        {
            var result = new Dictionary<string, object>(rootMetadata);
            foreach (var pair in tagMetadata)
            {
                // Root metadata wins for the same key
                if (!result.ContainsKey(pair.Key))
                {
                    result[pair.Key] = tagMetadata[pair.Key];
                }
            }
            return result;
        }

        private sealed class SplittedResult
        {
            public string Key { get; }
            public IEnumerable<TreeItem> TreeItems { get; }
            public IEnumerable<FileModel> Models { get; }

            public SplittedResult(string key, IEnumerable<TreeItem> items, IEnumerable<FileModel> models)
            {
                Key = key;
                TreeItems = items;
                Models = models;
            }
        }
    }
}
