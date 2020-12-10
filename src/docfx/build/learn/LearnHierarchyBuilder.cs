// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class LearnHierarchyBuilder
    {
        private readonly ErrorBuilder _errors;
        private readonly ContentValidator _contentValidator;

        private readonly ListBuilder<LearningPath> _learningPaths = new ListBuilder<LearningPath>();
        private readonly ListBuilder<Module> _modules = new ListBuilder<Module>();
        private readonly ListBuilder<ModuleUnit> _moduleUnits = new ListBuilder<ModuleUnit>();
        private readonly ListBuilder<Achievement> _achievements = new ListBuilder<Achievement>();

        public LearnHierarchyBuilder(ErrorBuilder errors, ContentValidator contentValidator)
        {
            _errors = errors;
            _contentValidator = contentValidator;
        }

        public void ValidateHierarchy()
        {
        }

        public void AddLearningPath(JObject content)
        {
            var path = JsonUtility.ToObject<LearningPath>(_errors, content);
            _learningPaths.Add(path);

            if (path.Trophy != null && GetAchievement(path.Trophy, out var achievement) && achievement != null)
            {
                _achievements.Add(achievement);
            }
        }

        public void AddModule(JObject content)
        {
            var module = JsonUtility.ToObject<Module>(_errors, content);
            _modules.Add(module);

            if (module.Badge != null && GetAchievement(module.Badge, out var achievement) && achievement != null)
            {
                _achievements.Add(achievement);
            }
        }

        public void AddModuleUnit(JObject content)
        {
            _moduleUnits.Add(JsonUtility.ToObject<ModuleUnit>(_errors, content));
        }

        public void AddAchievements(JObject content)
        {
            var achievements = JsonUtility.ToObject<AchievementArray>(_errors, content).Achievements;
            if (achievements?.Length > 0)
            {
                _achievements.AddRange(achievements);
            }
        }

        private bool GetAchievement(object obj, out Achievement? achievement)
        {
            switch (obj)
            {
                case TrophyModel trophy:
                    achievement = new Achievement { Uid = trophy.Uid, Type = Constants.Trophy };
                    return true;

                case BadgeModel badge:
                    achievement = new Achievement { Uid = badge.Uid, Type = Constants.Badge };
                    return true;

                default:
                    achievement = null;
                    return false;
            }
        }
    }
}
