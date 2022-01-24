// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.TripleCrown.Hierarchy.DataContract.Hierarchy;
using Newtonsoft.Json;

namespace Microsoft.Docs.LearnValidation;

internal partial class Validator
{
    private readonly LegacyManifest _manifest;
    private readonly string _outputBasePath;
    private readonly LearnValidationLogger _logger;
    private readonly Func<string, string, bool> _isSharedItem;

    public Validator(string manifestFilePath, LearnValidationLogger logger, Func<string, string, bool> isSharedItem)
    {
        _manifest = JsonConvert.DeserializeObject<LegacyManifest>(File.ReadAllText(manifestFilePath)) ?? new();
        _outputBasePath = Path.GetDirectoryName(manifestFilePath) ?? "";
        _logger = logger;
        _isSharedItem = isSharedItem;
    }

    public (bool, List<IValidateModel>) Validate()
    {
        var moduleFiles = _manifest.Files.Where(item => string.Equals(item.OriginalType, "Module", StringComparison.OrdinalIgnoreCase)).ToList();
        var unitFiles = _manifest.Files.Where(item => string.Equals(item.OriginalType, "ModuleUnit", StringComparison.OrdinalIgnoreCase)).ToList();
        var pathFiles = _manifest.Files.Where(item => string.Equals(item.OriginalType, "LearningPath", StringComparison.OrdinalIgnoreCase)).ToList();
        var achievementFiles = _manifest.Files.Where(item => string.Equals(item.OriginalType, "Achievements", StringComparison.OrdinalIgnoreCase)).ToList();

        var achievementValidator = new AchievementValidator(achievementFiles, _outputBasePath, _logger);
        var unitValidator = new UnitValidator(unitFiles, _outputBasePath, _logger);
        var moduleValidator = new ModuleValidator(moduleFiles, _outputBasePath, _logger);
        var pathValidator = new PathValidator(pathFiles, _outputBasePath, _logger, _isSharedItem);

        // Add badge and trophy (defined in path or module) to achievements
        achievementValidator.Items.AddRange(ExtractAchievementFromModuleOrPath(moduleValidator.Items, true));
        achievementValidator.Items.AddRange(ExtractAchievementFromModuleOrPath(pathValidator.Items, false));

        var validators = new List<ValidatorBase>
            {
                pathValidator,
                moduleValidator,
                unitValidator,
                achievementValidator,
            };

        var hierarchyItems = validators.Where(v => v.Items != null).SelectMany(v => v.Items).Where(i => i.Uid != null).ToList();

        // no duplicated uids
        var itemDict = hierarchyItems.ToDictionary(i => i.Uid, i => i);
        var isValid = true;
        validators.ForEach(v => isValid &= v.Validate(itemDict));
        return (isValid, hierarchyItems);
    }

    private static List<IValidateModel> ExtractAchievementFromModuleOrPath(List<IValidateModel> items, bool isModule)
    {
        var achievements = new List<IValidateModel>();
        foreach (var item in items)
        {
            var achievement = isModule ? (item as ModuleValidateModel)?.Achievement : (item as PathValidateModel)?.Achievement;
            if (achievement != null && achievement is not string)
            {
                var (_, achievementModel) = AchievementSyncModel.ConvertAchievement(achievement);
                if (achievementModel != null)
                {
                    achievements.Add(new AchievementValidateModel
                    {
                        UId = achievementModel.UId,
                        Type = achievementModel.Type,
                        Title = achievementModel.Title,
                        Summary = achievementModel.Summary,
                        IconUrl = achievementModel.IconUrl,
                        SourceRelativePath = item.SourceRelativePath,
                    });
                }
            }
        }

        return achievements;
    }
}
