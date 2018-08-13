// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build
{
    internal static class Exceptions
    {
        public static DocfxInternalException ExceedGitHubRateLimit()
            => new DocfxInternalException("GitHub API rate limit exceeded");

        public static DocfxInternalException GitHubUserNotFound(string user)
            => new DocfxInternalException($"User '{user}' not found on GitHub");

        public static DocfxInternalException GitHubCommitNotFound(string sha)
            => new DocfxInternalException($"Commit '{sha}' not found on GitHub");
    }
}
