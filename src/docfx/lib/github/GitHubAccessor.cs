// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Octokit;

namespace Microsoft.Docs.Build
{
    internal class GitHubAccessor
    {
        private const int RetryCount = 3;
        private const int RetryInterval = 10000;
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
                var (error, user) = await RetryApi(apiDetail, () => _client.User.Get(login));

                return (error, user == null ? null : new GitHubUser
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
                return (Errors.GitHubApiFailed(apiDetail, ex), null);
            }
        }

        public async Task<(Error, string login)> GetLoginByCommit(string repoOwner, string repoName, string commitSha)
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
                var (error, commit) = await RetryApi(apiDetail, () => _client.Repository.Commit.Get(repoOwner, repoName, commitSha));
                return (null, commit?.Author?.Login);
            }
            catch (NotFoundException)
            {
                // owner/repo doesn't exist
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
                return (Errors.GitHubApiFailed(apiDetail, ex), null);
            }
        }

        public async Task<(Error, T)> RetryApi<T>(string api, Func<Task<T>> action)
        {
            T result = default;
            for (var i = 0; i < RetryCount; i++)
            {
                try
                {
                    result = await action();
                }
                catch (Exception ex)
                {
                    if (i < RetryCount - 1)
                    {
                        await Task.Delay(RetryInterval);
                        continue;
                    }
                    else
                    {
                        return (Errors.GitHubApiFailed(api, ex), default);
                    }
                }
                return (default, result);
            }

            Debug.Fail("should never reach here");
            return default;
        }
    }
}
