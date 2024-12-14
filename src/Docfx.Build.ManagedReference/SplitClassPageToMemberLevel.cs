// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Composition;
using Docfx.Build.Common;
using Docfx.Common;
using Docfx.DataContracts.Common;
using Docfx.DataContracts.ManagedReference;
using Docfx.Plugins;
using Newtonsoft.Json.Linq;

namespace Docfx.Build.ManagedReference;

[Export("ManagedReferenceDocumentProcessor", typeof(IDocumentBuildStep))]
public class SplitClassPageToMemberLevel : BaseDocumentBuildStep
{
    private const char OverloadLastChar = '*';
    private const char Separator = '.';
    private const string SplitReferencePropertyName = "_splitReference";
    private const string SplitFromPropertyName = "_splitFrom";
    private const string IsOverloadPropertyName = "_isOverload";
    private const int MaximumFileNameLength = 180;
    private static readonly List<string> EmptyList = [];
    private static readonly string[] EmptyArray = [];

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
        var modelsDict = new Dictionary<string, FileModel>(FilePathComparer.OSPlatformSensitiveStringComparer);
        var dupeModels = new List<FileModel>();

        // Separate items into different models if the PageViewModel contains more than one item
        var treeMapping = new Dictionary<string, Tuple<FileAndType, IEnumerable<TreeItem>>>();
        foreach (var model in models)
        {
            var result = SplitModelToOverloadLevel(model, modelsDict, dupeModels);
            if (result != null)
            {
                if (!treeMapping.TryAdd(result.Uid, Tuple.Create(model.OriginalFileAndType, result.TreeItems)))
                {
                    Logger.LogWarning($"Model with the UID {result.Uid} already exists. '{model.OriginalFileAndType?.FullPath ?? model.FileAndType.FullPath}' is ignored.");
                }
            }
            else
            {
                AddModelToDict(model, modelsDict, dupeModels);
            }
        }

        // New dupe models
        var newFilePaths = new Dictionary<string, int>(FilePathComparer.OSPlatformSensitiveStringComparer);

        foreach (var dupeModel in dupeModels)
        {
            if (modelsDict.Remove(dupeModel.File, out var dupe))
            {
                RenewDupeFileModels(dupe, newFilePaths, modelsDict);
            }
            RenewDupeFileModels(dupeModel, newFilePaths, modelsDict);
        }

        host.TableOfContentRestructions =
            (from item in treeMapping
             select new TreeItemRestructure
             {
                 ActionType = TreeItemActionType.AppendChild,
                 Key = item.Key,
                 TypeOfKey = TreeItemKeyType.TopicUid,
                 RestructuredItems = item.Value.Item2.ToImmutableList(),
                 SourceFiles = new FileAndType[] { item.Value.Item1 }.ToImmutableList(),
             }).ToImmutableList();

