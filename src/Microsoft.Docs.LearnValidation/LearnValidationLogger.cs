// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Docs.LearnValidation
{
    public class LearnValidationLogger
    {
        /// <summary>
        /// Delegate to write to .errors.log file
        /// </summary>
        private readonly Action<LearnLogItem> _writeLog;
        private readonly HashSet<string> _filesWithError;

        private static IReadOnlyDictionary<LearnErrorCode, string> s_errorMessageMapping = new Dictionary<LearnErrorCode, string>
        {
            {LearnErrorCode.TripleCrown_Module_InvalidChildren, "This module can't publish since child units ({0}) are invalid."},
            {LearnErrorCode.TripleCrown_Unit_InvalidParent, "This unit can't publish since parent module {0} is invalid."},
            {LearnErrorCode.TripleCrown_Module_ChildrenCantFallback, "This module and it's child units will fallback to en-us. Child units ({0}) are invalid or missing and also not exist in en-us repository."},
            {LearnErrorCode.TripleCrown_LearningPath_ChildrenCantFallback, "This learning path will fallback to en-us. Child modules ({0}) are invalid or missing and also not exist in en-us repository."},
            {LearnErrorCode.TripleCrown_DuplicatedUid, "Uid ({0}) is already defined in files {1}"},
            {LearnErrorCode.TripleCrown_Module_NoBadgeBind, "No badge is bind with this module."},
            {LearnErrorCode.TripleCrown_Module_BadgeNotFound, "Achievement ({0}) can't be found."},
            {LearnErrorCode.TripleCrown_Module_NonSupportedAchievementType, "Uid ({0}) is not a Badge."},
            {LearnErrorCode.TripleCrown_Module_MultiParents, "Child ({0}) can not belong to two parents ({1}, {2})."},
            {LearnErrorCode.TripleCrown_Module_NonSupportedChildrenType, "Invalid children: ({0}). Module can only have Units as children."},
            {LearnErrorCode.TripleCrown_Module_ChildrenNotFound, "Children Uid(s): {0} can't be found."},
            {LearnErrorCode.TripleCrown_LearningPath_NoTrophyBind, "No trophy is bind with this learningpath."},
            {LearnErrorCode.TripleCrown_LearningPath_TrophyNotFound, "Achievement ({0}) can't be found."},
            {LearnErrorCode.TripleCrown_LearningPath_NonSupportedAchievementType, "Uid ({0}) is not a Trophy."},
            {LearnErrorCode.TripleCrown_LearningPath_ChildrenNotFound, "Children Uid(s): {0} can't be found."},
            {LearnErrorCode.TripleCrown_LearningPath_NonSupportedChildrenType, "Invalid children: ({0}). LearningPath can only have Modules as children."},
            {LearnErrorCode.TripleCrown_Token_NotFound, "Token ({0}) can't be found in current repository."},
            {LearnErrorCode.TripleCrown_Unit_ContainBothTaskAndQuiz, "Unit ({0}) can't have both Quize and Task Validation."},
            {LearnErrorCode.TripleCrown_Unit_NoModuleParent, "Unit ({0}) must belong to a valid Module."},
            {LearnErrorCode.TripleCrown_Task_NonSupportedType, "The {0}(th) task's azure resource type ({1}) is invalid."},
            {LearnErrorCode.TripleCrown_Task_NonSupportedTypeFormat, "The {0}(th) task's azure resource type ({1}) is invalid. Type must be formatted as '*/*' when name is specified."},
            {LearnErrorCode.TripleCrown_Quiz_MultiAnswers, "The {0}(th) question can only have one correct answer."},
            {LearnErrorCode.TripleCrown_Quiz_NoAnswer, "The {0}(th) question must have one correct answer."},
        };

        public LearnValidationLogger(Action<LearnLogItem> writeLog)
        {
            _writeLog = writeLog;
            _filesWithError = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        public bool HasFileWithError => _filesWithError.Count > 0;

        public bool FileHasError(string file)
        {
            lock (_filesWithError)
            {
                return _filesWithError.Contains(file);
            }
        }

        public void Log(LearnErrorLevel errorLevel, LearnErrorCode errorCode, string file, params object[] message)
        {
            _writeLog?.Invoke(new LearnLogItem(errorLevel, errorCode, FormatMessage(errorCode, message), file));

            if (!string.IsNullOrEmpty(file))
            {
                lock (_filesWithError)
                {
                    _filesWithError.Add(file);
                }
            }
        }

        private string FormatMessage(LearnErrorCode errorCode, object[] message)
        {
            if (s_errorMessageMapping.TryGetValue(errorCode, out var formatString))
            {
                return string.Format(formatString, message ?? Array.Empty<object>());
            }
            else if (message != null && message[0] is string str)
            {
                return str;
            }
            return "";
        }
    }

    public class LearnLogItem
    {
        public readonly LearnErrorLevel ErrorLevel;
        public readonly LearnErrorCode ErrorCode;
        public readonly string Message;
        public readonly string File;
        public LearnLogItem(LearnErrorLevel errorLevel, LearnErrorCode errorCode, string message, string file)
        {
            ErrorLevel = errorLevel;
            ErrorCode = errorCode;
            Message = message;
            File = file;
        }
    }

    public enum LearnErrorLevel
    {
        Warning,
        Error,
    }

    public enum LearnErrorCode
    {
        TripleCrown_Achievement_MetadataError,
        TripleCrown_DrySyncError,
        TripleCrown_DuplicatedUid,
        TripleCrown_LearningPath_ChildrenCantFallback,
        TripleCrown_LearningPath_ChildrenNotFound,
        TripleCrown_LearningPath_MetadataError,
        TripleCrown_LearningPath_NonSupportedAchievementType,
        TripleCrown_LearningPath_NonSupportedChildrenType,
        TripleCrown_LearningPath_NoTrophyBind,
        TripleCrown_LearningPath_TrophyNotFound,
        TripleCrown_Module_BadgeNotFound,
        TripleCrown_Module_ChildrenCantFallback,
        TripleCrown_Module_ChildrenNotFound,
        TripleCrown_Module_InvalidChildren,
        TripleCrown_Module_MetadataError,
        TripleCrown_Module_MultiParents,
        TripleCrown_Module_NoBadgeBind,
        TripleCrown_Module_NonSupportedAchievementType,
        TripleCrown_Module_NonSupportedChildrenType,
        TripleCrown_Quiz_MultiAnswers,
        TripleCrown_Quiz_NoAnswer,
        TripleCrown_Task_NonSupportedType,
        TripleCrown_Task_NonSupportedTypeFormat,
        TripleCrown_Token_NotFound,
        TripleCrown_Unit_ContainBothTaskAndQuiz,
        TripleCrown_Unit_InvalidParent,
        TripleCrown_Unit_MetadataError,
        TripleCrown_Unit_NoModuleParent,
    }
}
