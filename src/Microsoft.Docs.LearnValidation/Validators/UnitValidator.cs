// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.TripleCrown.Hierarchy.DataContract.Hierarchy;
using Newtonsoft.Json;

namespace Microsoft.Docs.LearnValidation
{
    public class UnitValidator : ValidatorBase
    {
        private readonly HashSet<string> _taskValidationTypeSet;

        public UnitValidator(List<LegacyManifestItem> manifestItems, string basePath, LearnValidationLogger logger)
              : base(manifestItems, basePath, logger)
        {
            _taskValidationTypeSet = GetTaskValidationTypeSet();
        }

        public override bool Validate(Dictionary<string, IValidateModel> fullItemsDict)
        {
            var validationResult = true;
            foreach (var item in Items)
            {
                var itemValid = true;
                var unit = item as UnitValidateModel;

                // unit has parent, but that module has error when SDP validating
                if (unit.Parent == null)
                {
                    itemValid = false;
                    Logger.Log(LearnErrorLevel.Error, LearnErrorCode.TripleCrown_Unit_NoModuleParent, file: item.SourceRelativePath, unit.UId);
                }

                item.IsValid = itemValid;
                validationResult &= itemValid;
            }

            return validationResult;
        }

        protected override HierarchyItem GetHierarchyItem(ValidatorHierarchyItem validatorHierarchyItem, LegacyManifestItem manifestItem)
        {
            var unit = JsonConvert.DeserializeObject<UnitValidateModel>(validatorHierarchyItem.ServiceData);
            SetHierarchyData(unit, validatorHierarchyItem, manifestItem);
            return unit;
        }

        private static HashSet<string> GetTaskValidationTypeSet()
        {
            var taskValidationTypeFile = Path.Combine(AppContext.BaseDirectory, "data/AzureResourceTypes.txt");
            var taskValidationTypeSet = new HashSet<string>();

            if (File.Exists(taskValidationTypeFile))
            {
                taskValidationTypeSet = new HashSet<string>(File.ReadAllLines(taskValidationTypeFile));
            }

            return taskValidationTypeSet;
        }
    }
}
