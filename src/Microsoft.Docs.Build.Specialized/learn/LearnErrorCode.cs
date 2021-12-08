// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.LearnValidation;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Error Code")]
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
