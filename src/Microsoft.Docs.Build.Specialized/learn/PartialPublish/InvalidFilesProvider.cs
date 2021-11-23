// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.LearnValidation;

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
        var uidMapping = _hierarchyItems.Where(h => h is not AchievementValidateModel).GroupBy(h => h.Uid).ToDictionary(key => key.Key, value => value.First());
        var modules = _hierarchyItems.Where(hi => hi is ModuleValidateModel).Select(hi => hi as ModuleValidateModel);
        var learningpaths = _hierarchyItems.Where(hi => hi is PathValidateModel).Select(hi => hi as PathValidateModel);

        var invalidFiles = new List<string>();

        // Mark modules
        foreach (var module in modules)
        {
            if (module is null)
            {
                continue;
            }

            if (!module.IsValid || module.Units.Any(u => !uidMapping[u].IsValid))
            {
                if (module.IsValid)
                {
                    module.IsValid = false;
                }

                foreach (var unitUid in module.Units.Where(u => uidMapping.ContainsKey(u) && uidMapping[u].IsValid))
                {
                    var unit = uidMapping[unitUid];
                    unit.IsValid = false;
                }
            }

            var unitsNeedCheck = module.Units.Where(u => !uidMapping.ContainsKey(u) || !uidMapping[u].IsValid).ToList();
            var unitCantFallback = unitsNeedCheck.Where(u => !_learnValidationHelper.IsUnit(u)).ToList();

            if (unitCantFallback.Any())
            {
                module.IsDeleted = true;
                _logger.Log(LearnErrorLevel.Error, LearnErrorCode.TripleCrown_Module_ChildrenCantFallback, file: module.SourceRelativePath, string.Join(", ", unitCantFallback));
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
            if (learningpath is null)
            {
                continue;
            }

            var modulesNeedCheck = learningpath.Modules.Where(m => !uidMapping.ContainsKey(m) || !uidMapping[m].IsValid).ToList();
            var moduleCantFallback = modulesNeedCheck.Where(m => !_learnValidationHelper.IsModule(m)).ToList();
            if (moduleCantFallback.Any())
            {
                learningpath.IsValid = false;
                learningpath.IsDeleted = true;
                _logger.Log(LearnErrorLevel.Error, LearnErrorCode.TripleCrown_LearningPath_ChildrenCantFallback, file: learningpath.SourceRelativePath, string.Join(", ", moduleCantFallback));
                invalidFiles.Add(learningpath.SourceRelativePath);
            }
        }

        return invalidFiles.ToHashSet();
    }
}
