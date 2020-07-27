// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Docs.LearnValidation
{
    public class InvalidFilesProvider
    {
        private readonly List<IValidateModel> _hierarchyItems;
        private readonly LearnValidationHelper _learnValidationHelper;
        private readonly LearnValidationLogger _logger;

        public InvalidFilesProvider(List<IValidateModel> hierarchyItems, LearnValidationHelper learnValidationHelper, LearnValidationLogger logger)
        {
            _hierarchyItems = hierarchyItems;
            _learnValidationHelper = learnValidationHelper;
            _logger = logger;
        }

        public HashSet<string> GetFilesToDelete()
        {
            var uidMapping = _hierarchyItems.Where(h => !(h is AchievementValidateModel)).GroupBy(h => h.Uid).ToDictionary(key => key.Key, value => value.First());
            var modules = _hierarchyItems.Where(hi => hi is ModuleValidateModel).Select(hi => hi as ModuleValidateModel);
            var learningpaths = _hierarchyItems.Where(hi => hi is PathValidateModel).Select(hi => hi as PathValidateModel);
            
            List<string> invalidFiles = new List<string>();

            // Mark modules
            foreach (var module in modules)
            {
                if (!module.IsValid || module.Units.Any(u => !uidMapping[u].IsValid))
                {
                    if(module.IsValid)
                    {
                        module.IsValid = false;
                        var invalidUnits = module.Units.Where(u => !uidMapping[u].IsValid);
                        _logger.Log(LearnErrorLevel.Error, LearnErrorCode.TripleCrown_Module_InvalidChildren, string.Join(",", invalidUnits), module.SourceRelativePath);
                    }

                    foreach(var unitUid in module.Units.Where(u => uidMapping.ContainsKey(u) && uidMapping[u].IsValid))
                    {
                        var unit = uidMapping[unitUid];
                        unit.IsValid = false;
                        _logger.Log(LearnErrorLevel.Error, LearnErrorCode.TripleCrown_Unit_InvalidParent, module.Uid, unit.SourceRelativePath);
                    }
                }

                var unitsNeedCheck = module.Units.Where(u => !uidMapping.ContainsKey(u) || !uidMapping[u].IsValid).ToList();
                var unitCantFallback = unitsNeedCheck.Where(u => !_learnValidationHelper.IsUnit(u)).ToList();

                if(unitCantFallback.Any())
                {
                    module.IsDeleted = true;
                    _logger.Log(LearnErrorLevel.Error, LearnErrorCode.TripleCrown_Module_ChildrenCantFallback, string.Join(", ", unitCantFallback), module.SourceRelativePath);
                    invalidFiles.Add(module.SourceRelativePath);
                    foreach (var unitUid in module.Units.Where(u => uidMapping.ContainsKey(u)))
                    {
                        var unit = uidMapping[unitUid];
                        unit.IsDeleted = true;
                        invalidFiles.Add(unit.SourceRelativePath);
                    }
                }
            }

            // Mark learningpath
            foreach (var learningpath in learningpaths)
            {
                var modulesNeedCheck = learningpath.Modules.Where(m => !uidMapping.ContainsKey(m) || !uidMapping[m].IsValid).ToList();
                var moduleCantFallback = modulesNeedCheck.Where(m => !_learnValidationHelper.IsModule(m)).ToList();
                if (moduleCantFallback.Any())
                {
                    learningpath.IsValid = false;
                    learningpath.IsDeleted = true;
                    _logger.Log(LearnErrorLevel.Error, LearnErrorCode.TripleCrown_LearningPath_ChildrenCantFallback, string.Join(", ", moduleCantFallback), learningpath.SourceRelativePath);
                    invalidFiles.Add(learningpath.SourceRelativePath);
                }
            }

            return invalidFiles.ToHashSet();
        }

    }
}
