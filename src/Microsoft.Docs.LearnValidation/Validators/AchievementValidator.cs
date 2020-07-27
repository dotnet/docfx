// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.TripleCrown.Hierarchy.DataContract.Hierarchy;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Linq;


namespace Microsoft.Docs.LearnValidation
{
    public class AchievementValidator : ValidatorBase
    {
        public AchievementValidator(List<LegacyManifestItem> manifestItems, string basePath, LearnValidationLogger logger)
            :base(manifestItems, basePath, logger)
        {
        }

        protected override void ExtractItems()
        {
            if (ManifestItems == null) return;
            Items = ManifestItems.SelectMany(m =>
            {
                var path = Path.Combine(BathPath, m.Output.TocOutput.RelativePath!);
                if (!File.Exists(path))
                {
                    path = m.Output.MetadataOutput.LinkToPath;
                }

                var achievements = JsonConvert.DeserializeObject<List<AchievementValidateModel>>(
                    JsonConvert.DeserializeObject<JObject>(File.ReadAllText(path)).Value<string>("content"));
                
                achievements.ForEach(achievement => achievement.SourceRelativePath = m.SourceRelativePath!);

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
                    Logger.Log(LearnErrorLevel.Error, LearnErrorCode.TripleCrown_Achievement_MetadataError, result, item.SourceRelativePath);
                }

                item.IsValid = itemValid;
                validationResult &= itemValid;
            }

            return validationResult;
        }

        /// <summary>
        /// won't be called
        /// </summary>
        protected override HierarchyItem GetHierarchyItem(ValidatorHierarchyItem validatorHierarchyItem, LegacyManifestItem manifestItem)
            => null;
    }
}
