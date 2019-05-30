// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Microsoft.Docs.Build
{
    internal sealed class GitHubAccessor : IDisposable
    {
        private readonly string _url;
        private readonly HttpClient _httpClient = new HttpClient();
        private readonly ConcurrentHashSet<(string owner, string name)> _unknownRepos = new ConcurrentHashSet<(string owner, string name)>();

        private volatile Error _rateLimitError;
        private volatile Error _unauthorizedError;

        public GitHubAccessor(string token = null)
        {
            _url = "https://api.github.com/graphql";

            _httpClient.DefaultRequestHeaders.Add("User-Agent", "DocFX");
            if (!string.IsNullOrEmpty(token))
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", token);
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }

        public async Task<(Error, GitHubUser)> GetUserByLogin(string login)
        {
            if (_rateLimitError != null)
            {
                return (_rateLimitError, null);
            }

            if (_unauthorizedError != null)
            {
                return default;
            }

            var query = @"
query ($login: String!) {
  user(login: $login) {
    name
    email
    databaseId
  }
}";

            try
            {
                var (error, response) = await Query(query, new
                {
                    login,
                });

                if (error != null)
                {
                    return (error, null);
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
                        user = new
                        {
                            name = "",
                            email = "",
                            login = "",
                            databaseId = 0,
                        },
                    },
                });

                if (grahpApiResponse.errors != null && grahpApiResponse.errors.Length != 0)
                {
                    var notFoundError = grahpApiResponse.errors.FirstOrDefault(e => e.type == "NOT_FOUND" && e.path.Contains("user"));
                    if (notFoundError != null)
                        return default;

                    var rateLimitError = grahpApiResponse.errors.FirstOrDefault(e => e.type == "MAX_NODE_LIMIT_EXCEEDED" || e.type == "RATE_LIMITED");
                    if (rateLimitError != null)
                    {
                        _rateLimitError = Errors.GitHubApiFailed(_url, rateLimitError.message);
                    }

                    return (Errors.GitHubApiFailed(_url, rateLimitError?.message ?? grahpApiResponse.errors.First().message), null);
                }

                return (null, new GitHubUser
                {
                    Id = grahpApiResponse.data.user.databaseId,
                    Login = grahpApiResponse.data.user.login,
                    Name = grahpApiResponse.data.user.name,
                    Emails = !string.IsNullOrEmpty(grahpApiResponse.data.user.email) ? new[] { grahpApiResponse.data.user.email } : Array.Empty<string>(),
                });
            }
            catch (Exception ex)
            {
                return (Errors.GitHubApiFailed(_url, ex.InnerException?.Message ?? ex.Message), null);
            }
        }

        public async Task<(Error, IEnumerable<GitHubUser>)> GetUsersByCommit(string owner, string name, string commit)
        {
            if (_rateLimitError != null)
            {
                return default;
            }

            if (_unauthorizedError != null)
            {
                return default;
            }

            if (_unknownRepos.Contains((owner, name)))
            {
                return default;
            }

            var query = @"
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

            try
            {
                var (error, response) = await Query(query, new
                {
                    owner,
                    name,
                    commit,
                });

                if (error != null)
                {
                    return (error, null);
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

                if (grahpApiResponse.errors != null && grahpApiResponse.errors.Length != 0)
                {
                    var notFoundError = grahpApiResponse.errors.FirstOrDefault(e => e.type == "NOT_FOUND" && e.path.Contains("repository"));
                    if (notFoundError != null)
                    {
                        // owner/repo doesn't exist or you don't have access to the repo
                        _unknownRepos.TryAdd((owner, name));
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
                return (Errors.GitHubApiFailed(_url, ex.InnerException?.Message ?? ex.Message), null);
            }
        }

        private async Task<(Error, HttpResponseMessage)> Query(string query, object variables)
        {
            var request = new
            {
                query,
                variables,
            };

            var response = await RetryUtility.Retry(
                   () => _httpClient.SendAsync(
                       new HttpRequestMessage
                       {
                           RequestUri = new Uri(_url),
                           Content = new StringContent(JsonUtility.Serialize(request), System.Text.Encoding.UTF8, "application/json"),
                           Method = HttpMethod.Post,
                       }),
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

            return (null, response);
        }
    }
}
