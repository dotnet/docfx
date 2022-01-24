// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build;

internal enum DependencyType
{
    /// <summary>
    /// Reference another file using link
    /// </summary>
    File,

    /// <summary>
    /// Reference another file using uid
    /// </summary>
    Uid,

    /// <summary>
    /// Include another file.
    /// </summary>
    Include,

    /// <summary>
    /// To generate the correct preview link for TOC files in PR comment.
    /// </summary>
    Metadata,

    /// <summary>
    /// Learn specific uid dependency:
    /// LearningPath -> Module
    /// Module -> ModuleUnit
    /// </summary>
    Hierarchy,

    /// <summary>
    /// Learn specific uid dependency:
    /// LearningPath|Module -> Achievement
    /// </summary>
    Achievement,
}
