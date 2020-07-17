// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.TripleCrown.Hierarchy.DataContract.Hierarchy;
using Microsoft.TripleCrown.Hierarchy.DataContract.Common;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Docs.LearnValidation
{
    public class ModuleValidator : ValidatorBase
    {
        public ModuleValidator(List<LegacyManifestItem> manifestItems, string basePath)
            :base(manifestItems, basePath)
        {
        }

        protected override HierarchyItem GetHierarchyItem(ValidatorHierarchyItem validatorHierarchyItem, LegacyManifestItem manifestItem)
        {
            var module = JsonConvert.DeserializeObject<ModuleValidateModel>(validatorHierarchyItem.ServiceData);
            SetHierarchyData(module, validatorHierarchyItem, manifestItem);
            return module;
        }

        public override bool Validate(Dictionary<string, IValidateModel> fullItemsDict)
        {
            var validationResult = true;
            foreach (var item in Items)
            {
                var itemValid = true;
                var module = item as ModuleValidateModel;
                var result = module.ValidateMetadata();
                if (!string.IsNullOrEmpty(result))
                {
                    itemValid = false;
                    Logger.Log(ErrorLevel.Error, ErrorCode.TripleCrown_Module_MetadataError, result, item.SourceRelativePath);
                }

                if (module.Achievement == null)
                {
                    Logger.Log(ErrorLevel.Error, ErrorCode.TripleCrown_Module_NoBadgeBind, filePath: item.SourceRelativePath);
                }
                else if (module.Achievement is string achievementUID)
                {
                    if (!fullItemsDict.ContainsKey(achievementUID))
                    {
                        itemValid = false;
                        Logger.Log(ErrorLevel.Error, ErrorCode.TripleCrown_Module_BadgeNotFound, achievementUID, item.SourceRelativePath);
                    }
                    else if (!(fullItemsDict[achievementUID] is AchievementValidateModel achievement) || achievement.Type != AchievementType.Badge)
                    {
                        itemValid = false;
                        Logger.Log(ErrorLevel.Error, ErrorCode.TripleCrown_Module_NonSupportedAchievementType, achievementUID, item.SourceRelativePath);
                    }
                }

                var childrenCantFind = module.Units.Where(u => !fullItemsDict.ContainsKey(u));
                var childrenNotUnit = module.Units.Except(childrenCantFind).Where(u =>
                {
                    if (fullItemsDict[u].Parent != null)
                    {
                        itemValid = false;
                        Logger.Log(ErrorLevel.Error, ErrorCode.TripleCrown_Module_MultiParents, $"{fullItemsDict[u].Uid}, {fullItemsDict[u].Parent?.Uid}, {module.Uid}", item.SourceRelativePath);
                    }
                    else
                    {
                        fullItemsDict[u].Parent = module;
                    }
                    return !(fullItemsDict[u] is UnitValidateModel);
                })
                .Select(c => fullItemsDict[c]).ToList();

                if (childrenCantFind.Any())
                {
                    itemValid = false;
                    Logger.Log(ErrorLevel.Error, ErrorCode.TripleCrown_Module_ChildrenNotFound, string.Join(",", childrenCantFind), item.SourceRelativePath);
                }

                if (childrenNotUnit.Any())
                {
                    itemValid = false;
                    Logger.Log(ErrorLevel.Error, ErrorCode.TripleCrown_Module_NonSupportedChildrenType, string.Join(",", childrenNotUnit.Select(c => c.Uid)), item.SourceRelativePath);
                }

                item.IsValid = itemValid;
                validationResult &= itemValid;
            }

            return validationResult;
        }
    }
}
