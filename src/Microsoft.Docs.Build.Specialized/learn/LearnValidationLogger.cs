// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.LearnValidation;

public class LearnValidationLogger
{
    /// <summary>
    /// Delegate to write to .errors.log file
    /// </summary>
    private readonly Action<LearnLogItem> _writeLog;
    private readonly HashSet<string> _filesWithError;

    private static readonly Dictionary<LearnErrorCode, string> s_errorMessageMapping = new()
    {
        { LearnErrorCode.TripleCrown_Module_InvalidChildren, "This module can't publish since child units ({0}) are invalid." },
        { LearnErrorCode.TripleCrown_Unit_InvalidParent, "This unit can't publish since parent module {0} is invalid." },
        { LearnErrorCode.TripleCrown_Module_ChildrenCantFallback, "This module and it's child units will fallback to en-us. Child units ({0}) are invalid or missing and also not exist in en-us repository." },
        { LearnErrorCode.TripleCrown_LearningPath_ChildrenCantFallback, "This learning path will fallback to en-us. Child modules ({0}) are invalid or missing and also not exist in en-us repository." },
        { LearnErrorCode.TripleCrown_DuplicatedUid, "Uid ({0}) is already defined in files {1}" },
        { LearnErrorCode.TripleCrown_Module_NoBadgeBind, "No badge is bind with this module." },
        { LearnErrorCode.TripleCrown_Module_BadgeNotFound, "Achievement ({0}) can't be found." },
        { LearnErrorCode.TripleCrown_Module_NonSupportedAchievementType, "Uid ({0}) is not a Badge." },
        { LearnErrorCode.TripleCrown_Module_MultiParents, "Child ({0}) can not belong to two parents ({1}, {2})." },
        { LearnErrorCode.TripleCrown_Module_NonSupportedChildrenType, "Invalid children: ({0}). Module can only have Units as children." },
        { LearnErrorCode.TripleCrown_Module_ChildrenNotFound, "Children Uid(s): {0} can't be found." },
        { LearnErrorCode.TripleCrown_LearningPath_NoTrophyBind, "No trophy is bind with this learningpath." },
        { LearnErrorCode.TripleCrown_LearningPath_TrophyNotFound, "Achievement ({0}) can't be found." },
        { LearnErrorCode.TripleCrown_LearningPath_NonSupportedAchievementType, "Uid ({0}) is not a Trophy." },
        { LearnErrorCode.TripleCrown_LearningPath_ChildrenNotFound, "Children Uid(s): {0} can't be found." },
        { LearnErrorCode.TripleCrown_LearningPath_NonSupportedChildrenType, "Invalid children: ({0}). LearningPath can only have Modules as children." },
        { LearnErrorCode.TripleCrown_Token_NotFound, "Token ({0}) can't be found in current repository." },
        { LearnErrorCode.TripleCrown_Unit_ContainBothTaskAndQuiz, "Unit ({0}) can't have both Quiz and Task Validation." },
        { LearnErrorCode.TripleCrown_Unit_NoModuleParent, "Unit ({0}) must belong to a valid Module." },
        { LearnErrorCode.TripleCrown_Task_NonSupportedType, "The {0}(th) task's azure resource type ({1}) is invalid." },
        { LearnErrorCode.TripleCrown_Task_NonSupportedTypeFormat, "The {0}(th) task's azure resource type ({1}) is invalid. Type must be formatted as '*/*' when name is specified." },
        { LearnErrorCode.TripleCrown_Quiz_MultiAnswers, "The {0}(th) question can only have one correct answer." },
        { LearnErrorCode.TripleCrown_Quiz_NoAnswer, "The {0}(th) question must have one correct answer." },
        { LearnErrorCode.TripleCrown_Achievement_MetadataError, "{0}" },
        { LearnErrorCode.TripleCrown_DrySyncError, "{0}" },
        { LearnErrorCode.TripleCrown_LearningPath_MetadataError, "{0}" },
        { LearnErrorCode.TripleCrown_Module_MetadataError, "{0}" },
        { LearnErrorCode.TripleCrown_Unit_MetadataError, "{0}" },
    };

    public LearnValidationLogger(Action<LearnLogItem> writeLog)
    {
        _writeLog = writeLog;
        _filesWithError = new(StringComparer.OrdinalIgnoreCase);
    }

    public bool HasFileWithError => _filesWithError.Count > 0;

    public bool FileHasError(string file)
    {
        lock (_filesWithError)
        {
            return _filesWithError.Contains(file);
        }
    }

    public void Log(LearnErrorLevel errorLevel, LearnErrorCode errorCode, string file, params object[] messageArgs)
    {
        _writeLog?.Invoke(new LearnLogItem(errorLevel, errorCode, string.Format(s_errorMessageMapping[errorCode], messageArgs), file));

        if (!string.IsNullOrEmpty(file))
        {
            lock (_filesWithError)
            {
                _filesWithError.Add(file);
            }
        }
    }
}
