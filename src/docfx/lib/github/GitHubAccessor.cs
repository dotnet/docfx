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
        private const string _rateLimitExceededMessage = "GitHub API rate limit exceeded";

        private readonly GitHubClient _client;
        private volatile bool _isRateLimitExceeded = false;

        public GitHubAccessor(string gitToken = null)
        {
            _client = new GitHubClient(new ProductHeaderValue("DocFXv3"));
            if (!string.IsNullOrEmpty(gitToken))
                _client.Credentials = new Credentials(gitToken);
        }

        /// <summary>
        /// Get user profile by user name from GitHub API
        /// </summary>
        public async Task<(List<Error> errors, UserProfile profile)> GetUserProfileByName(string name)
        {
            Debug.Assert(!string.IsNullOrEmpty(name));

            if (_isRateLimitExceeded)
                return (new List<Error> { Errors.ResolveAuthorFailed(name, _rateLimitExceededMessage) }, null);

            User user;
            try
            {
                user = await _client.User.Get(name);
            }
            catch (RateLimitExceededException)
            {
                _isRateLimitExceeded = true;
                return (new List<Error> { Errors.ResolveAuthorFailed(name, _rateLimitExceededMessage) }, null);
            }
            catch (NotFoundException)
            {
                // GitHub will return 404 "Not Found" if the user doesn't exist
                return (new List<Error> { Errors.AuthorNotFound(name) }, null);
            }
            catch (OperationCanceledException)
            {
                // To unblock the e2e test
                // Todo: better handle the operation canceled exception, @Renze
                return (new List<Error> { Errors.ResolveAuthorFailed(name, "Operation cancelled when calling GitHub") }, null);
            }

            return (new List<Error>(), ToUserProfile(user));
        }

        /// <summary>
        /// Get author by repo and commit from GitHub API
        /// </summary>
        /// <exception cref="DocfxException">Thrown when repo or commit doesn't exist, or GitHub rate limit exceeded</exception>"
        /// <returns> The commit author's name on GitHub </returns>
        public async Task<(List<Error> errors, string name)> GetNameByCommit(string repoOwner, string repoName, string commitSha)
        {
            Debug.Assert(!string.IsNullOrEmpty(repoOwner));
            Debug.Assert(!string.IsNullOrEmpty(repoName));
            Debug.Assert(!string.IsNullOrEmpty(commitSha));

            GitHubCommit githubCommit;
            Author author;
            if (_isRateLimitExceeded)
                return (new List<Error> { Errors.ResolveCommitFailed(commitSha, $"{repoOwner}/{repoName}", _rateLimitExceededMessage) }, null);

            try
            {
                githubCommit = await _client.Repository.Commit.Get(repoOwner, repoName, commitSha);
                author = githubCommit.Author;
            }
            catch (RateLimitExceededException)
            {
                _isRateLimitExceeded = true;
                return (new List<Error> { Errors.ResolveCommitFailed(commitSha, $"{repoOwner}/{repoName}", _rateLimitExceededMessage) }, null);
            }
            catch (Exception)
            {
                // catch NotFoundException if owner/repo doesn't exist
                // catch ApiValidationException if no commit found for SHA
                return (new List<Error>(), null);
            }

            return (new List<Error>(), author.Login);
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
