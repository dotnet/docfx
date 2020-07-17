using Microsoft.TripleCrown.Hierarchy.DataContract.Hierarchy;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TripleCrownValidation.Models;

namespace TripleCrownValidation.Validators
{
    public class AchievementValidator : ValidatorBase
    {
        public AchievementValidator(List<LegacyManifestItem> manifestItems, string basePath)
            :base(manifestItems, basePath)
        {
        }

        protected override void ExtractItems()
        {
            if (ManifestItems == null) return;
            Items = ManifestItems.SelectMany(m =>
            {
                var path = Path.Combine(BathPath, m.Output.MetadataOutput.RelativePath);
                if (!File.Exists(path))
                {
                    path = m.Output.MetadataOutput.LinkToPath;
                }

                var achievements = JsonConvert.DeserializeObject<List<AchievementValidateModel>>(File.ReadAllText(path));
                achievements.ForEach(achievement => achievement.SourceRelativePath = m.SourceRelativePath);

                return achievements;
            }).Cast<IValidateModel>().ToList();
        }
        
        public override bool Validate(Dictionary<string, IValidateModel> fullItemsDict)
        {
            var validationResult = true;
            foreach (var item in Items)
            {
                var itemValid = true;
                var achievement = item as AchievementValidateModel;
                var result = achievement.ValidateMetadata();
                if(!string.IsNullOrEmpty(result))
                {
                    itemValid = false;
                    OPSLogger.LogUserError(LogCode.TripleCrown_Achievement_MetadataError, result, item.SourceRelativePath);
                }

                item.IsValid = itemValid;
                validationResult &= itemValid;
            }

            return validationResult;
        }

        protected override HierarchyItem? GetHierarchyItem(ValidatorHierarchyItem validatorHierarchyItem, LegacyManifestItem manifestItem)
        {
            return null;
        }
    }
}
