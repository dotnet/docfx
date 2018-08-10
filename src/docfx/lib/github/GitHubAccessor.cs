// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Octokit;

namespace Microsoft.Docs.Build
{
    internal class GitHubAccessor
    {
        private readonly GitHubClient _client;

        private bool _isRateLimitExceeded = false;

        public GitHubAccessor(string gitToken = null)
        {
            _client = new GitHubClient(new ProductHeaderValue("DocFXv3"));
            if (!string.IsNullOrEmpty(gitToken))
                _client.Credentials = new Credentials(gitToken);
        }

        /// <summary>
        /// Get user profile by user name from GitHub API
        /// </summary>
        /// <exception cref="DocfxException">Thrown when user doesn't exist or GitHub rate limit exceeded</exception>
        public async Task<UserProfile> GetUserProfileByName(string name)
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

        /// <summary>
        /// Get author by repo and commit from GitHub API
        /// </summary>
        /// <exception cref="DocfxException">Thrown when repo or commit doesn't exist, or GitHub rate limit exceeded</exception>"
        /// <returns> The commit author's name on GitHub </returns>
        public async Task<string> GetNameByCommit(string repoOwner, string repoName, string commitSha)
        {
            Debug.Assert(!string.IsNullOrEmpty(repoOwner));
            Debug.Assert(!string.IsNullOrEmpty(repoName));
            Debug.Assert(!string.IsNullOrEmpty(commitSha));

            GitHubCommit githubCommit;
            Author author;
            var errors = new List<Error>();
            if (_isRateLimitExceeded)
                throw Errors.ExceedGitHubRateLimit().ToException();

            try
            {
                githubCommit = await _client.Repository.Commit.Get(repoOwner, repoName, commitSha);
                author = githubCommit.Author;
            }
            catch (RateLimitExceededException)
            {
                _isRateLimitExceeded = true;
                throw Errors.ExceedGitHubRateLimit().ToException();
            }
            catch (Exception)
            {
                // catch NotFoundException if owner/repo doesn't exist
                // catch ApiValidationException if no commit found for SHA
                throw Errors.GitHubCommitNotFound().ToException();
            }

            return author.Login;
        }

        private UserProfile ToUserProfile(User user)
        {
            if (user == null)
                return null;

            return new UserProfile()
            {
                ProfileUrl = user.HtmlUrl,
                DisplayName = user.Name,
                Name = user.Login,
                Id = user.Id.ToString(),

                // EmailAddress (public) can only be obtained with OAuth token (not required to be current user's token)
                EmailAddress = user.Email,

                // UserEmails can only be obtained with current user's OAuth token, so we only get the user's public email
                UserEmails = user.Email,
            };
        }
    }
}
