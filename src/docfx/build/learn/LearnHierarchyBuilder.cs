// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Microsoft.Docs.Validation;

namespace Microsoft.Docs.Build;

internal class LearnHierarchyBuilder
{
    private readonly ContentValidator _contentValidator;

    private readonly ListBuilder<LearningPath> _learningPaths = new();
    private readonly ListBuilder<Module> _modules = new();
    private readonly ListBuilder<ModuleUnit> _moduleUnits = new();
    private readonly ListBuilder<Achievement> _achievements = new();

    public LearnHierarchyBuilder(ContentValidator contentValidator)
    {
        _contentValidator = contentValidator;
    }

    public void ValidateHierarchy()
    {
        _contentValidator.ValidateHierarchy(GetAllLearnHierarchyModels().ToList());
    }

    public void AddLearningPath(LearningPath path)
    {
        _learningPaths.Add(path);
    }

    public void AddModule(Module module)
    {
        _modules.Add(module);
    }

    public void AddModuleUnit(ModuleUnit moduleUnit)
    {
        _moduleUnits.Add(moduleUnit);
    }

    public void AddAchievements(AchievementArray achievements)
    {
        if (achievements.Achievements?.Length > 0)
        {
            _achievements.AddRange(achievements.Achievements);
        }
    }

    private IEnumerable<HierarchyNode> GetAllLearnHierarchyModels()
    {
        var pathModels = _learningPaths.AsList().Select(p => new HierarchyNode
        {
            Uid = p.Uid.Value,
            ChildrenUids = p.Modules?.Select(m => m.Value).ToList(),
            SourceInfo = p.Uid.Source,
            PageType = "learningpath",
        }).ToList();

        var moduleModels = _modules.AsList().Select(p => new HierarchyNode
        {
            Uid = p.Uid.Value,
            ChildrenUids = p.Units?.Select(u => u.Value).ToList(),
            SourceInfo = p.Uid.Source,
            PageType = "module",
        }).ToList();

        var unitModels = _moduleUnits.AsList().Select(p => new HierarchyNode
        {
            Uid = p.Uid.Value,
            UseAzureSandbox = p.AzureSandbox,
            SourceInfo = p.Uid.Source,
            PageType = "moduleunit",
        }).ToList();

        var achievementModels = _achievements.AsList().Select(p => new HierarchyNode
        {
            Uid = p.Uid.Value,
            SourceInfo = p.Uid.Source,
            PageType = p.Type.ToString(),
        }).ToList();

        var trophys = _learningPaths.AsList().Where(p => p.Trophy != null).Select(p => new HierarchyNode
        {
            Uid = p.Trophy!.Uid.Value,
            SourceInfo = p.Trophy.Uid.Source,
            PageType = "trophy",
        }).ToList();

        var badges = _modules.AsList().Where(p => p.Badge != null).Select(p => new HierarchyNode
        {
            Uid = p.Badge!.Uid.Value,
            SourceInfo = p.Badge.Uid.Source,
            PageType = "badge",
        }).ToList();

        return pathModels.Concat(moduleModels).Concat(unitModels).Concat(achievementModels).Concat(trophys).Concat(badges);
    }
}