        return modelsDict.Values;
    }

    private static void RenewDupeFileModels(FileModel dupeModel, Dictionary<string, int> newFilePaths, Dictionary<string, FileModel> modelsDict)
    {
        var page = (PageViewModel)dupeModel.Content;
        var memberType = page.Items[0]?.Type;
        var newFileName = Path.GetFileNameWithoutExtension(dupeModel.File);

        if (memberType != null)
        {
            newFileName += $"({memberType})";
        }

        var newFilePath = GetUniqueFilePath(dupeModel.File, newFileName, newFilePaths, modelsDict);
        var newModel = GenerateNewFileModel(dupeModel, page, Path.GetFileNameWithoutExtension(newFilePath), []);
        modelsDict[newFilePath] = newModel;
    }

    private static string GetUniqueFilePath(string dupePath, string newFileName, Dictionary<string, int> newFilePaths, Dictionary<string, FileModel> modelsDict)
    {
        var dir = Path.GetDirectoryName(dupePath);
        var extension = Path.GetExtension(dupePath);
        var newFilePath = Path.Combine(dir, newFileName + extension).ToNormalizedPath();

        if (modelsDict.ContainsKey(newFilePath))
        {
            if (newFilePaths.TryGetValue(newFilePath, out int suffix))
            {
                // new file path already exist and have suffix
                newFileName += $"_{suffix}";
            }
            else
            {
                // new file path already exist but doesn't have suffix (special case)
                newFileName += "_1";
                newFilePaths[newFilePath] = 2;
            }

            // check if new file path unique for new file name (cover special case)
            return GetUniqueFilePath(dupePath, newFileName, newFilePaths, modelsDict);
        }
        else
        {
            if (newFilePaths.TryGetValue(newFilePath, out int suffix))
            {
                throw new Exception($"Failed to process new path {newFilePath}");
            }
            else
            {
                newFilePaths[newFilePath] = 1;
            }

            return newFilePath;
        }
    }

    private SplittedResult SplitModelToOverloadLevel(FileModel model, Dictionary<string, FileModel> models, List<FileModel> dupeModels)
    {
        if (model.Type != DocumentType.Article)
        {
            return null;
        }

        var page = (PageViewModel)model.Content;

        if (page.Items.Count <= 1 || page.MemberLayout != MemberLayout.SeparatePages)
        {
            return null;
        }

        // Make sure new file names generated from current page is unique
        var newFileNames = new Dictionary<string, int>(FilePathComparer.OSPlatformSensitiveStringComparer);

        var primaryItem = page.Items[0];
        var itemsToSplit = page.Items.Skip(1);
        var children = new List<TreeItem>();
        var splittedModels = new List<FileModel>();
        var pages = GetNewPages(page).ToList();
        if (pages.Count == 0)
        {
            return null;
        }

        foreach (var newPage in pages)
        {
            var newPrimaryItem = newPage.Items[0];

            var newFileName = GetNewFileName(primaryItem.Uid, newPrimaryItem);
            var newModel = GenerateNewFileModel(model, newPage, newFileName, newFileNames);

            newPage.Metadata[SplitReferencePropertyName] = true;

            AddToTree(newPrimaryItem, children);
            AddModelToDict(newModel, models, dupeModels);
        }

        // Convert children to references
        page.References = itemsToSplit.Select(ConvertToReference).Concat(page.References).ToList();

        page.Items = [primaryItem];
        page.Metadata[SplitReferencePropertyName] = true;
        page.Metadata[SplitFromPropertyName] = true;

        // Regenerate uids
        model.Uids = CalculateUids(page, model.LocalPathFromRoot);
        model.Content = page;

        AddModelToDict(model, models, dupeModels);
        return new SplittedResult(primaryItem.Uid, children.OrderBy(GetDisplayName, StringComparer.Ordinal), splittedModels);
    }

    private static IEnumerable<PageViewModel> GetNewPages(PageViewModel page)
    {
        var primaryItem = page.Items[0];
        if (primaryItem.Type == MemberType.Enum)
        {
            yield break;
        }

        var itemsToSplit = page.Items.Skip(1);
        var group = (from item in itemsToSplit group item by item.Overload).ToList();

        // Per Overload per page
        foreach (var overload in group)
        {
            if (overload.Key == null)
            {
                foreach (var item in overload)
                {
                    yield return ExtractPageViewModel(page, [item]);
                }
            }
            else
            {
                var m = GenerateOverloadPage(page, overload);
                var result = new List<ItemViewModel> { m };
                result.AddRange(overload);
                yield return ExtractPageViewModel(page, result);
            }
        }
    }

    private static void AddToTree(ItemViewModel item, List<TreeItem> tree)
    {
        var treeItem = ConvertToTreeItem(item);
        tree.Add(treeItem);
    }

    private static ItemViewModel GenerateOverloadPage(PageViewModel page, IGrouping<string, ItemViewModel> overload)
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
                [IsOverloadPropertyName] = true
            },
            Platform = MergeList(overload, s => s.Platform ?? EmptyList),
            SupportedLanguages = MergeList(overload, s => s.SupportedLanguages ?? EmptyArray).ToArray(),
            IsExplicitInterfaceImplementation = firstMember.IsExplicitInterfaceImplementation,
            Source = firstMember.Source,
            Documentation = firstMember.Documentation
        };

        var mergeVersion = MergeList(overload, s =>
        {
            List<string> versionList = null;
            if (s.Metadata.TryGetValue(Constants.MetadataName.Version, out object versionObj))
            {
                versionList = GetVersionFromMetadata(versionObj);
            }
            return versionList ?? EmptyList;
        });
        if (mergeVersion != null)
        {
            newPrimaryItem.Metadata[Constants.MetadataName.Version] = mergeVersion;
        }

        var referenceItem = page.References.FirstOrDefault(s => s.Uid == key);
        if (referenceItem != null)
        {
            // The properties defined in reference section overwrites the pre-defined values
            MergeWithReference(newPrimaryItem, referenceItem);
        }

        newPrimaryItem.Name ??= GetOverloadItemName(key, primaryItem.Uid, firstMember.Type == MemberType.Constructor);

        return newPrimaryItem;
    }

    private static List<string> MergeList(IEnumerable<ItemViewModel> children, Func<ItemViewModel, IEnumerable<string>> selector)
    {
        var items = children.SelectMany(selector).Distinct().OrderBy(s => s, StringComparer.Ordinal).ToList();
        if (items.Count == 0)
        {
            return null;
        }

        return items;
    }

    private static List<string> GetVersionFromMetadata(object value)
    {
        if (value is string text)
        {
            return [text];
        }

        return GetListFromObject(value);
    }

    private static string GetOverloadItemName(string overload, string parent, bool isCtor)
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

        if (overload.StartsWith(parent, StringComparison.Ordinal))
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

    private ReferenceViewModel ConvertToReference(ItemViewModel item)
    {
        // Save minimal info, as FillReferenceInformation will fill info from ItemViewModel if the property is needed
        var reference = new ReferenceViewModel
        {
            Uid = item.Uid,
            Parent = item.Parent,
            Name = item.Name,
            NameWithType = item.NameWithType,
            FullName = item.FullName,
        };

        foreach (var pair in item.Names)
        {
            reference.NameInDevLangs[pair.Key] = pair.Value;
        }
        foreach (var pair in item.FullNames)
        {
            reference.FullNameInDevLangs[pair.Key] = pair.Value;
        }
        foreach (var pair in item.NamesWithType)
        {
            reference.NameWithTypeInDevLangs[pair.Key] = pair.Value;
        }

        return reference;
    }

    private static void MergeWithReference(ItemViewModel item, ReferenceViewModel reference)
    {
        item.Name = reference.Name;
        item.NameWithType = reference.NameWithType;
        item.FullName = reference.FullName;
        item.CommentId = reference.CommentId;

        foreach (var pair in reference.NameInDevLangs)
        {
            item.Names[pair.Key] = pair.Value;
        }
        foreach (var pair in reference.FullNameInDevLangs)
        {
            item.FullNames[pair.Key] = pair.Value;
        }
        foreach (var pair in reference.NameWithTypeInDevLangs)
        {
            item.NamesWithType[pair.Key] = pair.Value;
        }

        // SHOULD sync with ItemViewModel & ReferenceViewModel
        // Make sure key inside Additional dictionary does not contain the same key value as ItemViewModel
        foreach (var pair in reference.Additional)
        {
            switch (pair.Key)
            {
                case "summary":
                    {
                        if (pair.Value is string summary)
                        {
                            item.Summary = summary;
                        }
                        break;
                    }
                case "remarks":
                    {
                        if (pair.Value is string remarks)
                        {
                            item.Remarks = remarks;
                        }
                        break;
                    }
                case "example":
                    {
                        var examples = GetListFromObject(pair.Value);
                        if (examples != null)
                        {
                            item.Examples = examples;
                        }
                        break;
                    }
                case Constants.PropertyName.Id:
                case Constants.PropertyName.Type:
                case Constants.PropertyName.Source:
                case Constants.PropertyName.Documentation:
                case "isEii":
                case "isExtensionMethod":
                case "children":
                case "assemblies":
                case "namespace":
                case "langs":
                case "syntax":
                case "overridden":
                case "overload":
                case "exceptions":
                case "seealso":
                case "see":
                    break;
                default:
                    item.Metadata[pair.Key] = pair.Value;
                    break;
            }
        }
    }

    private static TreeItem ConvertToTreeItem(ItemViewModel item, Dictionary<string, object> overwriteMetadata = null)
    {
        var result = new TreeItem
        {
            Metadata =
            {
                [Constants.PropertyName.Name] = item.Name,
                [Constants.PropertyName.FullName] = item.FullName,
                [Constants.PropertyName.TopicUid] = item.Uid,
                [Constants.PropertyName.NameWithType] = item.NameWithType,
                [Constants.PropertyName.Type] = item.Type.ToString(),
                ["isEii"] = item.IsExplicitInterfaceImplementation
            }
        };
        if (item.Platform != null)
        {
            result.Metadata[Constants.PropertyName.Platform] = item.Platform;
        }

        if (item.Metadata.TryGetValue(Constants.MetadataName.Version, out object version))
        {
            result.Metadata[Constants.MetadataName.Version] = version;
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

    private static PageViewModel ExtractPageViewModel(PageViewModel page, List<ItemViewModel> items)
    {
        var newPage = new PageViewModel
        {
            Items = items,
            Metadata = new Dictionary<string, object>(page.Metadata),
            References = page.References,
            ShouldSkipMarkup = page.ShouldSkipMarkup
        };
        return newPage;
    }

    private static FileModel GenerateNewFileModel(FileModel model, PageViewModel newPage, string fileNameWithoutExtension, Dictionary<string, int> existingFileNames)
    {
        var initialFile = model.FileAndType.File;
        var extension = Path.GetExtension(initialFile);
        var directory = Path.GetDirectoryName(initialFile);

        // encode file name to clean url so that without server hosting, href can work with file:/// navigation
        var cleanUrlFileName = fileNameWithoutExtension.ToCleanUrlFileName();
        var actualFileName = GetUniqueFileNameWithSuffix(cleanUrlFileName, existingFileNames);
        var newFileName = actualFileName + extension;
        var filePath = Path.Combine(directory, newFileName).ToNormalizedPath();

        var newFileAndType = new FileAndType(model.FileAndType.BaseDir, filePath, model.FileAndType.Type, model.FileAndType.SourceDir, model.FileAndType.DestinationDir);
        var keyForModel = "~/" + RelativePath.GetPathWithoutWorkingFolderChar(filePath);

        return new FileModel(newFileAndType, newPage, model.OriginalFileAndType, keyForModel)
        {
            LocalPathFromRoot = model.LocalPathFromRoot,
            Uids = CalculateUids(newPage, model.LocalPathFromRoot)
        };
    }

    private static string GetUniqueFileNameWithSuffix(string fileName, Dictionary<string, int> existingFileNames)
    {
        if (existingFileNames.TryGetValue(fileName, out int suffix))
        {
            existingFileNames[fileName] = suffix + 1;
            return GetUniqueFileNameWithSuffix($"{fileName}_{suffix}", existingFileNames);
        }
        else
        {
            existingFileNames[fileName] = 1;
            return fileName;
        }
    }

    private static string GetNewFileName(string parentUid, ItemViewModel model)
    {
        // For constructor, if the class is generic class e.g. ExpandedWrapper`11, class name can be pretty long
        // Use -ctor as file name
        return GetValidFileName(
            model.Uid.TrimEnd(OverloadLastChar),
            $"{parentUid}.{model.Name}",
            $"{parentUid}.{model.Name.Split(Separator).Last()}",
            $"{parentUid}.{Guid.NewGuid()}"
            );
    }

    private static string GetValidFileName(params string[] fileNames)
    {
        foreach (var fileName in fileNames)
        {
            if (!string.IsNullOrEmpty(fileName) && fileName.Length <= MaximumFileNameLength)
            {
                return fileName;
            }
        }

        var message = $"All the file name candidates {fileNames.ToDelimitedString()} exceed the maximum allowed file name length {MaximumFileNameLength}";
        Logger.LogError(message, code: ErrorCodes.Build.FileNamesMaxLengthExceeded);
        throw new DocumentException(message);
    }

    private static ImmutableArray<UidDefinition> CalculateUids(PageViewModel page, string file)
    {
        return (from item in page.Items
                where !string.IsNullOrEmpty(item.Uid)
                select new UidDefinition(item.Uid, file)).ToImmutableArray();
    }

    private static List<string> GetListFromObject(object value)
    {
        if (value is IEnumerable<object> collection)
        {
            return collection.OfType<string>().ToList();
        }

        if (value is JArray jArray)
        {
            try
            {
                return jArray.ToObject<List<string>>();
            }
            catch (Exception)
            {
                Logger.LogWarning($"Unknown version metadata: {jArray}");
            }
        }

        return null;
    }

    private static T GetPropertyValue<T>(Dictionary<string, object> metadata, string key) where T : class
    {
        if (metadata != null && metadata.TryGetValue(key, out object result))
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
        public ItemViewModel PrimaryItem { get; }
        public FileModel FileModel { get; }

        public ModelWrapper(ItemViewModel item, FileModel fileModel)
        {
            PrimaryItem = item;
            FileModel = fileModel;
        }
    }

    private static void AddModelToDict(FileModel model, Dictionary<string, FileModel> models, List<FileModel> dupeModels)
    {
        if (!models.TryAdd(model.File, model))
        {
            dupeModels.Add(model);
        }
    }
}
