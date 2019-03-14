// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Octokit;

namespace Microsoft.Docs.Build
{
    internal class GitHubAccessor
    {
        private readonly GitHubClient _client;

        // Bypass GitHub abuse detection:
        // https://developer.github.com/v3/guides/best-practices-for-integrators/#dealing-with-abuse-rate-limits
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(10, 10);

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
                    () => UseResource(() => _client.User.Get(login)),
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
                var commit = await RetryUtility.Retry(
                    () => UseResource(() => _client.Repository.Commit.Get(repoOwner, repoName, commitSha)),
                    ex => ex is OperationCanceledException);
                return (null, commit.Author?.Login);
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
                LogAbuseExceptionDetail(apiDetail, ex);
                return (Errors.GitHubApiFailed(apiDetail, ex), null);
            }
        }

        private void LogAbuseExceptionDetail(string api, Exception ex)
        {
            if (ex is AbuseException aex)
            {
                Log.Write($"Failed calling GitHub API '{api}', message: '{ex.Message}', retryAfterSeconds: '{aex.RetryAfterSeconds}'");
            }
        }

        private async Task<T> UseResource<T>(Func<Task<T>> action)
        {
            await _semaphore.WaitAsync();
            try
            {
                return await action();
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
