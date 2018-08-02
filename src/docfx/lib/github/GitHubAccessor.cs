// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Octokit;

namespace Microsoft.Docs.Build
{
    internal class GitHubAccessor
    {
        private readonly GitHubClient _client = new GitHubClient(new ProductHeaderValue("DocFXv3"));
        private readonly object _syncRoot = new object();
        private bool _isRateLimitExceeded = false;

        public async Task<(List<Error> errors, UserProfile profile)> GetUserProfileByName(string name)
        {
            Debug.Assert(!string.IsNullOrEmpty(name));

            var errors = new List<Error>();
            if (_isRateLimitExceeded)
                return (errors, null);

            User user;
            try
            {
                user = await _client.User.Get(name);
            }
            catch (RateLimitExceededException)
            {
                return (ExceedRateLimit(), null);
            }

            return (errors, ToUserProfile(user));
        }

        private List<Error> ExceedRateLimit()
        {
            if (!_isRateLimitExceeded)
            {
                lock (_syncRoot)
                {
                    if (!_isRateLimitExceeded)
                    {
                        _isRateLimitExceeded = true;
                        return new List<Error> { Errors.ExceedRateLimit() };
                    }
                }
            }
            return new List<Error>();
        }

        private UserProfile ToUserProfile(User user)
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
