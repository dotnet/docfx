// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.TripleCrown.Hierarchy.DataContract.Hierarchy;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TripleCrownValidation
{
    public class DependencyMapProcessor
    {
        private List<IValidateModel> _hierarchyItems;
        private string _docsetFolder;
        private string _dependencyMapFile;

        public DependencyMapProcessor(string dependencyMapFile, List<IValidateModel> hierarchyItems, string docsetFolder)
        {
            _dependencyMapFile = dependencyMapFile;
            _hierarchyItems = hierarchyItems;
            _docsetFolder = docsetFolder;
        }

        public void UpdateDependencyMap()
        {
            var dependencyItems = DependencyMapHelper.LoadDependentFileInfo(_dependencyMapFile);

            //backup original file
            File.WriteAllText(_dependencyMapFile + ".bak", File.ReadAllText(_dependencyMapFile));

            // Update Dependency Type
            var uidToFileMapping = _hierarchyItems.ToDictionary(key => key.Uid, value => Path.Combine(_docsetFolder, value.SourceRelativePath).BackSlashToForwardSlash());
            var dependencyMapping = dependencyItems.Where(item => item.DependencyType == "uid").GroupBy(item => item.FromFilePath)
                .ToDictionary(key => key.Key, value => value.GroupBy(v => v.ToFilePath).ToDictionary(vk => vk.Key, vv => vv.First()));
            
            var learningPaths = _hierarchyItems.Where(item => item is PathValidateModel).ToList();
            ProcessDependencyType(learningPaths, uidToFileMapping, dependencyMapping, false);
            
            var modules = _hierarchyItems.Where(item => item is ModuleValidateModel).ToList();
            ProcessDependencyType(modules, uidToFileMapping, dependencyMapping, true);

            // Remove Fragments
            dependencyItems.RemoveAll(di => di.DependencyType == "include" && di.ToFilePath == di.FromFilePath + ".md");

            //backup changed file
            File.WriteAllLines(_dependencyMapFile + ".updated", dependencyItems.Select(di => JsonConvert.SerializeObject(di)));
            File.WriteAllLines(_dependencyMapFile, dependencyItems.Select(di => JsonConvert.SerializeObject(di)));
        }

        private void ProcessDependencyType(List<IValidateModel> hierarchyItems, Dictionary<string, string> uidToFileMapping, Dictionary<string, Dictionary<string, DependencyItem>> dependencyMapping, bool isModule)
        {
            hierarchyItems.ForEach(item =>
            {
                var fromFilePath = uidToFileMapping.ContainsKey(item.Uid)? uidToFileMapping[item.Uid] : null;
                if (fromFilePath == null) return;
                var children = isModule ? (item as ModuleValidateModel).Units : (item as PathValidateModel).Modules;

                if (children != null && dependencyMapping.ContainsKey(fromFilePath))
                {
                    var toFilePaths = dependencyMapping[fromFilePath];

                    children.ForEach(child =>
                    {
                        var toFilePath = uidToFileMapping.ContainsKey(child)? uidToFileMapping[child]: null;
                        if (toFilePath == null) return;
                        if (toFilePaths.ContainsKey(toFilePath))
                        {
                            toFilePaths[toFilePath].DependencyType = "hierarchy";
                        }
                    });

                    var achievement = isModule ? (item as ModuleValidateModel).Achievement : (item as PathValidateModel).Achievement;
                    var (achievementUid, model) = AchievementSyncModel.ConvertAchievement(achievement);
                    var achievementToFilePath = uidToFileMapping.ContainsKey(achievementUid) ? uidToFileMapping[achievementUid] : null;
                    if (achievementToFilePath == null) return;

                    if (toFilePaths.ContainsKey(achievementToFilePath))
                    {
                        toFilePaths[achievementToFilePath].DependencyType = "achievement";
                    }
                }
            });
        }

        
    }
}
