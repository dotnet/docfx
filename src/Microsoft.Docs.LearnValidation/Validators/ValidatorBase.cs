using Microsoft.TripleCrown.Hierarchy.DataContract.Hierarchy;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TripleCrownValidation.Models;

namespace TripleCrownValidation.Validators
{
    public abstract class ValidatorBase
    {
        protected List<LegacyManifestItem> ManifestItems { get; set; }
        protected string BathPath { get; set; }
        public bool IsValidated { get; private set; }
        public List<IValidateModel> Items { get; set; } = new List<IValidateModel>();

        public ValidatorBase(List<LegacyManifestItem> manifestItems, string basePath)
        {
            ManifestItems = manifestItems;
            BathPath = basePath;

            ExtractItems();
        }

        public abstract bool Validate(Dictionary<string, IValidateModel> fullItemsDict);
        protected abstract HierarchyItem GetHierarchyItem(ValidatorHierarchyItem validatorHierarchyItem, LegacyManifestItem manifestItem);

        protected virtual void ExtractItems()
        {
            if (ManifestItems == null) return;
            Items = ManifestItems.Select(m =>
            {
                var path = Path.Combine(BathPath, m.Output.MetadataOutput.RelativePath);
                if (!File.Exists(path))
                {
                    path = m.Output.MetadataOutput.LinkToPath;
                }
                var validatorHierarchyItem = JsonConvert.DeserializeObject<ValidatorHierarchyItem>(File.ReadAllText(path));
                var hierarchyItem = GetHierarchyItem(validatorHierarchyItem, m);
                MergeToHierarchyItem(validatorHierarchyItem, hierarchyItem);
                return hierarchyItem;
            }).Cast<IValidateModel>().ToList();
        }

        protected virtual void SetHierarchyData(IValidateModel item, ValidatorHierarchyItem validatorHierarchyItem, LegacyManifestItem manifestItem)
        {
            item.SourceRelativePath = manifestItem.SourceRelativePath;
            item.AssetId = manifestItem.AssetId;
            item.MSDate = validatorHierarchyItem.MSDate;
            item.PublishUpdatedAt = validatorHierarchyItem.PublishUpdatedAt;
            item.PageKind = validatorHierarchyItem.PageKind;
            item.ServiceData = validatorHierarchyItem.ServiceData;
        }

        protected void MergeToHierarchyItem(ValidatorHierarchyItem validatorHierarchyItem, HierarchyItem hierarchyItem)
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
}
