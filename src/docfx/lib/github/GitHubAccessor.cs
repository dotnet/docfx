// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Octokit;

namespace Microsoft.Docs.Build
{
    internal class GitHubAccessor
    {
        private static readonly HttpClient s_httpClient = new HttpClient();

        private readonly string _token;
        private readonly string _url;
        private readonly GitHubClient _client;
        private readonly ConcurrentHashSet<(string owner, string name)> _unknownRepos = new ConcurrentHashSet<(string owner, string name)>();

        private volatile Error _rateLimitError;
        private volatile Error _unauthorizedError;

        public GitHubAccessor(string token = null)
        {
            _token = token;
            _client = new GitHubClient(new ProductHeaderValue("DocFX"));
            _url = "https://api.github.com/graphql";
            if (!string.IsNullOrEmpty(_token))
                _client.Credentials = new Credentials(_token);
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
                return default;
            }

            if (_unauthorizedError != null)
            {
                return default;
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
                    () => s_httpClient.SendAsync(
                        CreateHttpRequest(
                            new StringContent(JsonUtility.Serialize(request), System.Text.Encoding.UTF8, "application/json"))),
                    ex => ex is OperationCanceledException);

                if (!response.IsSuccessStatusCode)
                {
                    var message = await response.Content.ReadAsStringAsync();

                    if (response.StatusCode == (HttpStatusCode)401)
                    {
                        _unauthorizedError = Errors.GitHubApiFailed(_url, message);
                    }
                    return (Errors.GitHubApiFailed(_url, message), null);
                }

                var grahpApiResponse = JsonConvert.DeserializeAnonymousType(await response.Content.ReadAsStringAsync(), new
                {
                    errors = new[]
                    {
                        new
                        {
                            type = "",
                            path = Array.Empty<string>(),
                            message = "",
                        },
                    },
                    data = new
                    {
                        repository = new
                        {
                            @object = new
                            {
                                history = new
                                {
                                    nodes = new[]
                                    {
                                        new
                                        {
                                            author = new
                                            {
                                                email = "",
                                                user = new
                                                {
                                                    databaseId = 0,
                                                    login = "",
                                                    name = "",
                                                },
                                            },
                                        },
                                    },
                                },
                            },
                        },
                    },
                });

                if (grahpApiResponse.errors != null && grahpApiResponse.errors.Any())
                {
                    var notFoundError = grahpApiResponse.errors.FirstOrDefault(e => e.type == "NOT_FOUND" && e.path.Contains("repository"));
                    if (notFoundError != null)
                    {
                        // owner/repo doesn't exist or you don't have access to the repo
                        _unknownRepos.TryAdd((repoOwner, repoName));
                    }

                    var rateLimitError = grahpApiResponse.errors.FirstOrDefault(e => e.type == "MAX_NODE_LIMIT_EXCEEDED" || e.type == "RATE_LIMITED");
                    if (rateLimitError != null)
                    {
                        _rateLimitError = Errors.GitHubApiFailed(_url, rateLimitError.message);
                    }

                    return (Errors.GitHubApiFailed(_url, notFoundError?.message ?? rateLimitError?.message ?? grahpApiResponse.errors.First().message), null);
                }

                var githubUsers = new List<GitHubUser>();
                if (grahpApiResponse.data?.repository?.@object?.history?.nodes != null)
                {
                    foreach (var node in grahpApiResponse.data?.repository?.@object?.history?.nodes)
                    {
                        if (node.author != null)
                        {
                            githubUsers.Add(new GitHubUser
                            {
                                Id = node.author.user?.databaseId,
                                Login = node.author.user?.login,
                                Name = node.author.user?.name,
                                Emails = new[] { node.author.email },
                            });
                        }
                    }
                }

                return (null, githubUsers);
            }
            catch (Exception ex)
            {
                return (Errors.GitHubApiFailed(_url, ex.Message), null);
            }

            HttpRequestMessage CreateHttpRequest(HttpContent content)
            {
                var message = new HttpRequestMessage();
                message.Headers.Add("User-Agent", "DocFX");
                if (!string.IsNullOrEmpty(_token))
                    message.Headers.Add("Authorization", $"bearer {_token}");

                message.RequestUri = new Uri(_url);
                message.Content = content;
                message.Method = HttpMethod.Post;
                return message;
            }
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
