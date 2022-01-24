// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.TripleCrown.Hierarchy.DataContract.Hierarchy;
using Newtonsoft.Json;

namespace Microsoft.Docs.LearnValidation;

public class UnitValidator : ValidatorBase
{
    public UnitValidator(List<LegacyManifestItem> manifestItems, string basePath, LearnValidationLogger logger)
          : base(manifestItems, basePath, logger)
    {
    }

    public override bool Validate(Dictionary<string, IValidateModel> fullItemsDict)
    {
        var validationResult = true;
        foreach (var item in Items)
        {
            var itemValid = true;
            var unit = item as UnitValidateModel;

            // unit has parent, but that module has error when SDP validating
            if (unit?.Parent == null)
            {
                itemValid = false;
            }

            item.IsValid = itemValid;
            validationResult &= itemValid;
        }

        return validationResult;
    }

    protected override HierarchyItem GetHierarchyItem(ValidatorHierarchyItem validatorHierarchyItem, LegacyManifestItem manifestItem)
    {
        var unit = JsonConvert.DeserializeObject<UnitValidateModel>(validatorHierarchyItem.ServiceData) ?? new();
        SetHierarchyData(unit, validatorHierarchyItem, manifestItem);
        return unit;
    }
}
