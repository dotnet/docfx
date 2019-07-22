// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Microsoft.Docs.Build
{
    internal sealed class GitHubAccessor : IDisposable
    {
        private static readonly Uri _url = new Uri("https://api.github.com/graphql");

        private readonly HttpClient _httpClient = new HttpClient();
        private readonly ConcurrentHashSet<(string owner, string name)> _unknownRepos = new ConcurrentHashSet<(string owner, string name)>();

        private volatile Error _rateLimitError;
        private volatile Error _unauthorizedError;

        public GitHubAccessor(string token = null)
        {
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
    login
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

                var grahpApiResponse = JsonConvert.DeserializeAnonymousType(response, new
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
                        _rateLimitError = Errors.GitHubApiFailed(rateLimitError.message);
                    }

                    return (Errors.GitHubApiFailed(rateLimitError?.message ?? grahpApiResponse.errors.First().message), null);
                }

                return (null, new GitHubUser
                {
                    Id = grahpApiResponse.data.user.databaseId,
                    Login = grahpApiResponse.data.user.login,
                    Name = string.IsNullOrEmpty(grahpApiResponse.data.user.name) ? grahpApiResponse.data.user.login : grahpApiResponse.data.user.name,
                    Emails = !string.IsNullOrEmpty(grahpApiResponse.data.user.email) ? new[] { grahpApiResponse.data.user.email } : Array.Empty<string>(),
                });
            }
            catch (Exception ex)
            {
                Log.Write(ex);
                return (Errors.GitHubApiFailed(ex.InnerException?.Message ?? ex.Message), null);
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

                var grahpApiResponse = JsonConvert.DeserializeAnonymousType(response, new
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
                        _rateLimitError = Errors.GitHubApiFailed(rateLimitError.message);
                    }

                    return (Errors.GitHubApiFailed(notFoundError?.message ?? rateLimitError?.message ?? grahpApiResponse.errors.First().message), null);
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
                                Name = string.IsNullOrEmpty(node.author.user?.name) ? node.author.user?.login : node.author.user?.name,
                                Emails = new[] { node.author.email },
                            });
                        }
                    }
                }

                return (null, githubUsers);
            }
            catch (Exception ex)
            {
                Log.Write(ex);
                return (Errors.GitHubApiFailed(ex.InnerException?.Message ?? ex.Message), null);
            }
        }

        private async Task<(Error, string)> Query(string query, object variables)
        {
            var request = JsonUtility.Serialize(new { query, variables });

            using (var response = await RetryUtility.Retry(
                   () => _httpClient.PostAsync(_url, new StringContent(request, Encoding.UTF8, "application/json")),
                   ex =>
                    (ex.InnerException ?? ex) is OperationCanceledException ||
                    (ex.InnerException ?? ex) is System.IO.IOException))
            {
                var content = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == (HttpStatusCode)401)
                    {
                        _unauthorizedError = Errors.GitHubApiFailed(content);
                    }
                    return (Errors.GitHubApiFailed(content), null);
                }

                return (null, content);
            }
        }
    }
}
