// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.TripleCrown.Hierarchy.DataContract.Hierarchy;
using Newtonsoft.Json;

namespace Microsoft.Docs.LearnValidation;

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

            // when badge is defined in another module, but that module has error when SDP validating
            if (module?.Achievement is string achievementUID && !fullItemsDict.ContainsKey(achievementUID))
            {
                itemValid = false;
            }

            // module has child unit, but that unit has error when SDP validating
            var childrenCantFind = module?.Units.Where(u => !fullItemsDict.ContainsKey(u));
            if (childrenCantFind != null && childrenCantFind.Any())
            {
                itemValid = false;
            }

            var validUnits = childrenCantFind is null ? module?.Units : module?.Units.Except(childrenCantFind);
            if (validUnits != null && module != null)
            {
                foreach (var unit in validUnits)
                {
                    fullItemsDict[unit].Parent = module;
                }
            }

            item.IsValid = itemValid;
            validationResult &= itemValid;
        }

        return validationResult;
    }

    protected override HierarchyItem GetHierarchyItem(ValidatorHierarchyItem validatorHierarchyItem, LegacyManifestItem manifestItem)
    {
        var module = JsonConvert.DeserializeObject<ModuleValidateModel>(validatorHierarchyItem.ServiceData) ?? new();
        SetHierarchyData(module, validatorHierarchyItem, manifestItem);
        return module;
    }
}
