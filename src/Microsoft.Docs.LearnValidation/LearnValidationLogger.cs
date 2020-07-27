// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Docs.LearnValidation
{
    public class LearnValidationLogger
    {
        /// <summary>
        /// Delegate to write to .errors.log file
        /// </summary>
        private readonly Action<LearnLogItem> _writeLog;
        private readonly HashSet<string> _filesWithError;

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

        public void Log(LearnErrorLevel errorLevel, LearnErrorCode errorCode, string message = "", string file = null)
        {
            _writeLog?.Invoke(new LearnLogItem(errorLevel, errorCode, message, file));

            if (!string.IsNullOrEmpty(file))
            {
                lock (_filesWithError)
                {
                    _filesWithError.Add(file);
                }
            }
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
        TripleCrown_InternalError,
        TripleCrown_LearningPath_ChildrenCantFallback,
        TripleCrown_LearningPath_ChildrenNotFound,
        TripleCrown_LearningPath_MetadataError,
        TripleCrown_LearningPath_NonSupportedAchievementType,
        TripleCrown_LearningPath_NonSupportedChildrenType,
        TripleCrown_LearningPath_NoTrophyBind,
        TripleCrown_LearningPath_TrophyNotFound,
        TripleCrown_ManifestFile_UpdateFailed,
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
        TripleCrown_Unimplemented,
    }
}
