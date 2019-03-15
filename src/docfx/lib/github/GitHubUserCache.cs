// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal class GitHubUserCache
    {
        public IEnumerable<GitHubUser> Users => _usersByLogin.Values.Concat(_usersByEmail.Values).Distinct();

        // calls GitHubAccessor.GetUserByLogin, which only for private use, and tests can swap this out
        internal Func<string, Task<(Error, GitHubUser)>> _getUserByLoginFromGitHub;

        // calls GitHubAccessor.GetLoginByCommit, which ohly for private use, and tests can swap this out
        internal Func<string, string, string, Task<(Error, string)>> _getLoginByCommitFromGitHub;

        private static int s_randomSeed = Environment.TickCount;
        private static ThreadLocal<Random> t_random = new ThreadLocal<Random>(() => new Random(Interlocked.Increment(ref s_randomSeed)));

        private readonly object _lock = new object();
        private readonly Dictionary<string, GitHubUser> _usersByLogin = new Dictionary<string, GitHubUser>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, GitHubUser> _usersByEmail = new Dictionary<string, GitHubUser>(StringComparer.OrdinalIgnoreCase);

        // Ensures we only call GitHub once for parallel requests with same input parameter
        private readonly ConcurrentDictionary<string, Lazy<Task<(Error, GitHubUser)>>> _outgoingGetUserByLoginRequests
                   = new ConcurrentDictionary<string, Lazy<Task<(Error, GitHubUser)>>>(StringComparer.OrdinalIgnoreCase);

        private readonly ConcurrentDictionary<string, Lazy<Task<(Error, string)>>> _outgoingGetLoginByCommitRequests
                   = new ConcurrentDictionary<string, Lazy<Task<(Error, string)>>>(StringComparer.OrdinalIgnoreCase);

        private readonly string _url = null;
        private readonly string _content = null;
        private readonly EntityTagHeaderValue _etag = null;
        private readonly string _cachePath;
        private readonly double _expirationInHours;
        private bool _updated = false;

        /// <summary>
        /// Only for test purpose
        /// </summary>
        internal GitHubUserCache(GitHubUser[] users, string cachePath, double expirationInHours)
        {
            _cachePath = cachePath;
            _expirationInHours = expirationInHours;
            UnsafeUpdateUsers(users);
        }

        private GitHubUserCache(Docset docset, string cachePath)
        {
            Debug.Assert(!string.IsNullOrEmpty(cachePath));

            var github = new GitHubAccessor(docset.Config.GitHub.AuthToken);
            _getUserByLoginFromGitHub = github.GetUserByLogin;
            _getLoginByCommitFromGitHub = github.GetLoginByCommit;
            _expirationInHours = docset.Config.GitHub.UserCacheExpirationInHours;
            _cachePath = cachePath;
        }

        private GitHubUserCache(Docset docset, string url, string content, string etag, string cachePath)
            : this(docset, cachePath)
        {
            Debug.Assert(!string.IsNullOrEmpty(url));
            Debug.Assert(!string.IsNullOrEmpty(content));
            Debug.Assert(!string.IsNullOrEmpty(etag));

            _url = url;
            _content = content;
            _etag = new EntityTagHeaderValue(etag);
        }

        public static async Task<GitHubUserCache> Create(Docset docset)
        {
            var result = await Create();
            await result.ReadCacheFiles();
            return result;

            async Task<GitHubUserCache> Create()
            {
                var path = docset.Config.GitHub.UserCache;
                if (string.IsNullOrEmpty(path))
                {
                    return new GitHubUserCache(docset, AppData.DefaultGitHubUserCachePath);
                }

                var (localPath, content, etag) = await RestoreMap.GetRestoredFileContent(docset, path);
                if (string.IsNullOrEmpty(localPath))
                {
                    return new GitHubUserCache(docset, path, content, etag, AppData.GetGitHubUserCachePath(path));
                }

                return new GitHubUserCache(docset, localPath);
            }
        }

        public async Task<(Error error, GitHubUser user)> GetByLogin(string login)
        {
            Error error;

            if (string.IsNullOrEmpty(login))
                return default;

            Telemetry.TrackCacheTotalCount(TelemetryName.GitHubUserCache);
            var user = TryGetByLogin(login);
            if (user != null)
            {
                if (user.IsValid())
                    return ((Error)null, user);
                if (!user.IsPartial())
                    return (Errors.GitHubUserNotFound(login), null);
            }

            (error, user) = await _outgoingGetUserByLoginRequests.GetOrAdd(
                login,
                new Lazy<Task<(Error, GitHubUser)>>(() =>
                {
                    Telemetry.TrackCacheMissCount(TelemetryName.GitHubUserCache);
                    return _getUserByLoginFromGitHub(login);
                })).Value;

            if (error == null)
            {
                if (user == null)
                    (error, user) = (Errors.GitHubUserNotFound(login), new GitHubUser { Login = login });
                user.Expiry = NextExpiry();
                UpdateUser(user);
            }

            if (error != null)
                return (error, null);

            return (null, TryGetByLogin(login));
        }

        public async Task<(Error, GitHubUser)> GetByEmailOrCommit(string authorEmail, string repoOwner, string repoName, string commitSha, bool isGitHubRepo = true)
        {
            if (string.IsNullOrEmpty(authorEmail))
                return default;

            Telemetry.TrackCacheTotalCount(TelemetryName.GitHubUserCache);
            var user = TryGetByEmail(authorEmail);
            if (user != null || !isGitHubRepo)
                return (null, (user?.IsValid() ?? false) ? user : null);

            var (error, login) = await _outgoingGetLoginByCommitRequests.GetOrAdd(
                commitSha,
                new Lazy<Task<(Error, string)>>(() =>
                {
                    Telemetry.TrackCacheMissCount(TelemetryName.GitHubUserCache);
                    return _getLoginByCommitFromGitHub(repoOwner, repoName, commitSha);
                })).Value;

            if (error == null)
                UpdateUser(new GitHubUser { Login = login, Emails = new[] { authorEmail }, Expiry = NextExpiry() });

            if (login == null)
                return (error, null);

            return await GetByLogin(login);
        }

        public async Task<Error> SaveChanges(Config config)
        {
            if (!_updated)
            {
                return null;
            }

            var remainingRetries = 3;
            var (error, collide) = await SaveChangesCore(config, _etag);
            while (collide && remainingRetries-- > 0)
            {
                HttpResponseMessage response;
                try
                {
                    response = await HttpClientUtility.GetAsync(_url, config);
                }
                catch (Exception ex)
                {
                    throw Errors.DownloadFailed(_url, ex.Message).ToException(ex);
                }
                var content = await response.EnsureSuccessStatusCode().Content.ReadAsStringAsync();
                ReadCache(content);
                (error, collide) = await SaveChangesCore(config, response.Headers.ETag);
            }
            return error;
        }

        public async Task<(Error error, bool collide)> SaveChangesCore(Config config, EntityTagHeaderValue etag)
        {
            string file;
            lock (_lock)
            {
                file = JsonUtility.Serialize(new GitHubUserCacheFile
                {
                    Users = Users.ToArray(),
                });
            }

            await SaveLocal(file);
            if (config.GitHub.UpdateRemoteUserCache && _url != null)
            {
                return await SaveRemote(file);
            }
            return default;

            async Task SaveLocal(string content)
            {
                PathUtility.CreateDirectoryFromFilePath(_cachePath);
                await ProcessUtility.WriteFile(_cachePath, content);
            }

            async Task<(Error error, bool collide)> SaveRemote(string content)
            {
                try
                {
                    var response = await HttpClientUtility.PutAsync(_url, new StringContent(file), config, etag);
                    if (response.IsSuccessStatusCode)
                    {
                        return (null, false);
                    }
                    if (response.StatusCode == HttpStatusCode.PreconditionFailed)
                    {
                        return (null, true);
                    }
                    else
                    {
                        return (Errors.UploadFailed(_url, response.ReasonPhrase), false);
                    }
                }
                catch (Exception ex)
                {
                    return (Errors.UploadFailed(_url, ex.Message), false);
                }
            }
        }

        private GitHubUser TryGetByLogin(string login)
        {
            Debug.Assert(!string.IsNullOrEmpty(login));

            lock (_lock)
            {
                return _usersByLogin.TryGetValue(login, out var user) && !user.IsExpired() ? user : null;
            }
        }

        private GitHubUser TryGetByEmail(string email)
        {
            Debug.Assert(!string.IsNullOrEmpty(email));

            lock (_lock)
            {
                return _usersByEmail.TryGetValue(email, out var user) && !user.IsExpired() ? user : null;
            }
        }

        private void UpdateUser(GitHubUser user)
        {
            lock (_lock)
            {
                UnsafeUpdateUser(user);

                _updated = true;
            }
        }

        /// <summary>
        /// Update the cache index by login/email. It can have several useages:
        ///
        /// 1. Get user from GitHub Users API. It has 2 forms depending on whether the user is valid:
        ///   1.1 Valid user: { "id": 123, "login": "...", "emails": ["..."] }
        ///   1.2 Invalid User: { "login": "..." }
        ///
        /// 2. Get login-email matching from GitHub Commits API. It has 2 forms depending on whether matching is got:
        ///   2.1 Valid login-email pair (partial user): { "login": "...", "emails": [ "..." ] }
        ///   2.2 Invalid email: { "emails": [ "..." ] }
        ///
        /// 3. Construct cache when first read from disk. It can be one of the following formats:
        ///   - Valid user (See 1.1)
        ///   - Invalid user (See 1.2)
        ///   - Invalid email (See 2.2)
        /// </summary>
        private void UnsafeUpdateUser(GitHubUser user)
        {
            Debug.Assert(user != null);
            if (user.IsExpired())
                return;

            if (user.Login != null
                && _usersByLogin.TryGetValue(user.Login, out var existingUser)
                && !existingUser.IsExpired())
            {
                existingUser.Merge(user);
                user = existingUser;
            }

            if (user.Expiry == null)
                user.Expiry = NextExpiry();

            if (user.Login != null)
                _usersByLogin[user.Login] = user;

            foreach (var email in user.Emails)
                _usersByEmail[email] = user;
        }

        private DateTime NextExpiry()
            => DateTime.UtcNow.AddHours((_expirationInHours / 2) + (t_random.Value.NextDouble() * _expirationInHours / 2));

        private async Task ReadCacheFiles()
        {
            await ReadCacheFile(_cachePath);

            if (!string.IsNullOrEmpty(_content))
            {
                ReadCache(_content);
            }

            async Task ReadCacheFile(string path)
            {
                if (path != null && File.Exists(path))
                {
                    var content = await ProcessUtility.ReadFile(path);
                    ReadCache(content);
                }
            }
        }

        private void ReadCache(string content)
        {
            var users = JsonUtility.Deserialize<GitHubUserCacheFile>(content).Users;
            if (users != null)
            {
                lock (_lock)
                {
                    UnsafeUpdateUsers(users);
                }
            }
        }

        private void UnsafeUpdateUsers(GitHubUser[] users)
        {
            foreach (var user in users)
            {
                UnsafeUpdateUser(user);
            }
        }
    }
}
