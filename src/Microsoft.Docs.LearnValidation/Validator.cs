// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.TripleCrown.Hierarchy.DataContract.Hierarchy;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.Docs.LearnValidation
{
    partial class Validator
    {
        private readonly LegacyManifest _manifest;
        private readonly string _outputBasePath;
        private readonly LearnValidationHelper _learnValidationHelper;
        private readonly LearnValidationLogger _logger;

        public Validator(LearnValidationHelper learnValidationHelper, string manifestFilePath, LearnValidationLogger logger)
        {
            _learnValidationHelper = learnValidationHelper;
            _manifest = JsonConvert.DeserializeObject<LegacyManifest>(File.ReadAllText(manifestFilePath));
            _outputBasePath = Path.GetDirectoryName(manifestFilePath);
            _logger = logger;
        }

        public (bool, List<IValidateModel>) Validate()
        {
            var moduleFiles = _manifest.Files.Where(item => string.Equals(item.OriginalType, "Module", StringComparison.OrdinalIgnoreCase)).ToList();
            var unitFiles = _manifest.Files.Where(item => string.Equals(item.OriginalType, "ModuleUnit", StringComparison.OrdinalIgnoreCase)).ToList();
            var pathFiles = _manifest.Files.Where(item => string.Equals(item.OriginalType, "LearningPath", StringComparison.OrdinalIgnoreCase)).ToList();
            var achievementFiles = _manifest.Files.Where(item => string.Equals(item.OriginalType, "Achievements", StringComparison.OrdinalIgnoreCase)).ToList();

            var hierarchyItems = new List<IValidateModel>();
            var achievementValidator = new AchievementValidator(achievementFiles, _outputBasePath, _logger);
            var unitValidator = new UnitValidator(unitFiles, _outputBasePath, _logger);
            var moduleValidator = new ModuleValidator(moduleFiles, _outputBasePath, _logger);
            var pathValidator = new PathValidator(pathFiles, _outputBasePath, _learnValidationHelper, _logger);

            //Add badge and trophy to achievements
            achievementValidator.Items.AddRange(ExtractAchievementFromModuleOrPath(moduleValidator.Items, true));
            achievementValidator.Items.AddRange(ExtractAchievementFromModuleOrPath(pathValidator.Items, false));

            //no duplicated uids
            Dictionary<string, IValidateModel> itemDict = new Dictionary<string, IValidateModel>();

            List<ValidatorBase> validators = new List<ValidatorBase>();
            validators.Add(pathValidator);
            validators.Add(moduleValidator);
            validators.Add(unitValidator);
            validators.Add(achievementValidator);

            var isValid = true;
            hierarchyItems = validators.Where(v => v.Items != null).SelectMany(v => v.Items).ToList();

            var duplicateFiles = hierarchyItems.GroupBy(i => i.Uid).Where(g => g.Count() > 1).SelectMany(g =>
            {
                var files = string.Join(",", g.Select(i => i.SourceRelativePath));
                foreach (var item in g)
                {
                    _logger.Log(LearnErrorLevel.Error, LearnErrorCode.TripleCrown_DuplicatedUid, file: item.SourceRelativePath, item.Uid, files);
                }
                isValid = false;
                return g.Select(gu => gu.SourceRelativePath);
            }).ToArray();

            foreach (var validator in validators)
            {
                if (validator.Items != null)
                {
                    validator.Items.RemoveAll(i => duplicateFiles.Contains(i.SourceRelativePath));
                    validator.Items.ForEach(i => itemDict.Add(i.Uid, i));
                }
            }

            validators.ForEach(v => isValid &= v.Validate(itemDict));
            return (isValid, hierarchyItems);
        }

        private List<IValidateModel> ExtractAchievementFromModuleOrPath(List<IValidateModel> items, bool isModule)
        {
            List<IValidateModel> achievements = new List<IValidateModel>();
            foreach (var item in items)
            {
                var achievement = isModule ? (item as ModuleValidateModel).Achievement : (item as PathValidateModel).Achievement;
                if (achievement != null && !(achievement is string))
                {
                    var (achievementUid, achievementModel) = AchievementSyncModel.ConvertAchievement(achievement);
                    if (achievementModel != null)
                    {
                        achievements.Add(new AchievementValidateModel
                        {
                            UId = achievementModel.UId,
                            Type = achievementModel.Type,
                            Title = achievementModel.Title,
                            Summary = achievementModel.Summary,
                            IconUrl = achievementModel.IconUrl, 
                            SourceRelativePath = item.SourceRelativePath
                        });
                    }
                }
            }

            return achievements;
        }
    }
}
