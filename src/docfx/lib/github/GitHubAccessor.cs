// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Octokit;

namespace Microsoft.Docs.Build
{
    internal class GitHubAccessor
    {
        private readonly Config _config;
        private readonly string _url;
        private readonly GitHubClient _client;
        private readonly ConcurrentHashSet<(string owner, string name)> _unknownRepos = new ConcurrentHashSet<(string owner, string name)>();

        private volatile Error _rateLimitError;

        public GitHubAccessor(Config config = null)
        {
            _config = config;
            _client = new GitHubClient(new ProductHeaderValue("DocFX"));
            _url = "https://api.github.com/graphql";
            if (!string.IsNullOrEmpty(_config.GitHub.AuthToken))
                _client.Credentials = new Credentials(_config.GitHub.AuthToken);
        }

        public async Task<(Error, GitHubUser)> GetUserByLogin(string login)
        {
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
                _rateLimitError = Errors.GitHubApiFailed(apiDetail, ex.Message);
                return (_rateLimitError, null);
            }
            catch (Exception ex)
            {
                LogAbuseExceptionDetail(apiDetail, ex);
                return (Errors.GitHubApiFailed(apiDetail, ex.Message), null);
            }
        }

        public async Task<(Error, IEnumerable<GitHubUser>)> GetUsersByCommit(string repoOwner, string repoName, string commitSha)
        {
            if (_rateLimitError != null)
            {
                return (_rateLimitError, null);
            }

            if (_unknownRepos.Contains((repoOwner, repoName)))
            {
                return default;
            }

            var queryStr = @"
query ($owner: String!, $name: String!, $commit: String!) {
  repository(owner: $owner, name: $name) {
    object(expression: $commit) {
      ... on Commit {
        history(first: 100) {
          nodes {
            author {
              email
              user {
                databaseId
                name
                login
              }
            }
          }
        }
      }
    }
  }
}";

            var request = new
            {
                query = queryStr,
                variables = new
                {
                    owner = repoOwner,
                    name = repoName,
                    commit = commitSha,
                },
            };

            try
            {
                var response = await RetryUtility.Retry(
                    () => HttpClientUtility.PostAsync(
                        _url,
                        new StringContent(JsonUtility.Serialize(request), System.Text.Encoding.UTF8, "application/json"),
                        _config,
                        new Dictionary<string, string>
                        {
                            { "User-Agent", repoOwner },
                        }),
                    ex => ex is OperationCanceledException);

                if (!response.IsSuccessStatusCode)
                {
                    var message = await response.Content.ReadAsStringAsync();
                    return (Errors.GitHubApiFailed(_url, message), null);
                }

                var grahpApiResponse = JsonUtility.Deserialize<GithubGraphApiResponse<JObject>>(await response.Content.ReadAsStringAsync(), null);
                if (grahpApiResponse.Errors != null && grahpApiResponse.Errors.Any())
                {
                    return (Errors.GitHubApiFailed(_url, grahpApiResponse.Errors.First().Message), null);
                }

                var githubUsers = new List<GitHubUser>();
                if (grahpApiResponse.Data.TryGetValue("repository", out var r) && r is JObject repo &&
                    repo.TryGetValue("object", out var o) && o is JObject obj &&
                    obj.TryGetValue("history", out var h) && h is JObject history &&
                    history.TryGetValue("nodes", out var n) && n is JArray nodes)
                {
                    foreach (var node in nodes)
                    {
                        if (node is JObject nodeObj && nodeObj.TryGetValue("author", out var a) && a is JObject)
                        {
                            var (_, author) = JsonUtility.ToObject<GithubGraphApAuthor>(a);
                            githubUsers.Add(ToGitHubUser(author));
                        }
                    }
                }

                return (null, githubUsers);
            }
            catch (RateLimitExceededException ex)
            {
                _rateLimitError = Errors.GitHubApiFailed(_url, ex.Message);
                return (_rateLimitError, null);
            }
            catch (Exception ex)
            {
                LogAbuseExceptionDetail(_url, ex);
                return (Errors.GitHubApiFailed(_url, ex.Message), null);
            }

            GitHubUser ToGitHubUser(GithubGraphApAuthor author)
            {
                return new GitHubUser
                {
                    Id = author.User?.DatabaseId,
                    Login = author.User?.Login,
                    Name = author.User?.Name,
                    Emails = new[] { author.Email },
                };
            }
        }

        private static void LogAbuseExceptionDetail(string api, Exception ex)
        {
            if (ex is AbuseException aex)
            {
                Log.Write($"Failed calling GitHub API '{api}', message: '{ex.Message}', retryAfterSeconds: '{aex.RetryAfterSeconds}'");
            }
        }

        private class GithubGraphApAuthor
        {
            public string Email { get; set; }

            public GithubGraphApUser User { get; set; }
        }

        private class GithubGraphApUser
        {
            public int? DatabaseId { get; set; }

            public string Name { get; set; }

            public string Login { get; set; }
        }

        private class GithubGraphApiResponse<T>
        {
            public List<GitHubGraphApiError> Errors { get; set; }

            public T Data { get; set; }
        }

        private class GitHubGraphApiError
        {
            public string Message { get; set; }
        }
    }
}
