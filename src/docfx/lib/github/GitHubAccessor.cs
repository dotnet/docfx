// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Polly;
using Polly.Extensions.Http;

namespace Microsoft.Docs.Build;

internal sealed class GitHubAccessor
{
    private static readonly Uri s_url = new("https://api.github.com/graphql");

    private readonly HttpClient? _httpClient;
    private readonly SemaphoreSlim _syncRoot = new(1, 1);
    private readonly ConcurrentHashSet<(string owner, string name)> _unknownRepos = new();
    private readonly JsonDiskCache<Error, string, GitHubUser> _userCache;

    private volatile Error? _fatalError;

    public GitHubAccessor(Config config, string githubToken)
    {
        _userCache = new(
            AppData.GitHubUserCachePath,
            TimeSpan.FromHours(config.GithubUserCacheExpirationInHours),
            StringComparer.OrdinalIgnoreCase,
            ResolveGitHubUserConflict);
        if (!string.IsNullOrEmpty(githubToken))
        {
            _httpClient = new HttpClient(new HttpClientHandler { CheckCertificateRevocationList = true });
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "DocFX");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", githubToken);
        }
    }

    public (Error?, GitHubUser?) GetUserByLogin(SourceInfo<string> login)
    {
        if (string.IsNullOrEmpty(login))
        {
            return default;
        }

        var (error, user) = _userCache.GetOrAdd(login.Value, GetUserByLoginCore);
        if (user != null && !user.IsValid())
        {
            return (Errors.Metadata.AuthorNotFound(login), null);
        }

        return (error, user);
    }

    public (Error?, GitHubUser?) GetUserByEmail(string email, string owner, string name, string commit)
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(owner) || string.IsNullOrEmpty(name) || string.IsNullOrEmpty(commit))
        {
            return default;
        }

        var (error, user) = _userCache.GetOrAdd(email, _ => GetUserByEmailCore(email, owner, name, commit));
        return (error, user != null && user.IsValid() ? user : null);
    }

    public Error[] Save()
    {
        return _userCache.Save();
    }

    private async Task<(Error?, GitHubUser?)> GetUserByLoginCore(string login)
    {
        if (_fatalError != null || _httpClient is null)
        {
            return default;
        }

        var (error, errorCode, data) = await Query(
            login,
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

        var emails = from email in new[] { data.user.email } where !string.IsNullOrEmpty(email) select email;

        return (null, new GitHubUser
        {
            Id = data.user.databaseId,
            Login = data.user.login,
            Name = string.IsNullOrEmpty(data.user.name) ? data.user.login : data.user.name,
            Emails = emails.ToArray(),
        });
    }

    private async Task<(Error?, IEnumerable<GitHubUser>)> GetUserByEmailCore(string email, string owner, string name, string commit)
    {
        if (_unknownRepos.Contains((owner, name)))
        {
            return default;
        }

        if (_fatalError != null || _httpClient is null)
        {
            return default;
        }

        var user = new { name = "", email = "", login = "", databaseId = 0 };
        var history = new { nodes = new[] { new { author = new { email = "", user } } } };

        var (error, errorCode, data) = await Query(
            email,
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
            return (error, Array.Empty<GitHubUser>());
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
                        Emails = new[] { node.author.user?.email!, node.author.email }
                            .Where(str => !string.IsNullOrEmpty(str)).Distinct().ToArray(),
                    });
                }
            }
        }

        return (null, githubUsers);
    }

    private async Task<(Error? error, string? errorCode, T? data)> Query<T>(string api, string query, object variables, T dataType)
        where T : class
    {
        Debug.Assert(dataType != null);

        try
        {
            using var request = new StringContent(JsonUtility.Serialize(new { query, variables }), Encoding.UTF8, "application/json");
            using var response = await HttpPolicyExtensions
                .HandleTransientHttpError()
                .Or<OperationCanceledException>()
                .Or<IOException>()
                .RetryAsync(3, onRetry: (_, i) => Log.Write($"[{i}] Retrying: {api}"))
                .ExecuteAsync(() => SendRequest(api, request));

            if (response.Headers.TryGetValues("X-RateLimit-Remaining", out var remainings))
            {
                Telemetry.TrackGitHubRateLimit(remainings.FirstOrDefault());
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
            {
                Log.Write(await response.Content.ReadAsStringAsync());
                _fatalError = Errors.System.GitHubApiFailed(response.StatusCode.ToString());
                return (_fatalError, default, default);
            }

            var content = await response.EnsureSuccessStatusCode().Content.ReadAsStringAsync();

            var body = JsonConvert.DeserializeAnonymousType(
                content,
                new { data = default(T), errors = new[] { new { type = "", message = "" } } });

            if (body?.errors != null)
            {
                foreach (var error in body.errors)
                {
                    switch (error.type)
                    {
                        case "MAX_NODE_LIMIT_EXCEEDED":
                        case "RATE_LIMITED":
                            _fatalError = Errors.System.GitHubApiFailed($"[{error.type}] {error.message}");
                            return (_fatalError, default, default);

                        default:
                            return (Errors.System.GitHubApiFailed($"[{error.type}] {error.message}"), error.type, default);
                    }
                }
            }

            return (null, null, body?.data);
        }
        catch (Exception ex)
        {
            Log.Write(ex);
            return (Errors.System.GitHubApiFailed(ex.Message), default, default);
        }
    }

    private async Task<HttpResponseMessage> SendRequest(string api, StringContent request)
    {
        await _syncRoot.WaitAsync();
        try
        {
            using (PerfScope.Start($"Calling GitHub API: {api}"))
            {
                return await _httpClient!.PostAsync(s_url, request);
            }
        }
        finally
        {
            _syncRoot.Release();
        }
    }

    private static GitHubUser ResolveGitHubUserConflict(GitHubUser a, GitHubUser b)
    {
        // Pick the user with a valid Id
        if (a.Id is null)
        {
            return b;
        }

        if (b.Id is null)
        {
            return a;
        }

        // otherwise pick the latest one
        return (a.UpdatedAt ?? DateTime.MinValue) >= (b.UpdatedAt ?? DateTime.MinValue) ? a : b;
    }
}
