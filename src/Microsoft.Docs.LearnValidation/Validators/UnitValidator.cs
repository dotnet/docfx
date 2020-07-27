// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.TripleCrown.Hierarchy.DataContract.Hierarchy;
using Microsoft.TripleCrown.Hierarchy.DataContract.TaskValidation;
using Microsoft.TripleCrown.Hierarchy.DataContract.Quiz;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Microsoft.Docs.LearnValidation
{
    public class UnitValidator : ValidatorBase
    {
        private HashSet<string> _taskValidationTypeSet;

        public UnitValidator(List<LegacyManifestItem> manifestItems, string basePath, LearnValidationLogger logger)
              : base(manifestItems, basePath, logger)
        {
            _taskValidationTypeSet = GetTaskValidationTypeSet();
        }

        protected override HierarchyItem GetHierarchyItem(ValidatorHierarchyItem validatorHierarchyItem, LegacyManifestItem manifestItem)
        {
            var unit = JsonConvert.DeserializeObject<UnitValidateModel>(validatorHierarchyItem.ServiceData);
            SetHierarchyData(unit, validatorHierarchyItem, manifestItem);
            return unit;
        }

        public override bool Validate(Dictionary<string, IValidateModel> fullItemsDict)
        {
            var validationResult = true;
            foreach (var item in Items)
            {
                var itemValid = true;
                var unit = item as UnitValidateModel;
                var result = unit.ValidateMetadata();
                if (!string.IsNullOrEmpty(result))
                {
                    itemValid = false;
                    Logger.Log(LearnErrorLevel.Error, LearnErrorCode.TripleCrown_Unit_MetadataError, result, item.SourceRelativePath);
                }

                if (unit.Tasks != null && unit.QuizAnswers != null)
                {
                    itemValid = false;
                    Logger.Log(LearnErrorLevel.Error, LearnErrorCode.TripleCrown_Unit_ContainBothTaskAndQuiz, unit.UId, item.SourceRelativePath);
                }

                if (unit.Parent == null || !(unit.Parent is ModuleValidateModel))
                {
                    itemValid = false;
                    Logger.Log(LearnErrorLevel.Error, LearnErrorCode.TripleCrown_Unit_NoModuleParent, unit.UId, item.SourceRelativePath);
                }

                itemValid &= ValidateQuiz(unit.QuizAnswers, item);
                itemValid &= ValidateTaskValidation(unit.Tasks, item);

                item.IsValid = itemValid;
                validationResult &= itemValid;
            }

            return validationResult;
        }

        private bool ValidateTaskValidation(ValidationTask[] tasks, IValidateModel unit)
        {
            if (tasks == null) return true;

            var validateResult = true;
            for (int index = 0; index < tasks.Length; index++)
            {
                var task = tasks[index];
                if (task.Azure == null) continue;

                var azureResource = task.Azure.ToAzureResource();
                if (azureResource == null) continue;

                if (!_taskValidationTypeSet.Contains(azureResource.Type, StringComparer.OrdinalIgnoreCase))
                {
                    validateResult = false;
                    Logger.Log(LearnErrorLevel.Error, LearnErrorCode.TripleCrown_Task_NonSupportedType, $"{azureResource.Type} at index: {index}", unit.SourceRelativePath);
                }

                if (string.IsNullOrEmpty(azureResource.Name) && azureResource.Type.Count(t => t == '/') != 1)
                {
                    validateResult = false;
                    Logger.Log(LearnErrorLevel.Error, LearnErrorCode.TripleCrown_Task_NonSupportedTypeFormat, $"{azureResource.Type} at index: {index}", unit.SourceRelativePath);
                }
            }

            return validateResult;
        }

        private bool ValidateQuiz(QuestionWithAnswer[] quizAnswers, IValidateModel unit)
        {
            if (quizAnswers == null) return true;

            var validateResult = true;
            for (int index = 0; index < quizAnswers.Count(); index++)
            {
                var answer = quizAnswers[index];
                var answerCount = answer.Choices.Count(c => c.IsCorrect);

                if (answerCount > 1)
                {
                    validateResult = false;
                    Logger.Log(LearnErrorLevel.Error, LearnErrorCode.TripleCrown_Quiz_MultiAnswers, $"error index: {index}", unit.SourceRelativePath);
                }

                if (answerCount < 1)
                {
                    validateResult = false;
                    Logger.Log(LearnErrorLevel.Error, LearnErrorCode.TripleCrown_Quiz_NoAnswer, $"error index: {index}", unit.SourceRelativePath);
                }
            }

            return validateResult;
        }

        private HashSet<string> GetTaskValidationTypeSet()
        {
            var taskValidationTypeFile = Path.Combine(AppContext.BaseDirectory, "data/AzureResourceTypes.txt");
            var taskValidationTypeSet = new HashSet<string>();

            if (File.Exists(taskValidationTypeFile))
            {
                taskValidationTypeSet = new HashSet<string>(File.ReadAllLines(taskValidationTypeFile));
            }

            return taskValidationTypeSet;
        }
    }
}
