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

        private static bool _isRateLimitExceeded = false;

        /// <summary>
        /// Get user profile by user name from GitHub API
        /// </summary>
        /// <exception cref="DocfxException">Thrown when user doesn't exist or GitHub rate limit exceeded</exception>
        public static async Task<UserProfile> GetUserProfileByName(string name)
        {
            Debug.Assert(!string.IsNullOrEmpty(name));

            var errors = new List<Error>();
            if (_isRateLimitExceeded)
                throw Errors.ExceedGitHubRateLimit().ToException();

            User user;
            try
            {
                user = await _client.User.Get(name);
            }
            catch (RateLimitExceededException)
            {
                _isRateLimitExceeded = true;
                throw Errors.ExceedGitHubRateLimit().ToException();
            }
            catch (NotFoundException)
            {
                // GitHub will return 404 "Not Found" if the user doesn't exist
                throw Errors.GitHubUserNotFound().ToException();
            }

            return ToUserProfile(user);
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
