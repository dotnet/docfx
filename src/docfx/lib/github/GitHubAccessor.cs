// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Octokit;

namespace Microsoft.Docs.Build
{
    internal class GitHubAccessor
    {
        private readonly GitHubClient _client;

        private static volatile Error _rateLimitError;

        public GitHubAccessor(string token = null)
        {
            _client = new GitHubClient(new ProductHeaderValue("DocFX"));
            if (!string.IsNullOrEmpty(token))
                _client.Credentials = new Credentials(token);
        }

        public async Task<(Error, GitHubUser)> GetUserByLogin(string login)
        {
            Debug.Assert(!string.IsNullOrEmpty(login));

            if (_rateLimitError != null)
            {
                return (_rateLimitError, null);
            }

            var apiDetail = $"GET /users/{login}";
            try
            {
                var user = await RetryUtility.Retry(
                    () => _client.User.Get(login),
                    ex => ex is OperationCanceledException);

                return (null, new GitHubUser
                {
                    Id = user.Id,
                    Login = user.Login,
                    Name = user.Name,
                    Emails = !string.IsNullOrEmpty(user.Email) ? new[] { user.Email } : Array.Empty<string>(),
                });
            }
            catch (NotFoundException)
            {
                // GitHub will return 404 "Not Found" if the user doesn't exist
                return default;
            }
            catch (RateLimitExceededException ex)
            {
                _rateLimitError = Errors.GitHubApiFailed(apiDetail, ex);
                return (_rateLimitError, null);
            }
            catch (Exception ex)
            {
                LogAbuseExceptionDetail(apiDetail, ex);
                return (Errors.GitHubApiFailed(apiDetail, ex), null);
            }
        }

        public async Task<(Error, IEnumerable<GitHubUser>)> GetUsersByCommit(string repoOwner, string repoName, string commitSha)
        {
            Debug.Assert(!string.IsNullOrEmpty(repoOwner));
            Debug.Assert(!string.IsNullOrEmpty(repoName));
            Debug.Assert(!string.IsNullOrEmpty(commitSha));

            if (_rateLimitError != null)
            {
                return (_rateLimitError, null);
            }

            var apiDetail = $"GET /repos/{repoOwner}/{repoName}/commits/{commitSha}";
            try
            {
                var commits = await RetryUtility.Retry(
                    () => _client.Repository.Commit.GetAll(
                        repoOwner,
                        repoName,
                        new CommitRequest { Sha = commitSha },
                        new ApiOptions { PageCount = 1, PageSize = 100 }),
                    ex => ex is OperationCanceledException);

                return (null, commits.Select(ToGitHubUser));
            }
            catch (NotFoundException)
            {
                // owner/repo doesn't exist or you don't have access to the repo
                return default;
            }
            catch (ApiValidationException)
            {
                // commit does not exist
                return default;
            }
            catch (RateLimitExceededException ex)
            {
                _rateLimitError = Errors.GitHubApiFailed(apiDetail, ex);
                return (_rateLimitError, null);
            }
            catch (Exception ex)
            {
                LogAbuseExceptionDetail(apiDetail, ex);
                return (Errors.GitHubApiFailed(apiDetail, ex), null);
            }
        }

        private static GitHubUser ToGitHubUser(GitHubCommit commit)
        {
            return new GitHubUser
            {
                Id = commit.Author?.Id,
                Login = commit.Author?.Login,
                Name = commit.Commit.Author.Name,
                Emails = new[] { commit.Commit.Author.Email },
            };
        }

        private static void LogAbuseExceptionDetail(string api, Exception ex)
        {
            if (ex is AbuseException aex)
            {
                Log.Write($"Failed calling GitHub API '{api}', message: '{ex.Message}', retryAfterSeconds: '{aex.RetryAfterSeconds}'");
            }
        }
    }
}
