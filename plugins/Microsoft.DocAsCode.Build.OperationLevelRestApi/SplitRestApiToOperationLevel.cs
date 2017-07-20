// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.OperationLevelRestApi
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
    public class SplitRestApiToOperationLevel : BaseDocumentBuildStep
    {
        public override string Name => nameof(SplitRestApiToOperationLevel);

        // Set build order as 2, to run after SplitRestApiToTagLevel
        public override int BuildOrder => 2;

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

                    var tocRestuctions = result.Item2;
                    var duplicateKeys = tocRestuctions.Where(t => treeItemRestructions.Any(i => i.Key == t.Key)).Select(i => i.Key);
                    if (duplicateKeys.Any())
                    {
                        Logger.LogWarning($"Model with the key {string.Join(",", duplicateKeys)} already exists. '{model.OriginalFileAndType?.FullPath ?? model.FileAndType.FullPath}' is ignored.");
                    }
                    else
                    {
                        treeItemRestructions.AddRange(tocRestuctions);
                    }
                }
            }

            host.TableOfContentRestructions = host.TableOfContentRestructions == null ?
                treeItemRestructions.ToImmutableList() :
                host.TableOfContentRestructions.Concat(treeItemRestructions).ToImmutableList();

            return collection;
        }

        private Tuple<List<FileModel>, List<TreeItemRestructure>> SplitModelToOperationGroup(FileModel model)
        {
            if (model.Type != DocumentType.Article)
            {
                return null;
            }

            var content = (RestApiRootItemViewModel)model.Content;

            if (content.Children.Count == 0)
            {
                return null;
            }

            var treeItems = new List<TreeItem>();
            var splittedModels = new List<FileModel>();
            foreach (var operationModel in GenerateOperationModels(content))
            {
                operationModel.Metadata["_isSplittedToOperation"] = true;
                var newModel = GenerateNewFileModel(model, operationModel);
                splittedModels.Add(newModel);
                treeItems.Add(ConvertToTreeItem(operationModel, newModel.Key));
            }

            // Reset children
            content.Children = new List<RestApiChildItemViewModel>();
            content.Metadata["_isSplittedByOperation"] = true;
            content.Tags = new List<RestApiTagViewModel>();
            model.Content = content;

            // Reset uid definition
            model.Uids = new[] { new UidDefinition(content.Uid, model.LocalPathFromRoot) }.ToImmutableArray();

            // Support both restructure by TopicHref and TopicUid
            var treeItemRestructions = new List<TreeItemRestructure>
            {
                new TreeItemRestructure
                {
                    ActionType = TreeItemActionType.AppendChild,
                    Key = model.Key,
                    TypeOfKey = TreeItemKeyType.TopicHref,
                    RestructuredItems = treeItems.OrderBy(s => s.Metadata[Constants.PropertyName.Name]).ToImmutableList(),
                    SourceFiles = new FileAndType[] { model.OriginalFileAndType }.ToImmutableList(),
                },
                new TreeItemRestructure
                {
                    ActionType = TreeItemActionType.AppendChild,
                    Key = content.Uid,
                    TypeOfKey = TreeItemKeyType.TopicUid,
                    RestructuredItems = treeItems.OrderBy(s => s.Metadata[Constants.PropertyName.Name]).ToImmutableList(),
                    SourceFiles = new FileAndType[] { model.OriginalFileAndType }.ToImmutableList(),
                }
            };

            return Tuple.Create(splittedModels, treeItemRestructions);
        }

        private IEnumerable<RestApiRootItemViewModel> GenerateOperationModels(RestApiRootItemViewModel root)
        {
            foreach (var child in root.Children)
            {
                // Pop child's info into root model, include the uid
                var model = new RestApiRootItemViewModel
                {
                    Uid = child.Uid,
                    Name = child.OperationId,
                    Conceptual = child.Conceptual,
                    Description = child.Description,
                    Summary = child.Summary,
                    Remarks = child.Remarks,
                    Documentation = child.Documentation,
                    Children = new List<RestApiChildItemViewModel> { child },
                    Tags = new List<RestApiTagViewModel>(),
                    Metadata = MergeChildMetadata(root, child)
                };

                // Reset child's uid to "originalUid/operation", that is to say, overwrite of original Uid will show in operation page.
                child.Uid = string.Join("/", child.Uid, "operation");

                // Reset html id, which is set by template
                child.HtmlId = null;

                // Reset child's additional content, which will show in operation page.
                child.Conceptual = null;
                child.Description = null;
                child.Summary = null;
                child.Remarks = null;
                child.Tags = new List<string>();

                yield return model;
            }
        }

        private FileModel GenerateNewFileModel(FileModel model, RestApiRootItemViewModel operationModel)
        {
            var originalFile = model.FileAndType.File;
            var fileExtension = Path.GetExtension(originalFile);

            // When split into operation for petstore.swagger.json, the operation file path should be petstore/{operationName}.json, to prevent operation name confliction
            var originalFileName = Path.GetFileName(originalFile);
            var subDirectory = originalFileName.Remove(originalFileName.IndexOf('.'));
            var directory = Path.GetDirectoryName(originalFile);
            var filePath = Path.Combine(directory, subDirectory, operationModel.Name + fileExtension).ToNormalizedPath();

            var newFileAndType = new FileAndType(model.FileAndType.BaseDir, filePath, model.FileAndType.Type, model.FileAndType.SourceDir, model.FileAndType.DestinationDir);
            var newKey = "~/" + RelativePath.GetPathWithoutWorkingFolderChar(filePath);
            var newModel = new FileModel(newFileAndType, operationModel, model.OriginalFileAndType, model.Serializer, newKey)
            {
                LocalPathFromRoot = model.LocalPathFromRoot,
                Uids = new[] { new UidDefinition(operationModel.Uid, model.LocalPathFromRoot) }.ToImmutableArray()
            };

            return newModel;
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

        private Dictionary<string, object> MergeChildMetadata(RestApiRootItemViewModel root, RestApiChildItemViewModel child)
        {
            var result = new Dictionary<string, object>(child.Metadata);
            foreach (var pair in root.Metadata)
            {
                // Child metadata wins for the same key
                if (!result.ContainsKey(pair.Key))
                {
                    result[pair.Key] = pair.Value;
                }
            }
            return result;
        }
    }
}
