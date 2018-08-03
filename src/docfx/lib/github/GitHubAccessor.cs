// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Octokit;

namespace Microsoft.Docs.Build
{
    internal static class GitHubAccessor
    {
        private static readonly GitHubClient _client = new GitHubClient(new ProductHeaderValue("DocFXv3"));

        // 0 for false, 1 for true.
        private static int _isRateLimitExceeded = 0;

        /// <summary>
        /// Get user profile by user name from GitHub API
        /// </summary>
        /// <exception cref="DocfxException">Thrown when user doesn't exist or GitHub rate limit exceeded</exception>
        public static async Task<UserProfile> GetUserProfileByName(string name)
        {
            Debug.Assert(!string.IsNullOrEmpty(name));

            var errors = new List<Error>();
            if (IsRateLimitExceeded())
                throw Errors.ExceedGitHubRateLimit().ToException();

            User user;
            try
            {
                user = await _client.User.Get(name);
            }
            catch (RateLimitExceededException)
            {
                ExceedRateLimit();
                throw Errors.ExceedGitHubRateLimit().ToException();
            }
            catch (NotFoundException)
            {
                // GitHub will return 404 "Not Found" if the user doesn't exist
                throw Errors.GitHubUserNotFound().ToException();
            }

            return ToUserProfile(user);
        }

        private static bool IsRateLimitExceeded() => _isRateLimitExceeded == 1;

        private static List<Error> ExceedRateLimit()
        {
            if (!IsRateLimitExceeded())
            {
                if (Interlocked.Exchange(ref _isRateLimitExceeded, 1) == 0)
                    return new List<Error> { Errors.ExceedGitHubRateLimit() };
            }
            return new List<Error>();
        }

        private static UserProfile ToUserProfile(User user)
        {
            if (user == null)
                return null;

            // Can't get emails for now. Will try getting if after passing the token.
            return new UserProfile()
            {
                ProfileUrl = user.HtmlUrl,
                DisplayName = user.Name,
                Name = user.Login,
                Id = user.Id.ToString(),
            };
        }
    }
}
