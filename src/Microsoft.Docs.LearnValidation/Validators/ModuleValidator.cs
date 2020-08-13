// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.TripleCrown.Hierarchy.DataContract.Common;
using Microsoft.TripleCrown.Hierarchy.DataContract.Hierarchy;
using Newtonsoft.Json;

namespace Microsoft.Docs.LearnValidation
{
    public class ModuleValidator : ValidatorBase
    {
        public ModuleValidator(List<LegacyManifestItem> manifestItems, string basePath, LearnValidationLogger logger)
            : base(manifestItems, basePath, logger)
        {
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
                    Logger.Log(LearnErrorLevel.Error, LearnErrorCode.TripleCrown_Module_MetadataError, file: item.SourceRelativePath, result);
                }

                if (module.Achievement == null)
                {
                    Logger.Log(LearnErrorLevel.Error, LearnErrorCode.TripleCrown_Module_NoBadgeBind, file: item.SourceRelativePath);
                }
                else if (module.Achievement is string achievementUID)
                {
                    if (!fullItemsDict.ContainsKey(achievementUID))
                    {
                        itemValid = false;
                        Logger.Log(LearnErrorLevel.Error, LearnErrorCode.TripleCrown_Module_BadgeNotFound, file: item.SourceRelativePath, achievementUID);
                    }
                    else if (!(fullItemsDict[achievementUID] is AchievementValidateModel achievement) || achievement.Type != AchievementType.Badge)
                    {
                        itemValid = false;
                        Logger.Log(LearnErrorLevel.Error, LearnErrorCode.TripleCrown_Module_NonSupportedAchievementType, file: item.SourceRelativePath, achievementUID);
                    }
                }

                var childrenCantFind = module.Units.Where(u => !fullItemsDict.ContainsKey(u));
                var childrenNotUnit = module.Units.Except(childrenCantFind).Where(u =>
                {
                    if (fullItemsDict[u].Parent != null)
                    {
                        itemValid = false;
                        Logger.Log(LearnErrorLevel.Error, LearnErrorCode.TripleCrown_Module_MultiParents, file: item.SourceRelativePath, fullItemsDict[u].Uid, fullItemsDict[u].Parent?.Uid, module.Uid);
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
                    Logger.Log(LearnErrorLevel.Error, LearnErrorCode.TripleCrown_Module_ChildrenNotFound, file: item.SourceRelativePath, string.Join(",", childrenCantFind));
                }

                if (childrenNotUnit.Any())
                {
                    itemValid = false;
                    Logger.Log(LearnErrorLevel.Error, LearnErrorCode.TripleCrown_Module_NonSupportedChildrenType, file: item.SourceRelativePath, string.Join(",", childrenNotUnit.Select(c => c.Uid)));
                }

                item.IsValid = itemValid;
                validationResult &= itemValid;
            }

            return validationResult;
        }

        protected override HierarchyItem GetHierarchyItem(ValidatorHierarchyItem validatorHierarchyItem, LegacyManifestItem manifestItem)
        {
            var module = JsonConvert.DeserializeObject<ModuleValidateModel>(validatorHierarchyItem.ServiceData);
            SetHierarchyData(module, validatorHierarchyItem, manifestItem);
            return module;
        }
    }
}
