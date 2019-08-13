// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Polly;
using Polly.Extensions.Http;

namespace Microsoft.Docs.Build
{
    internal sealed class GitHubAccessor : IDisposable
    {
        private static readonly Uri s_url = new Uri("https://api.github.com/graphql");

        private static readonly string[] s_fatelErrorCodes = { "MAX_NODE_LIMIT_EXCEEDED", "RATE_LIMITED" };
        private static readonly HttpStatusCode[] s_fatelStatusCodes = { HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden };

        private readonly HttpClient _httpClient = new HttpClient();

        private volatile Error _fatelError;

        public GitHubAccessor(string token)
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
            // Stop calling GitHub whenever any non-transient error is returned from github
            if (_fatelError != null)
            {
                return (_fatelError, default);
            }

            Log.Write($"Calling GitHub user API to resolve {login}");

            var query = @"
query ($login: String!) {
  user(login: $login) {
    name
    email
    databaseId
    login
  }
}";

            var (error, data) = await Query(
                query,
                new { login },
                new { user = new { name = "", email = "", login = "", databaseId = 0 } });

            if (error != null || data?.user is null)
            {
                return (error, null);
            }

            return (null, new GitHubUser
            {
                Id = data.user.databaseId,
                Login = data.user.login,
                Name = string.IsNullOrEmpty(data.user.name) ? data.user.login : data.user.name,
                Emails = new[] { data.user.email }.Where(email => !string.IsNullOrEmpty(email)).ToArray(),
            });
        }

        public async Task<(Error, IEnumerable<GitHubUser>)> GetUsersByCommit(string owner, string name, string commit, string authorEmail = null)
        {
            // Stop calling GitHub whenever any error is returned from github
            if (_fatelError != null)
            {
                return (_fatelError, default);
            }

            Log.Write($"Calling GitHub commit API to resolve {authorEmail}");

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

            var user = new { name = "", email = "", login = "", databaseId = 0 };
            var history = new { nodes = new[] { new { author = new { email = "", user } } } };

            var (error, data) = await Query(
                query,
                new { owner, name, commit },
                new { repository = new { @object = new { history } } });

            if (error != null)
            {
                return (error, null);
            }

            var githubUsers = new List<GitHubUser>();

            if (data?.repository?.@object?.history?.nodes != null)
            {
                foreach (var node in data.repository.@object.history.nodes)
                {
                    if (node.author != null)
                    {
                        githubUsers.Add(new GitHubUser
                        {
                            Id = node.author?.user.databaseId,
                            Login = node.author?.user.login,
                            Name = string.IsNullOrEmpty(node.author.user?.name) ? node.author.user?.login : node.author.user?.name,
                            Emails = new[] { node.author.email, node.author.user?.email }
                                .Where(email => !string.IsNullOrEmpty(email)).ToArray(),
                        });
                    }
                }
            }

            return (null, githubUsers);
        }

        private async Task<(Error error, T data)> Query<T>(string query, object variables, T dataType)
        {
            Debug.Assert(dataType != null);

            var request = JsonUtility.Serialize(new { query, variables });

            try
            {
                using (var response = await HttpPolicyExtensions
                    .HandleTransientHttpError()
                    .Or<OperationCanceledException>()
                    .RetryAsync(3)
                    .ExecuteAsync(() => _httpClient.PostAsync(s_url, new StringContent(request, Encoding.UTF8, "application/json"))))
                {
                    if (s_fatelStatusCodes.Contains(response.StatusCode))
                    {
                        Log.Write(await response.Content.ReadAsStringAsync());
                        _fatelError = Errors.GitHubApiFailed(response.StatusCode.ToString());
                        return (_fatelError, default);
                    }

                    var content = await response.EnsureSuccessStatusCode().Content.ReadAsStringAsync();

                    var body = JsonConvert.DeserializeAnonymousType(
                        content,
                        new { data = default(T), errors = new[] { new { type = "", message = "" } } });

                    var fatelError = body.errors?.FirstOrDefault(error => s_fatelErrorCodes.Contains(error.type));
                    if (fatelError != null)
                    {
                        _fatelError = Errors.GitHubApiFailed($"[{fatelError.type}] {fatelError.message}");
                        return (_fatelError, default);
                    }

                    return (null, body.data);
                }
            }
            catch (Exception ex)
            {
                Log.Write(ex);
                return (Errors.GitHubApiFailed(ex.Message), default);
            }
        }
    }
}
