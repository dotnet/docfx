// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.TripleCrown.Hierarchy.DataContract.Hierarchy;
using Newtonsoft.Json;

namespace Microsoft.Docs.LearnValidation;

internal static class HierarchyGenerator
{
    private const string HierarchyFileName = "hierarchy.json";

    internal static RawHierarchy GenerateHierarchy(List<IValidateModel> hierarchyItems, string docsetOutputPath)
    {
        var hierarchy = new RawHierarchy();
        foreach (var item in hierarchyItems)
        {
            if (item.IsDeleted)
            {
                continue;
            }

            if (!item.IsValid)
            {
                hierarchy.InvalidItems.Add(item.Uid);
                continue;
            }

            if (item is AchievementValidateModel achievement && achievement != null)
            {
                hierarchy.Achievements.Add(ConvertValidationModelToAchievement(achievement));
                continue;
            }

            hierarchy.Items.Add(ConvertValidationModelToHierarchyItem(item));
        }

        var hierarchyFullFileName = Path.Combine(docsetOutputPath, HierarchyFileName);
        File.WriteAllText(hierarchyFullFileName, JsonConvert.SerializeObject(hierarchy));

        return hierarchy;
    }

    private static RawAchievement ConvertValidationModelToAchievement(AchievementValidateModel achievement)
    {
        return new RawAchievement
        {
            IconUrl = achievement.IconUrl,
            Summary = achievement.Summary,
            Title = achievement.Title,
            Type = achievement.Type,
            Uid = achievement.Uid,
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
            UpdatedAt = validationItem.PublishUpdatedAt,
        };
    }
}
