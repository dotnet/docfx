// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build
{
    internal class ContributionConfig
    {
        /// <summary>
        /// Determine whether to show links for contribution.
        /// </summary>
        public readonly bool Enabled = true;

        /// <summary>
        /// Specify the which repository edit link goes to.
        /// User the current repo if not set.
        /// </summary>
        public readonly string Repository = string.Empty;

        /// <summary>
        /// Specify the which branch edit link goes to.
        /// User the current branch if not set.
        /// </summary>
        public readonly string Branch = string.Empty;

        /// <summary>
        /// The path of user profile cache, used for generating authoer and contributors.
        /// </summary>
        public readonly string UserProfileCachePath = string.Empty;

        /// <summary>
        /// The path of commit time history file, which contains the time each commit being pushed.
        /// </summary>
        public readonly string GitCommitsHistoryPath = string.Empty;
    }
}
