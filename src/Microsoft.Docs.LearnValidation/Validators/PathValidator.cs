// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.TripleCrown.Hierarchy.DataContract.Common;
using Microsoft.TripleCrown.Hierarchy.DataContract.Hierarchy;
using Newtonsoft.Json;

namespace Microsoft.Docs.LearnValidation
{
    public class PathValidator : ValidatorBase
    {
        public PathValidator(List<LegacyManifestItem> manifestItems, string basePath, LearnValidationLogger logger)
            : base(manifestItems, basePath, logger)
        {
        }

        public override bool Validate(Dictionary<string, IValidateModel> fullItemsDict)
        {
            var validationResult = true;
            foreach (var item in Items)
            {
                var itemValid = true;
                var path = item as PathValidateModel;

                // when trophy is defined in another path, but that path has error when SDP validating
                if (path.Achievement is string achievementUID && !fullItemsDict.ContainsKey(achievementUID))
                {
                    itemValid = false;
                }

                // path has child module, but that module has error when SDP validating
                var childrenCantFind = path.Modules.Where(m => !fullItemsDict.ContainsKey(m)).ToList();
                if (childrenCantFind.Any())
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
            var path = JsonConvert.DeserializeObject<PathValidateModel>(validatorHierarchyItem.ServiceData);
            SetHierarchyData(path, validatorHierarchyItem, manifestItem);
            return path;
        }
    }
}
