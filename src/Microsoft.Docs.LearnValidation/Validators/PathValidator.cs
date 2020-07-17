// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.TripleCrown.Hierarchy.DataContract.Hierarchy;
using Microsoft.TripleCrown.Hierarchy.DataContract.Common;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Docs.LearnValidation
{
    public class PathValidator : ValidatorBase
    {
        private XRefHelper XrefHelper { get; set; }
        private LearnValidationHelper TripleCrownHelper { get; set; }
        private string Locale { get; set; }

        public PathValidator(List<LegacyManifestItem> manifestItems, string basePath, XRefHelper xrefHelper, LearnValidationHelper tripleCrownHelper, string locale)
            : base(manifestItems, basePath)
        {
            XrefHelper = xrefHelper;
            TripleCrownHelper = tripleCrownHelper;
            Locale = locale;
        }

        protected override HierarchyItem GetHierarchyItem(ValidatorHierarchyItem validatorHierarchyItem, LegacyManifestItem manifestItem)
        {
            var path = JsonConvert.DeserializeObject<PathValidateModel>(validatorHierarchyItem.ServiceData);
            SetHierarchyData(path, validatorHierarchyItem, manifestItem);
            return path;
        }

        public override bool Validate(Dictionary<string, IValidateModel> fullItemsDict)
        {
            var validationResult = true;
            foreach (var item in Items)
            {
                var itemValid = true;
                var path = item as PathValidateModel;
                var result = path.ValidateMetadata();
                if (!string.IsNullOrEmpty(result))
                {
                    itemValid = false;
                    Logger.Log(ErrorLevel.Error, ErrorCode.TripleCrown_LearningPath_MetadataError, result, item.SourceRelativePath);
                }

                if (path.Achievement == null)
                {
                    Logger.Log(ErrorLevel.Error, ErrorCode.TripleCrown_LearningPath_NoTrophyBind, filePath: item.SourceRelativePath);
                }
                else if (path.Achievement is string achievementUID)
                {
                    if (!fullItemsDict.ContainsKey(achievementUID))
                    {
                        itemValid = false;
                        Logger.Log(ErrorLevel.Error, ErrorCode.TripleCrown_LearningPath_TrophyNotFound, achievementUID, item.SourceRelativePath);
                    }
                    else if (!(fullItemsDict[achievementUID] is AchievementValidateModel achievement) || achievement.Type != AchievementType.Trophy)
                    {
                        itemValid = false;
                        Logger.Log(ErrorLevel.Error, ErrorCode.TripleCrown_LearningPath_NonSupportedAchievementType, achievementUID, item.SourceRelativePath);
                    }
                }

                var childrenCantFind = path.Modules.Where(m => !fullItemsDict.ContainsKey(m) && !TripleCrownHelper.IsModule(m)).ToList();

                var childrenNotModule = path.Modules.Except(childrenCantFind).Where(m =>
                {
                    if (!fullItemsDict.ContainsKey(m)) return false;

                    fullItemsDict[m].Parent = path;
                    return !(fullItemsDict[m] is ModuleValidateModel);
                }).ToList();

                if (childrenCantFind.Any())
                {
                    if (XrefHelper != null)
                    {
                        itemValid = false;
                        Logger.Log(ErrorLevel.Error, ErrorCode.TripleCrown_LearningPath_ChildrenNotFound, string.Join(",", childrenCantFind), item.SourceRelativePath);
                    }
                    else
                    {
                        Logger.Log(ErrorLevel.Warning, ErrorCode.TripleCrown_LearningPath_DebugMode_ChildrenNotFound, string.Join(",", childrenCantFind), item.SourceRelativePath);
                    }
                }

                if (childrenNotModule.Any())
                {
                    itemValid = false;
                    Logger.Log(ErrorLevel.Error, ErrorCode.TripleCrown_LearningPath_NonSupportedChildrenType, string.Join(",", childrenNotModule),
                        item.SourceRelativePath);
                }

                item.IsValid = itemValid;
                validationResult &= itemValid;
            }

            return validationResult;
        }
    }
}
