// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.TripleCrown.Hierarchy.DataContract.Hierarchy;
using Newtonsoft.Json;

namespace Microsoft.Docs.LearnValidation;

public abstract class ValidatorBase
{
    public List<IValidateModel> Items { get; protected set; } = new List<IValidateModel>();

    protected LearnValidationLogger Logger { get; }

    protected List<LegacyManifestItem> ManifestItems { get; }

    protected string BathPath { get; }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2214:Do not call overridable methods in constructors", Justification = "Legacy")]
    public ValidatorBase(List<LegacyManifestItem> manifestItems, string basePath, LearnValidationLogger logger)
    {
        ManifestItems = manifestItems;
        BathPath = basePath;
        Logger = logger;

        ExtractItems();
    }

    public abstract bool Validate(Dictionary<string, IValidateModel> fullItemsDict);

    protected virtual HierarchyItem GetHierarchyItem(ValidatorHierarchyItem validatorHierarchyItem, LegacyManifestItem manifestItem) => throw new NotImplementedException();

    protected virtual void ExtractItems()
    {
        if (ManifestItems == null)
        {
            return;
        }

        var items = new IValidateModel[ManifestItems.Count];
        Parallel.For(0, ManifestItems.Count, i =>
        {
            var manifestItem = ManifestItems[i];
            var path = Path.Combine(BathPath, manifestItem?.Output?.MetadataOutput?.RelativePath ?? "");
            if (!File.Exists(path))
            {
                path = manifestItem?.Output?.MetadataOutput?.LinkToPath ?? "";
            }
            var validatorHierarchyItem = JsonConvert.DeserializeObject<ValidatorHierarchyItem>(File.ReadAllText(path)) ?? new();
            var hierarchyItem = GetHierarchyItem(validatorHierarchyItem, manifestItem ?? new LegacyManifestItem());
            MergeToHierarchyItem(validatorHierarchyItem, hierarchyItem);
            items[i] = (IValidateModel)hierarchyItem;
        });
        Items = items.ToList();
    }

    protected virtual void SetHierarchyData(IValidateModel item, ValidatorHierarchyItem validatorHierarchyItem, LegacyManifestItem manifestItem)
    {
        item.SourceRelativePath = manifestItem.SourceRelativePath!;
        item.AssetId = manifestItem.AssetId;
        item.MSDate = validatorHierarchyItem.MSDate;
        item.PublishUpdatedAt = validatorHierarchyItem.PublishUpdatedAt;
        item.PageKind = validatorHierarchyItem.PageKind;
        item.ServiceData = validatorHierarchyItem.ServiceData;
    }

    protected static void MergeToHierarchyItem(ValidatorHierarchyItem validatorHierarchyItem, HierarchyItem hierarchyItem)
    {
        hierarchyItem.Abstract = validatorHierarchyItem.Abstract;
        hierarchyItem.Branch = validatorHierarchyItem.Branch;
        hierarchyItem.DepotName = validatorHierarchyItem.DepotName;
        hierarchyItem.Locale = validatorHierarchyItem.Locale;
        hierarchyItem.Points = validatorHierarchyItem.Points;
        hierarchyItem.Summary = validatorHierarchyItem.Summary;
        hierarchyItem.Url = validatorHierarchyItem.Url;
    }
}
