// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

namespace Microsoft.Docs.Build
{
    internal sealed class GitHubConfig
    {
        /// <summary>
        /// Token that can be used to access the GitHub API.
        /// </summary>
        public string AuthToken { get; } = string.Empty;

        /// <summary>
        /// Determines how long at most a user remains valid in cache.
        /// </summary>
        public int UserCacheExpirationInHours { get; } = 30 * 24;

        /// <summary>
        /// Determines whether to resolve git commit user and GitHub user.
        /// We only resolve github user when an <see cref="AuthToken"/> is provided.
        /// </summary>
        public bool ResolveUsers { get; } = true;
    }
}
