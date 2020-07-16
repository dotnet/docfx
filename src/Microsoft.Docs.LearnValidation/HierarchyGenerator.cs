using Microsoft.TripleCrown.Hierarchy.DataContract.Hierarchy;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using TripleCrownValidation.Models;

namespace TripleCrownValidation
{
    static class HierarchyGenerator
    {
        internal static string HierarchyFileName = "hierarchy.json";

        internal static RawHierarchy GenerateHierarchy(List<IValidateModel> hierarchyItems, string manifestFilePath)
        {
            var hierarchy = new RawHierarchy();
            foreach (var item in hierarchyItems)
            {
                if (item.IsDeleted) continue;

                if (!item.IsValid)
                {
                    hierarchy.InvalidItems.Add(item.Uid);
                    continue;
                }

                if (item is AchievementValidateModel)
                {
                    hierarchy.Achievements.Add(ConvertValidationModelToAchievement(item as AchievementValidateModel));
                    continue;
                }

                hierarchy.Items.Add(ConvertValidationModelToHierarchyItem(item));
            }

            var hierarchyFullFileName = GetHierarchyFullFileName(manifestFilePath);
            File.WriteAllText(hierarchyFullFileName, JsonConvert.SerializeObject(hierarchy));

            return hierarchy;
        }

        internal static string GetHierarchyFullFileName(string manifestFilePath)
        {
            return Path.Combine(Path.GetDirectoryName(manifestFilePath), HierarchyFileName);
        }

        private static RawAchievement ConvertValidationModelToAchievement(AchievementValidateModel achievement)
        {
            return new RawAchievement
            {
                IconUrl = achievement.IconUrl,
                Summary = achievement.Summary,
                Title = achievement.Title,
                Type = achievement.Type,
                Uid = achievement.Uid
            };
        }

        private static RawHierarchyItem ConvertValidationModelToHierarchyItem(IValidateModel validationItem)
        {
            return new RawHierarchyItem
            {
                AssetId = validationItem.AssetId,
                MSDate = validationItem.MSDate,
                PageKind = validationItem.PageKind,
                ServiceData = validationItem.ServiceData,
                UpdatedAt = validationItem.PublishUpdatedAt
            };
        }
    }
}
