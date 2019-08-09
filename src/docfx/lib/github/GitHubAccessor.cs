// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Microsoft.Docs.Build
{
    internal sealed class GitHubAccessor : IDisposable
    {
        private static readonly Uri s_url = new Uri("https://api.github.com/graphql");

        private readonly HttpClient _httpClient = new HttpClient();

        private readonly ConcurrentHashSet<string> _loginApiCalls = new ConcurrentHashSet<string>();
        private readonly ConcurrentHashSet<string> _emailApiCalls = new ConcurrentHashSet<string>();

        private volatile Error _error;

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
            // Stop calling GitHub whenever any error is returned from github
            if (_error != null)
            {
                return (_error, default);
            }

            Log.Write($"Calling GitHub user API to resolve {login}");
            Debug.Assert(_loginApiCalls.TryAdd(login));

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

            if (error != null)
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
            if (_error != null)
            {
                return (_error, default);
            }

            Log.Write($"Calling GitHub commit API to resolve {authorEmail}");
            Debug.Assert(_emailApiCalls.TryAdd(authorEmail));

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

            if (data?.repository?.@object?.history?.nodes == null)
            {
                foreach (var node in data.repository.@object.history.nodes)
                {
                    if (node.author?.user != null)
                    {
                        githubUsers.Add(new GitHubUser
                        {
                            Id = node.author.user.databaseId,
                            Login = node.author.user.login,
                            Name = string.IsNullOrEmpty(node.author.user.name) ? node.author.user.login : node.author.user.name,
                            Emails = new[] { node.author.email, node.author.user.email }
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
                using (var response = await RetryUtility.Retry(
                       () => _httpClient.PostAsync(s_url, new StringContent(request, Encoding.UTF8, "application/json")),
                       ex =>
                        (ex.InnerException ?? ex) is OperationCanceledException ||
                        (ex.InnerException ?? ex) is System.IO.IOException))
                {
                    var content = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        _error = Errors.GitHubApiFailed(response.StatusCode.ToString());
                        return (_error, default);
                    }

                    // https://graphql.github.io/graphql-spec/June2018/#sec-Response-Format
                    // 7.1: If the operation encountered any errors, the response map must contain an entry with key `errors`
                    if (content.Contains("\"errors\":"))
                    {
                        var body = JsonConvert.DeserializeAnonymousType(content, new { errors = new[] { new { message = "" } } });

                        if (body.errors != null && body.errors.Length > 0)
                        {
                            _error = Errors.GitHubApiFailed(body.errors[0].message);
                            return (_error, default);
                        }
                    }

                    return (null, JsonConvert.DeserializeAnonymousType(content, new { data = default(T) }).data);
                }
            }
            catch (Exception ex)
            {
                _error = Errors.GitHubApiFailed(ex.Message);
                Log.Write(_error.ToException(ex));
                return (_error, default);
            }
        }
    }
}
