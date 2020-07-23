// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Docs.LearnValidation
{
    public class PartialPublishProcessor
    {
        private List<IValidateModel> _hierarchyItems;
        private string _docsetPath;
        private LearnValidationHelper _learnValidationHelper;

        public PartialPublishProcessor(List<IValidateModel> hierarchyItems, string docsetPath, LearnValidationHelper learnValidationHelper)
        {
            _hierarchyItems = hierarchyItems;
            _docsetPath = docsetPath;
            _learnValidationHelper = learnValidationHelper;
        }

        public void MarkInvalidHierarchyItem()
        {
            var uidMapping = _hierarchyItems.Where(h => !(h is AchievementValidateModel)).GroupBy(h => h.Uid).ToDictionary(key => key.Key, value => value.First());
            var modules = _hierarchyItems.Where(hi => hi is ModuleValidateModel).Select(hi => hi as ModuleValidateModel);
            var learningpaths = _hierarchyItems.Where(hi => hi is PathValidateModel).Select(hi => hi as PathValidateModel);
            
            List<string> skipPublishFilePathList = new List<string>();

            // Mark modules
            foreach (var module in modules)
            {
                if (!module.IsValid || module.Units.Any(u => !uidMapping[u].IsValid))
                {
                    if(module.IsValid)
                    {
                        module.IsValid = false;
                        var invalidUnits = module.Units.Where(u => !uidMapping[u].IsValid);
                        LearnValidationLogger.Log(LearnErrorLevel.Error, LearnErrorCode.TripleCrown_Module_InvalidChildren, string.Join(",", invalidUnits), module.SourceRelativePath);
                    }

                    foreach(var unitUid in module.Units.Where(u => uidMapping.ContainsKey(u) && uidMapping[u].IsValid))
                    {
                        var unit = uidMapping[unitUid];
                        unit.IsValid = false;
                        LearnValidationLogger.Log(LearnErrorLevel.Error, LearnErrorCode.TripleCrown_Unit_InvalidParent, module.Uid, unit.SourceRelativePath);
                    }
                }

                var unitsNeedCheck = module.Units.Where(u => !uidMapping.ContainsKey(u) || !uidMapping[u].IsValid).ToList();
                var unitCantFallback = unitsNeedCheck.Where(u => !_learnValidationHelper.IsUnit(u)).ToList();

                if(unitCantFallback.Any())
                {
                    module.IsDeleted = true;
                    LearnValidationLogger.Log(LearnErrorLevel.Error, LearnErrorCode.TripleCrown_Module_ChildrenCantFallback, string.Join(", ", unitCantFallback), module.SourceRelativePath);
                    // TODO: remove invalid module from publish.json
                    skipPublishFilePathList.Add(module.SourceRelativePath);
                    foreach (var unitUid in module.Units.Where(u => uidMapping.ContainsKey(u)))
                    {
                        var unit = uidMapping[unitUid];
                        unit.IsDeleted = true;
                        // TODO: remove invalid units from publish.json
                        skipPublishFilePathList.Add(unit.SourceRelativePath);
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
                    LearnValidationLogger.Log(LearnErrorLevel.Error, LearnErrorCode.TripleCrown_LearningPath_ChildrenCantFallback, string.Join(", ", moduleCantFallback), learningpath.SourceRelativePath);
                    // TODO: remove invalid path from publish.json
                    skipPublishFilePathList.Add(learningpath.SourceRelativePath);
                }
            }
        }

        // TODO: use other ways rather than skip-publish-file.json to control delete from DHS, for skip-publish-file.json is not supported in v3
        //File.WriteAllText(_skipPublishFilePath, JsonConvert.SerializeObject(skipPublishFilePathList, Formatting.Indented));
    }
}
