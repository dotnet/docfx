using Microsoft.OpenPublishing.Build.DataContracts.PublishModel;
using Microsoft.OpenPublishing.PluginHelper;
using Microsoft.TripleCrown.Hierarchy.DataContract.Hierarchy;
using Microsoft.TripleCrown.DataContract.Quiz;
using Microsoft.TripleCrown.DataContract.TaskValidation;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TripleCrownValidation.Models;

namespace TripleCrownValidation.Validators
{
    public class UnitValidator : ValidatorBase
    {
        private HashSet<string> _taskValidationTypeSet;

        public UnitValidator(List<LegacyManifestItem> manifestItems, string basePath)
              : base(manifestItems, basePath)
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
                    OPSLogger.LogUserError(LogCode.TripleCrown_Unit_MetadataError, result, item.SourceRelativePath);
                }

                if (unit.Tasks != null && unit.QuizAnswers != null)
                {
                    itemValid = false;
                    OPSLogger.LogUserError(LogCode.TripleCrown_Unit_ContainBothTaskAndQuiz, LogMessageUtility.FormatMessage(LogCode.TripleCrown_Unit_ContainBothTaskAndQuiz, unit.UId), item.SourceRelativePath);
                }

                if (unit.Parent == null || !(unit.Parent is ModuleValidateModel))
                {
                    itemValid = false;
                    OPSLogger.LogUserError(LogCode.TripleCrown_Unit_NoModuleParent, LogMessageUtility.FormatMessage(LogCode.TripleCrown_Unit_NoModuleParent, unit.UId), item.SourceRelativePath);
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
                    OPSLogger.LogUserError(LogCode.TripleCrown_Task_NonSupportedType, LogMessageUtility.FormatMessage(LogCode.TripleCrown_Task_NonSupportedType, index + 1, azureResource.Type), unit.SourceRelativePath);
                }

                if (string.IsNullOrEmpty(azureResource.Name) && azureResource.Type.Count(t => t == '/') != 1)
                {
                    validateResult = false;
                    OPSLogger.LogUserError(LogCode.TripleCrown_Task_NonSupportedTypeFormat, LogMessageUtility.FormatMessage(LogCode.TripleCrown_Task_NonSupportedTypeFormat, index + 1, azureResource.Type), unit.SourceRelativePath);
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
                    OPSLogger.LogUserError(LogCode.TripleCrown_Quiz_MultiAnswers, LogMessageUtility.FormatMessage(LogCode.TripleCrown_Quiz_MultiAnswers, index + 1), unit.SourceRelativePath);
                }

                if (answerCount < 1)
                {
                    validateResult = false;
                    OPSLogger.LogUserError(LogCode.TripleCrown_Quiz_NoAnswer, LogMessageUtility.FormatMessage(LogCode.TripleCrown_Quiz_NoAnswer, index + 1), unit.SourceRelativePath);
                }
            }

            return validateResult;
        }

        private HashSet<string> GetTaskValidationTypeSet()
        {
            var executingFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var taskValidationTypeFile = Path.Combine(executingFolder, "TaskValidationTypes.txt");
            var taskValidationTypeSet = new HashSet<string>();

            if (File.Exists(taskValidationTypeFile))
            {
                taskValidationTypeSet = new HashSet<string>(File.ReadAllLines(taskValidationTypeFile));
            }

            return taskValidationTypeSet;
        }
    }
}
