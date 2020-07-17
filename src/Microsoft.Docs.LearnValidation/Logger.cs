// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;

namespace Microsoft.Docs.LearnValidation
{
    public static class Logger
    {
        public static ConcurrentBag<LogItem> LogItems { get; } = new ConcurrentBag<LogItem>();

        /// <summary>
        /// Delegate to write to .errors.log file
        /// </summary>
        public static Action<LogItem> WriteLog;

        public static void Log(ErrorLevel level, ErrorCode code, string message = "", string filePath = null)
        {
            var logItem = new LogItem();
            WriteLog?.Invoke(logItem);
            // Todo: implement error log
        }
    }

    public class LogItem
    {
        public ErrorLevel ErrorLevel;
        public ErrorCode LogCode;
        public string File;
    }

    public enum ErrorLevel
    {
        Warning,
        Error,
    }

    public enum ErrorCode
    {
        TripleCrown_Achievement_MetadataError,
        TripleCrown_DependencyFile_NotExist,
        TripleCrown_DocsetFolder_IsNull,
        TripleCrown_DrySyncError,
        TripleCrown_DuplicatedUid,
        TripleCrown_InternalError,
        TripleCrown_LearningPath_ChildrenCantFallback,
        TripleCrown_LearningPath_ChildrenNotFound,
        TripleCrown_LearningPath_DebugMode_ChildrenNotFound,
        TripleCrown_LearningPath_MetadataError,
        TripleCrown_LearningPath_NonSupportedAchievementType,
        TripleCrown_LearningPath_NonSupportedChildrenType,
        TripleCrown_LearningPath_NoTrophyBind,
        TripleCrown_LearningPath_TrophyNotFound,
        TripleCrown_ManifestFile_NotExist,
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
        TripleCrown_RepoRootPath_IsNull,
        TripleCrown_Task_NonSupportedType,
        TripleCrown_Task_NonSupportedTypeFormat,
        TripleCrown_Token_NotFound,
        TripleCrown_Unit_ContainBothTaskAndQuiz,
        TripleCrown_Unit_InvalidParent,
        TripleCrown_Unit_MetadataError,
        TripleCrown_Unit_NoModuleParent,
    }
}
