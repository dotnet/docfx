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
        /// The address of user profile cache, used for generating authoer and contributors.
        /// It should be an absolute url or a relative path.
        /// </summary>
        public readonly string UserCache = string.Empty;

        /// <summary>
        /// Determines how long a user remains valid in cache.
        /// </summary>
        public readonly int UserCacheExpirationInHours = 7 * 24;
    }
}
