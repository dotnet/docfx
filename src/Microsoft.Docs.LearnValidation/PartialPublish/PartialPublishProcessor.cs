using Microsoft.OpenPublishing.PluginHelper;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TripleCrownValidation.DependencyMap;
using TripleCrownValidation.Models;
using TripleCrownValidation.TripleCrown;

namespace TripleCrownValidation.PartialPublish
{
    public class PartialPublishProcessor
    {
        private List<IValidateModel> _hierarchyItems;
        private string _docsetFolder;
        private string _repoRootPath;
        private string _skipPublishFilePath;
        private TripleCrownHelper _tripleCrownHelper;

        public PartialPublishProcessor(List<IValidateModel> hierarchyItems, CommandLineOptions opt)
        {
            _hierarchyItems = hierarchyItems;
            _docsetFolder = opt.DocsetFolder;
            _skipPublishFilePath = opt.SkipPublishFilePath;
            _repoRootPath = opt.RepoRootPath;
            _tripleCrownHelper = new TripleCrownHelper(opt.TripleCrownEndpoint, opt.Branch);
        }

        public void MarkInvalidHierarchyItem()
        {
            var uidMapping = _hierarchyItems.Where(h => !(h is AchievementValidateModel)).GroupBy(h => h.Uid).ToDictionary(key => key.Key, value => value.First());
            var modules = _hierarchyItems.Where(hi => hi is ModuleValidateModel).Select(hi => hi as ModuleValidateModel);
            var learningpaths = _hierarchyItems.Where(hi => hi is PathValidateModel).Select(hi => hi as PathValidateModel);
            
            List<string> skipPublishFilePathList = new List<string>();
            if (File.Exists(_skipPublishFilePath))
            {
                skipPublishFilePathList = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(_skipPublishFilePath));
            }

            // Mark modules
            foreach (var module in modules)
            {
                if (!module.IsValid || module.Units.Any(u => !uidMapping[u].IsValid))
                {
                    if(module.IsValid)
                    {
                        module.IsValid = false;
                        var invalidUnits = module.Units.Where(u => !uidMapping[u].IsValid);
                        OPSLogger.LogUserError(LogCode.TripleCrown_Module_InvalidChildren, LogMessageUtility.FormatMessage(LogCode.TripleCrown_Module_InvalidChildren, string.Join(",", invalidUnits)), module.SourceRelativePath);
                    }

                    foreach(var unitUid in module.Units.Where(u => uidMapping.ContainsKey(u) && uidMapping[u].IsValid))
                    {
                        var unit = uidMapping[unitUid];
                        unit.IsValid = false;
                        OPSLogger.LogUserError(LogCode.TripleCrown_Unit_InvalidParent, LogMessageUtility.FormatMessage(LogCode.TripleCrown_Unit_InvalidParent, module.Uid), unit.SourceRelativePath);
                    }
                }

                var unitsNeedCheck = module.Units.Where(u => !uidMapping.ContainsKey(u) || !uidMapping[u].IsValid).ToList();
                var unitCantFallback = unitsNeedCheck.Where(u => !_tripleCrownHelper.IsUnit(u)).ToList();

                if(unitCantFallback.Any())
                {
                    module.IsDeleted = true;
                    OPSLogger.LogUserError(LogCode.TripleCrown_Module_ChildrenCantFallback, LogMessageUtility.FormatMessage(LogCode.TripleCrown_Module_ChildrenCantFallback, string.Join(", ", unitCantFallback)), module.SourceRelativePath);
                    skipPublishFilePathList.Add(ValidationHelper.GetSkipPublishFilePath(_docsetFolder, _repoRootPath, module.SourceRelativePath));
                    foreach(var unitUid in module.Units.Where(u => uidMapping.ContainsKey(u)))
                    {
                        var unit = uidMapping[unitUid];
                        unit.IsDeleted = true;
                        skipPublishFilePathList.Add(ValidationHelper.GetSkipPublishFilePath(_docsetFolder, _repoRootPath, unit.SourceRelativePath));
                    }
                }
            }

            // Mark learningpath
            foreach (var learningpath in learningpaths)
            {
                var modulesNeedCheck = learningpath.Modules.Where(m => !uidMapping.ContainsKey(m) || !uidMapping[m].IsValid).ToList();
                var moduleCantFallback = modulesNeedCheck.Where(m => !_tripleCrownHelper.IsModule(m)).ToList();
                if (moduleCantFallback.Any())
                {
                    learningpath.IsValid = false;
                    learningpath.IsDeleted = true;
                    OPSLogger.LogUserError(LogCode.TripleCrown_LearningPath_ChildrenCantFallback, LogMessageUtility.FormatMessage(LogCode.TripleCrown_LearningPath_ChildrenCantFallback, string.Join(", ", moduleCantFallback)), learningpath.SourceRelativePath);
                    skipPublishFilePathList.Add(ValidationHelper.GetSkipPublishFilePath(_docsetFolder, _repoRootPath, learningpath.SourceRelativePath));
                }
            }

            File.WriteAllText(_skipPublishFilePath, JsonConvert.SerializeObject(skipPublishFilePathList, Formatting.Indented));
        }
    }
}
