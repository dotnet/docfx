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
        internal Func<string, string, string, Task<(Error, GitHubUser)>> _getUserByCommitFromGitHub;

        private static int s_randomSeed = Environment.TickCount;
        private static ThreadLocal<Random> t_random = new ThreadLocal<Random>(() => new Random(Interlocked.Increment(ref s_randomSeed)));

        private readonly Dictionary<string, GitHubUser> _usersByLogin = new Dictionary<string, GitHubUser>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, GitHubUser> _usersByEmail = new Dictionary<string, GitHubUser>(StringComparer.OrdinalIgnoreCase);

        // A lock to ensure mutations to `_usersByLogin` and `_usersByEmail` is sequential.
        private readonly object _lock = new object();

        // A lock to ensure async operations with the same key are sequential.
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _syncRoots = new ConcurrentDictionary<string, SemaphoreSlim>(StringComparer.OrdinalIgnoreCase);

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
            _getUserByCommitFromGitHub = github.GetUserByCommit;
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

        public Task<(Error error, GitHubUser user)> GetByLogin(string login)
        {
            if (string.IsNullOrEmpty(login))
                return default;

            return Synchronized(login, GetByLoginCore);

            async Task<(Error error, GitHubUser user)> GetByLoginCore()
            {
                Telemetry.TrackCacheTotalCount(TelemetryName.GitHubUserCache);
                var existingUser = TryGetByLogin(login);
                if (existingUser != null)
                    return (null, existingUser.IsValid() ? existingUser : null);

                Log.Write($"Calling GitHub user API to resolve {login}");
                Telemetry.TrackCacheMissCount(TelemetryName.GitHubUserCache);

                var (error, user) = await _getUserByLoginFromGitHub(login);
                if (error == null)
                {
                    if (user == null)
                        error = Errors.GitHubUserNotFound(login);
                    UpdateUser(user ?? new GitHubUser { Login = login });
                }
                return (error, user);
            }
        }

        public Task<(Error, GitHubUser)> GetByCommit(string authorEmail, string repoOwner, string repoName, string commitSha)
        {
            if (string.IsNullOrEmpty(authorEmail))
                return default;

            return Synchronized(authorEmail, GetByCommitCore);

            async Task<(Error, GitHubUser)> GetByCommitCore()
            {
                Telemetry.TrackCacheTotalCount(TelemetryName.GitHubUserCache);
                var existingUser = TryGetByEmail(authorEmail);
                if (existingUser != null)
                    return (null, existingUser.IsValid() ? existingUser : null);

                Log.Write($"Calling GitHub commit API to resolve {authorEmail}");
                Telemetry.TrackCacheMissCount(TelemetryName.GitHubUserCache);

                var (error, user) = await _getUserByCommitFromGitHub(repoOwner, repoName, commitSha);
                if (error == null)
                {
                    UpdateUser(user ?? new GitHubUser { Emails = new[] { authorEmail } });
                }
                return (error, user);
            }
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
        /// Update the cache index by login/email. It can have 3 forms:
        ///
        /// 1. Valid user: { "id": 123, "login": "...", "emails": ["..."] }
        /// 2. User missing with the specified login: { "login": "..." }
        /// 3. User missing with the specified email: { "emails": [ "..." ] }
        /// </summary>
        private void UnsafeUpdateUser(GitHubUser user)
        {
            Debug.Assert(user != null);

            if (user.IsExpired())
                return;

            if (user.Login != null &&
                _usersByLogin.TryGetValue(user.Login, out var existingUser) &&
                !existingUser.IsExpired())
            {
                if (user.Id == null)
                    user.Id = existingUser.Id;
                if (user.Login == null)
                    user.Login = existingUser.Login;
                if (user.Name == null)
                    user.Name = existingUser.Name;
                user.Emails = user.Emails.Concat(existingUser.Emails).Distinct().ToArray();
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

        private async Task<T> Synchronized<T>(string key, Func<Task<T>> action)
        {
            var semaphore = _syncRoots.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));

            try
            {
                await semaphore.WaitAsync();
                return await action();
            }
            finally
            {
                if (semaphore.Release() == 0)
                {
                    _syncRoots.TryRemove(key, out _);
                }
            }
        }
    }
}
