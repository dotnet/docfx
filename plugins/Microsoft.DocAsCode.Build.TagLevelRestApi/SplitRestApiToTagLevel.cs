// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.TagLevelRestApi
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
    public class SplitRestApiToTagLevel : BaseDocumentBuildStep
    {
        public override string Name => nameof(SplitRestApiToTagLevel);

        public override int BuildOrder => 1;

        public override IEnumerable<FileModel> Prebuild(ImmutableList<FileModel> models, IHostService host)
        {
            var collection = new List<FileModel>(models);

            var treeItemRestructions = new List<TreeItemRestructure>();
            foreach (var model in models)
            {
                var result = SplitModelToOperationGroup(model);
                if (result != null)
                {
                    collection.AddRange(result.Item1);

                    var tocRestuction = result.Item2;
                    if (treeItemRestructions.Any(i => i.Key == tocRestuction.Key))
                    {
                        Logger.LogWarning($"Model with the key {tocRestuction.Key} already exists. '{model.OriginalFileAndType?.FullPath ?? model.FileAndType.FullPath}' is ignored.");
                    }
                    else
                    {
                        treeItemRestructions.Add(tocRestuction);
                    }
                }
            }

            host.TableOfContentRestructions = host.TableOfContentRestructions == null ?
                treeItemRestructions.ToImmutableList() :
                host.TableOfContentRestructions.Concat(treeItemRestructions).ToImmutableList();

            return collection;
        }

        private Tuple<List<FileModel>, TreeItemRestructure> SplitModelToOperationGroup(FileModel model)
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
                tagModel.Metadata["_isSplittedToTag"] = true;
                var newModel = GenerateNewFileModel(model, tagModel);
                splittedModels.Add(newModel);
                treeItems.Add(ConvertToTreeItem(tagModel, newModel.Key));
            }

            // Only keep not tagged children in root model
            var groupedUids = splittedModels.SelectMany(m => m.Uids).Select(u => u.Name).ToList();
            content.Tags = new List<RestApiTagViewModel>();
            content.Children = content.Children.Where(child => !groupedUids.Contains(child.Uid)).ToList();
            content.Metadata["_isSplittedByTag"] = true;
            model.Content = content;

            // Reset uid definition
            model.Uids = model.Uids.Where(u => !groupedUids.Contains(u.Name)).ToImmutableArray();

            var treeItemRestruction = new TreeItemRestructure
            {
                ActionType = TreeItemActionType.AppendChild,
                Key = model.Key,
                TypeOfKey = TreeItemKeyType.TopicHref,
                RestructuredItems = treeItems.OrderBy(s => s.Metadata[Constants.PropertyName.Name]).ToImmutableList(),
                SourceFiles = new FileAndType[] { model.OriginalFileAndType }.ToImmutableList(),
            };

            return Tuple.Create(splittedModels, treeItemRestruction);
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
                        Metadata = MergeTagMetadata(root, tag)
                    };
                }
            }
        }

        private IEnumerable<RestApiChildItemViewModel> GetChildrenByTag(RestApiRootItemViewModel root, string tagName)
        {
            // Only group children into first tag, to keep cross reference unique
            var children = root.Children.Where(child => child.Tags != null && tagName == child.Tags.FirstOrDefault());
            foreach (var child in children)
            {
                child.Tags = new List<string>();
                yield return child;
            }
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
                Uids = CalculateUids(tagModel).Select(i => new UidDefinition(i, model.LocalPathFromRoot)).ToImmutableArray()
        };

            return newModel;
        }

        private IEnumerable<string> CalculateUids(RestApiRootItemViewModel root)
        {
            if (!string.IsNullOrEmpty(root.Uid))
            {
                yield return root.Uid;
            }
            foreach (var child in root.Children ?? Enumerable.Empty<RestApiChildItemViewModel>())
            {
                if (!string.IsNullOrEmpty(child.Uid))
                {
                    yield return child.Uid;
                }
            }
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

        private Dictionary<string, object> MergeTagMetadata(RestApiRootItemViewModel root, RestApiTagViewModel tag)
        {
            var result = new Dictionary<string, object>(tag.Metadata);
            foreach (var pair in root.Metadata)
            {
                // Tag metadata wins for the same key
                if (!result.ContainsKey(pair.Key))
                {
                    result[pair.Key] = pair.Value;
                }
            }
            return result;
        }
    }
}
