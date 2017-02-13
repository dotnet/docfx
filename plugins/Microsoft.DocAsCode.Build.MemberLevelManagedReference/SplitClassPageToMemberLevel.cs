﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.ManagedReference
{
    using System;
    using System.Collections.Generic;
    using System.Composition;
    using System.Linq;
    using System.IO;
    using System.Collections.Immutable;

    using Microsoft.DocAsCode.Build.Common;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.DataContracts.ManagedReference;
    using Microsoft.DocAsCode.Plugins;

    [Export("ManagedReferenceDocumentProcessor", typeof(IDocumentBuildStep))]
    public class SplitClassPageToMemberLevel : BaseDocumentBuildStep
    {
        private const char OverloadLastChar = '*';
        private const char Separator = '.';
        private const string SplitReferencePropertyName = "_splitReference";
        private const string IsOverloadPropertyName = "_isOverload";

        public override string Name => nameof(SplitClassPageToMemberLevel);

        public override int BuildOrder => 1;

        /// <summary>
        /// Extract: group with overload
        /// </summary>
        /// <param name="models"></param>
        /// <param name="host"></param>
        /// <returns></returns>
        public override IEnumerable<FileModel> Prebuild(ImmutableList<FileModel> models, IHostService host)
        {
            var collection = new List<FileModel>(models);

            // Separate items into different models if the PageViewModel contains more than one item
            var treeMapping = new Dictionary<string, IEnumerable<TreeItem>>();
            foreach (var model in models)
            {
                var result = SplitModelToOverloadLevel(model);
                if (result != null)
                {
                    if (treeMapping.ContainsKey(result.Uid))
                    {
                        Logger.LogWarning($"Model with the UID {result.Uid} already exists. '{model.OriginalFileAndType?.FullPath ?? model.FileAndType.FullPath}' is ignored.");
                    }
                    else
                    {
                        treeMapping.Add(result.Uid, result.TreeItems);
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
                     TypeOfKey = TreeItemKeyType.TopicUid,
                     RestructuredItems = item.Value.ToImmutableList(),
                 }).ToImmutableList();

            return collection;
        }

        private SplittedResult SplitModelToOverloadLevel(FileModel model)
        {
            if (model.Type != DocumentType.Article)
            {
                return null;
            }

            var page = (PageViewModel)model.Content;

            if (page.Items.Count <= 1)
            {
                return null;
            }

            var primaryItem = page.Items[0];
            var itemsToSplit = page.Items.Skip(1);

            var children = new List<TreeItem>();
            var splittedModels = new List<FileModel>();

            var group = (from item in itemsToSplit group item by item.Overload).ToList();

            // Per Overload per page
            foreach (var overload in group)
            {
                if (overload.Key == null)
                {
                    foreach (var i in overload)
                    {
                        var m = GenerateNonOverloadPage(page, model, i);
                        splittedModels.Add(m.FileModel);

                        // Order toc by display name
                        children.Add(m.TreeItem);
                    }
                }
                else
                {
                    var m = GenerateOverloadPage(page, model, overload);
                    splittedModels.Add(m.FileModel);
                    children.Add(m.TreeItem);
                }
            }

            // Convert children to references
            page.References = itemsToSplit.Select(ConvertToReference).Concat(page.References).ToList();

            page.Items = new List<ItemViewModel> { primaryItem };

            // Regenerate uids
            model.Uids = CalculateUids(page, model.LocalPathFromRoot);
            model.Content = page;
            return new SplittedResult(primaryItem.Uid, children.OrderBy(s => GetDisplayName(s)), splittedModels);
        }

        private ModelWrapper GenerateNonOverloadPage(PageViewModel page, FileModel model, ItemViewModel item)
        {
            item.Metadata[SplitReferencePropertyName] = true;
            var newPage = ExtractPageViewModel(page, new List<ItemViewModel> { item });
            var newModel = GenerateNewFileModel(model, newPage, item.Uid);
            var tree = ConvertToTreeItem(item);
            return new ModelWrapper(newPage, newModel, tree);
        }

        private ModelWrapper GenerateOverloadPage(PageViewModel page, FileModel model, IGrouping<string, ItemViewModel> overload)
        {
            var primaryItem = page.Items[0];

            // For ctor, rename #ctor to class name
            var firstMember = overload.First();
            var key = overload.Key;

            var newPrimaryItem = new ItemViewModel
            {
                Uid = key,
                Children = overload.Select(s => s.Uid).ToList(),
                Type = firstMember.Type,
                AssemblyNameList = firstMember.AssemblyNameList,
                NamespaceName = firstMember.NamespaceName,
                Metadata = new Dictionary<string, object>
                {
                    [IsOverloadPropertyName] = true,
                    [SplitReferencePropertyName] = true
                }
            };
            var referenceItem = page.References.FirstOrDefault(s => s.Uid == key);
            if (referenceItem != null)
            {
                MergeWithReference(newPrimaryItem, referenceItem);
            }

            if (newPrimaryItem.Name == null)
            {
                newPrimaryItem.Name = GetOverloadItemName(key, primaryItem.Uid, firstMember.Type == MemberType.Constructor);
            }

            var newPage = ExtractPageViewModel(page, new List<ItemViewModel> { newPrimaryItem }.Concat(overload).ToList());
            var newModel = GenerateNewFileModel(model, newPage, overload.Key.Trim(OverloadLastChar));
            var tree = ConvertToTreeItem(
                newPrimaryItem,
                new Dictionary<string, object>
                {
                    [Constants.PropertyName.Type] = firstMember.Type
                });
            return new ModelWrapper(newPage, newModel, tree);
        }

        private string GetOverloadItemName(string overload, string parent, bool isCtor)
        {
            if (string.IsNullOrEmpty(overload) || string.IsNullOrEmpty(parent))
            {
                return overload;
            }

            if (isCtor)
            {
                // Replace #ctor with parent name
                var parts = parent.Split(Separator);
                return parts[parts.Length - 1];
            }

            if (overload.StartsWith(parent))
            {
                return overload.Substring(parent.Length).Trim(Separator, OverloadLastChar);
            }
            return overload;
        }

        private string GetDisplayName(TreeItem item)
        {
            var metadata = item.Metadata;
            return GetPropertyValue<string>(metadata, Constants.PropertyName.Name)
                ?? GetPropertyValue<string>(metadata, Constants.PropertyName.FullName)
                ?? GetPropertyValue<string>(metadata, Constants.PropertyName.TopicUid);
        }

        /// <summary>
        /// TODO: can save minimum info when ApiBuildOutput depends on global references instead of current page only
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private ReferenceViewModel ConvertToReference(ItemViewModel item)
        {
            // Use serializer to keep most info from item, e.g. reference's summary is possibly required by class template
            return YamlUtility.ConvertTo<ReferenceViewModel>(item);
        }

        private void MergeWithReference(ItemViewModel item, ReferenceViewModel reference)
        {
            item.Name = reference.Name;
            item.NameWithType = reference.NameWithType;
            item.FullName = reference.FullName;
            item.CommentId = reference.CommentId;

            if (reference.NameInDevLangs.Count > 0)
            {
                foreach (var pair in item.Names)
                {
                    item.Metadata[Constants.ExtensionMemberPrefix.Name + pair.Key] = pair.Value;
                }
            }
            if (reference.FullNameInDevLangs.Count > 0)
            {
                foreach (var pair in item.FullNames)
                {
                    item.Metadata[Constants.ExtensionMemberPrefix.FullName + pair.Key] = pair.Value;
                }
            }
            if (reference.NameWithTypeInDevLangs.Count > 0)
            {
                foreach (var pair in item.NamesWithType)
                {
                    item.Metadata[Constants.ExtensionMemberPrefix.NameWithType + pair.Key] = pair.Value;
                }
            }
        }

        private TreeItem ConvertToTreeItem(ItemViewModel item, Dictionary<string, object> overwriteMetadata = null)
        {
            var result = new TreeItem();
            result.Metadata = new Dictionary<string, object>()
            {
                [Constants.PropertyName.Name] = item.Name,
                [Constants.PropertyName.FullName] = item.FullName,
                [Constants.PropertyName.TopicUid] = item.Uid,
                [Constants.PropertyName.NameWithType] = item.NameWithType,
                [Constants.PropertyName.Type] = item.Type.ToString(),
            };

            if (item.Names.Count > 0)
            {
                foreach (var pair in item.Names)
                {
                    result.Metadata[Constants.ExtensionMemberPrefix.Name + pair.Key] = pair.Value;
                }
            }
            if (item.FullNames.Count > 0)
            {
                foreach (var pair in item.FullNames)
                {
                    result.Metadata[Constants.ExtensionMemberPrefix.FullName + pair.Key] = pair.Value;
                }
            }
            if (item.NamesWithType.Count > 0)
            {
                foreach (var pair in item.NamesWithType)
                {
                    result.Metadata[Constants.ExtensionMemberPrefix.NameWithType + pair.Key] = pair.Value;
                }
            }

            if (overwriteMetadata != null)
            {
                foreach (var pair in overwriteMetadata)
                {
                    result.Metadata[pair.Key] = pair.Value;
                }
            }
            return result;
        }

        private PageViewModel ExtractPageViewModel(PageViewModel page, List<ItemViewModel> items)
        {
            var newPage = new PageViewModel
            {
                Items = items,
                Metadata = new Dictionary<string, object>(page.Metadata),
                References = new List<ReferenceViewModel>(page.References.Select(s => s.Clone())),
                ShouldSkipMarkup = page.ShouldSkipMarkup
            };
            return newPage;
        }

        private FileModel GenerateNewFileModel(FileModel model, PageViewModel newPage, string key)
        {
            var initialFile = model.FileAndType.File;
            var extension = Path.GetExtension(initialFile);
            var directory = Path.GetDirectoryName(initialFile);
            var newFileName = PathUtility.ToValidFilePath(key, '-').Replace('`', '-') + extension;
            var filePath = Path.Combine(directory, newFileName).ToNormalizedPath();
            var newFileAndType = new FileAndType(model.FileAndType.BaseDir, filePath, model.FileAndType.Type, model.FileAndType.SourceDir, model.FileAndType.DestinationDir);
            var newModel = new FileModel(newFileAndType, newPage, null, model.Serializer);
            newModel.LocalPathFromRoot = model.LocalPathFromRoot;
            newModel.Uids = CalculateUids(newPage, model.LocalPathFromRoot);
            return newModel;
        }

        private ImmutableArray<UidDefinition> CalculateUids(PageViewModel page, string file)
        {
            return (from item in page.Items select new UidDefinition(item.Uid, file)).ToImmutableArray();
        }

        private T GetPropertyValue<T>(Dictionary<string, object> metadata, string key) where T : class
        {
            object result;
            if (metadata != null && metadata.TryGetValue(key, out result))
            {
                return result as T;
            }

            return null;
        }

        private sealed class SplittedResult
        {
            public string Uid { get; }
            public IEnumerable<TreeItem> TreeItems { get; }
            public IEnumerable<FileModel> Models { get; }

            public SplittedResult(string uid, IEnumerable<TreeItem> items, IEnumerable<FileModel> models)
            {
                Uid = uid;
                TreeItems = items;
                Models = models;
            }
        }

        private sealed class ModelWrapper
        {
            public PageViewModel PageViewModel { get; }
            public FileModel FileModel { get; }
            public TreeItem TreeItem { get; }

            public ModelWrapper(PageViewModel page, FileModel fileModel, TreeItem tree)
            {
                PageViewModel = page;
                FileModel = fileModel;
                TreeItem = tree;
            }
        }
    }
}
