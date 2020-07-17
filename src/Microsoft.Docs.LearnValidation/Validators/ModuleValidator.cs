using Microsoft.OpenPublishing.Build.DataContracts.PublishModel;
using Microsoft.OpenPublishing.PluginHelper;
using Microsoft.TripleCrown.DataContract.Common;
using Microsoft.TripleCrown.Hierarchy.DataContract.Hierarchy;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using TripleCrownValidation.Models;

namespace TripleCrownValidation.Validators
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
                    OPSLogger.LogUserError(LogCode.TripleCrown_Module_MetadataError, result, item.SourceRelativePath);
                }

                if (module.Achievement == null)
                {
                    OPSLogger.LogUserError(LogCode.TripleCrown_Module_NoBadgeBind, LogMessageUtility.FormatMessage(LogCode.TripleCrown_Module_NoBadgeBind), item.SourceRelativePath);
                }
                else if (module.Achievement is string achievementUID)
                {
                    if (!fullItemsDict.ContainsKey(achievementUID))
                    {
                        itemValid = false;
                        OPSLogger.LogUserError(LogCode.TripleCrown_Module_BadgeNotFound, LogMessageUtility.FormatMessage(LogCode.TripleCrown_Module_BadgeNotFound, achievementUID), item.SourceRelativePath);
                    }
                    else if (!(fullItemsDict[achievementUID] is AchievementValidateModel achievement) || achievement.Type != AchievementType.Badge)
                    {
                        itemValid = false;
                        OPSLogger.LogUserError(LogCode.TripleCrown_Module_NonSupportedAchievementType, LogMessageUtility.FormatMessage(LogCode.TripleCrown_Module_NonSupportedAchievementType, achievementUID), item.SourceRelativePath);
                    }
                }

                var childrenCantFind = module.Units.Where(u => !fullItemsDict.ContainsKey(u));
                var childrenNotUnit = module.Units.Except(childrenCantFind).Where(u =>
                {
                    if (fullItemsDict[u].Parent != null)
                    {
                        itemValid = false;
                        OPSLogger.LogUserError(LogCode.TripleCrown_Module_MultiParents, LogMessageUtility.FormatMessage(LogCode.TripleCrown_Module_MultiParents, fullItemsDict[u].Uid, fullItemsDict[u].Parent.Uid, module.Uid), item.SourceRelativePath);
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
                    OPSLogger.LogUserError(LogCode.TripleCrown_Module_ChildrenNotFound, LogMessageUtility.FormatMessage(LogCode.TripleCrown_Module_ChildrenNotFound, string.Join(",", childrenCantFind)), item.SourceRelativePath);
                }

                if (childrenNotUnit.Any())
                {
                    itemValid = false;
                    OPSLogger.LogUserError(LogCode.TripleCrown_Module_NonSupportedChildrenType, LogMessageUtility.FormatMessage(LogCode.TripleCrown_Module_NonSupportedChildrenType, string.Join(",", childrenNotUnit.Select(c => c.Uid))), item.SourceRelativePath);
                }

                item.IsValid = itemValid;
                validationResult &= itemValid;
            }

            return validationResult;
        }
    }
}
