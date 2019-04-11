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
        public readonly SourceInfo<string> UserCache = new SourceInfo<string>(string.Empty);

        /// <summary>
        /// Whether upload the updated user cache to remote if it is set to a URL.
        /// </summary>
        public readonly bool UpdateRemoteUserCache = false;

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
