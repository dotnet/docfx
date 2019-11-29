// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
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

        private readonly HttpClient _httpClient;
        private readonly ConcurrentHashSet<(string owner, string name)> _unknownRepos = new ConcurrentHashSet<(string owner, string name)>();
        private readonly JsonDiskCache<Error, string, GitHubUser> _userCache;

        private volatile Error _fatalError;

        public GitHubAccessor(Config config)
        {
            _userCache = new JsonDiskCache<Error, string, GitHubUser>(
                AppData.GitHubUserCachePath, TimeSpan.FromHours(config.GitHub.UserCacheExpirationInHours), StringComparer.OrdinalIgnoreCase);

            if (!string.IsNullOrEmpty(config.GitHub.AuthToken))
            {
                _httpClient = new HttpClient();
                _httpClient.DefaultRequestHeaders.Add("User-Agent", "DocFX");
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", config.GitHub.AuthToken);
            }
        }

        public (Error, GitHubUser) GetUserByLogin(SourceInfo<string> login)
        {
            var (error, user) = _userCache.GetOrAdd(login.Value, GetUserByLoginCore);
            if (user != null && !user.IsValid())
            {
                return (Errors.AuthorNotFound(login), null);
            }

            return (error, user);
        }

        public (Error, GitHubUser) GetUserByEmail(string email, string owner, string name, string commit)
        {
            var (error, user) = _userCache.GetOrAdd(email, _ => GetUserByEmailCore(email, owner, name, commit));
            return (error, user != null && user.IsValid() ? user : null);
        }

        public Task<Error[]> Save()
        {
            return _userCache.Save();
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }

        private async Task<(Error, GitHubUser)> GetUserByLoginCore(string login)
        {
            if (_fatalError != null || _httpClient is null)
            {
                return default;
            }

            using (PerfScope.Start($"Calling GitHub user API to resolve {login}"))
            {
                var (error, errorCode, data) = await Query(
                    GitHubQueries.UserQuery,
                    new { login },
                    new { user = new { name = "", email = "", login = "", databaseId = 0 } });

                if (error != null && errorCode != "NOT_FOUND")
                {
                    // Return `null` here to avoid caching an invalid user on disk when GitHub is down
                    return (error, null);
                }

                if (data?.user is null)
                {
                    // Cache invalid github user by creating a fake object.
                    // JsonDiskCache does not save `null` values on disk.
                    return (null, new GitHubUser { Login = login });
                }

                return (null, new GitHubUser
                {
                    Id = data.user.databaseId,
                    Login = data.user.login,
                    Name = string.IsNullOrEmpty(data.user.name) ? data.user.login : data.user.name,
                    Emails = new[] { data.user.email }.Where(email => !string.IsNullOrEmpty(email)).ToArray(),
                });
            }
        }

        private async Task<(Error, IEnumerable<GitHubUser>)> GetUserByEmailCore(string email, string owner, string name, string commit)
        {
            if (_unknownRepos.Contains((owner, name)))
            {
                return default;
            }

            if (_fatalError != null || _httpClient is null)
            {
                return default;
            }

            using (PerfScope.Start($"Calling GitHub commit API to resolve {email}"))
            {
                var user = new { name = "", email = "", login = "", databaseId = 0 };
                var history = new { nodes = new[] { new { author = new { email = "", user } } } };

                var (error, errorCode, data) = await Query(
                    GitHubQueries.CommitQuery,
                    new { owner, name, commit },
                    new { repository = new { @object = new { history } } });

                if (errorCode == "NOT_FOUND")
                {
                    _unknownRepos.TryAdd((owner, name));
                    return default;
                }

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
                                Id = node.author.user?.databaseId,
                                Login = node.author.user?.login,
                                Name = string.IsNullOrEmpty(node.author.user?.name) ? node.author.user?.login : node.author.user?.name,
                                Emails = new[] { node.author.user?.email, node.author.email }
                                    .Where(str => !string.IsNullOrEmpty(str)).Distinct().ToArray(),
                            });
                        }
                    }
                }

                return (null, githubUsers);
            }
        }

        private async Task<(Error error, string errorCode, T data)> Query<T>(string query, object variables, T dataType)
        {
            dataType.GetHashCode();

            var request = JsonUtility.Serialize(new { query, variables });

            try
            {
                using (var response = await HttpPolicyExtensions
                    .HandleTransientHttpError()
                    .Or<OperationCanceledException>()
                    .Or<IOException>()
                    .RetryAsync(3)
                    .ExecuteAsync(() => _httpClient.PostAsync(s_url, new StringContent(request, Encoding.UTF8, "application/json"))))
                {
                    if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
                    {
                        Log.Write(await response.Content.ReadAsStringAsync());
                        _fatalError = Errors.GitHubApiFailed(response.StatusCode.ToString());
                        return (_fatalError, default, default);
                    }

                    var content = await response.EnsureSuccessStatusCode().Content.ReadAsStringAsync();

                    var body = JsonConvert.DeserializeAnonymousType(
                        content,
                        new { data = default(T), errors = new[] { new { type = "", message = "" } } });

                    if (body.errors != null)
                    {
                        foreach (var error in body.errors)
                        {
                            switch (error.type)
                            {
                                case "MAX_NODE_LIMIT_EXCEEDED":
                                case "RATE_LIMITED":
                                    _fatalError = Errors.GitHubApiFailed($"[{error.type}] {error.message}");
                                    return (_fatalError, default, default);

                                default:
                                    return (Errors.GitHubApiFailed($"[{error.type}] {error.message}"), error.type, default);
                            }
                        }
                    }

                    return (null, null, body.data);
                }
            }
            catch (Exception ex)
            {
                Log.Write(ex);
                return (Errors.GitHubApiFailed(ex.Message), default, default);
            }
        }
    }
}
