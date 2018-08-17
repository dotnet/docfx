// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Docs.Build
{
    internal class ContributionConfig
    {
        /// <summary>
        /// Determine whether to show contributors and update time based on commits.
        /// </summary>
        public readonly bool ShowContributors = true;

        /// <summary>
        /// Determine whether to show edit button for contribution.
        /// </summary>
        public readonly bool ShowEdit = true;

        /// <summary>
        /// Specify the repository for contribution. For GitHub, it is `account/name`.
        /// Fallback to git origin if not set.
        /// </summary>
        public readonly string Repository;

        /// <summary>
        /// Specify the which branch edit link goes to. Fallback to the current branch if not set.
        /// </summary>
        public readonly string Branch;

        /// <summary>
        /// The address of user profile cache, used for generating authoer and contributors.
        /// It should be an absolute url or a relative path
        /// </summary>
        public readonly string UserProfileCache = string.Empty;

        /// <summary>
        /// The address of commit time history file, which contains the time each commit being pushed.
        /// It should be an absolute url or a relative path
        /// </summary>
        public readonly string GitCommitsTime = string.Empty;

        /// <summary>
        /// The excluded contributors which you do want to show
        /// </summary>
        public readonly string[] ExcludedContributors = Array.Empty<string>();
    }
}
