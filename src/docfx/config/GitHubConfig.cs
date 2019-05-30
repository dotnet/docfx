// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build
{
    internal sealed class GitHubConfig
    {
        /// <summary>
        /// Token that can be used to access the GitHub API.
        /// </summary>
        public readonly string AuthToken = string.Empty;

        /// <summary>
        /// Determines how long at most a user remains valid in cache.
        /// </summary>
        public readonly int UserCacheExpirationInHours = 30 * 24;

        /// <summary>
        /// Determines whether to resolve git commit user and GitHub user.
        /// </summary>
        public readonly bool ResolveUsers = false;
    }
}
