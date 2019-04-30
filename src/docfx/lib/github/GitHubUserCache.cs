// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
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
    internal class GitHubUserCache : IDisposable
    {
        public IEnumerable<GitHubUser> Users => _usersByLogin.Values.Concat(_usersByEmail.Values).Distinct();

        // calls GitHubAccessor.GetUserByLogin, which only for private use, and tests can swap this out
        internal Func<string, Task<(Error, GitHubUser)>> _getUserByLoginFromGitHub;

        // calls GitHubAccessor.GetLoginByCommit, which ohly for private use, and tests can swap this out
        internal Func<string, string, string, Task<(Error, IEnumerable<GitHubUser>)>> _getUsersByCommitFromGitHub;

        private static int s_randomSeed = Environment.TickCount;
        private static ThreadLocal<Random> t_random = new ThreadLocal<Random>(() => new Random(Interlocked.Increment(ref s_randomSeed)));

        private readonly Dictionary<string, GitHubUser> _usersByLogin = new Dictionary<string, GitHubUser>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, GitHubUser> _usersByEmail = new Dictionary<string, GitHubUser>(StringComparer.OrdinalIgnoreCase);

        private readonly SemaphoreSlim _syncRoot = new SemaphoreSlim(1, 1);

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
            UpdateUsers(users);
        }

        private GitHubUserCache(Docset docset, string cachePath)
        {
            Debug.Assert(!string.IsNullOrEmpty(cachePath));

            var github = new GitHubAccessor(docset.Config.GitHub.AuthToken);
            _getUserByLoginFromGitHub = github.GetUserByLogin;
            _getUsersByCommitFromGitHub = github.GetUsersByCommit;
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
            _etag = EntityTagHeaderValue.Parse(etag);
        }

        public static GitHubUserCache Create(Docset docset)
        {
            var result = Create();
            result.ReadCacheFiles();
            return result;

            GitHubUserCache Create()
            {
                var path = docset.Config.GitHub.UserCache;
                if (string.IsNullOrEmpty(path))
                {
                    return new GitHubUserCache(docset, AppData.DefaultGitHubUserCachePath);
                }

                var (localPath, content, etag) = RestoreMap.GetRestoredFileContent(docset, new SourceInfo<string>(path, docset.Config.GitHub.UserCache));
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

            return Synchronized(GetByLoginCore);

            async Task<(Error error, GitHubUser user)> GetByLoginCore()
            {
                Telemetry.TrackCacheTotalCount(TelemetryName.GitHubUserCache);

                if (_usersByLogin.TryGetValue(login, out var existingUser))
                {
                    if (existingUser.IsValid())
                        return (null, existingUser);
                    return (Errors.GitHubUserNotFound(login), null);
                }

                Log.Write($"Calling GitHub user API to resolve {login}");
                Telemetry.TrackCacheMissCount(TelemetryName.GitHubUserCache);

                var (error, user) = await _getUserByLoginFromGitHub(login);
                if (error is null)
                {
                    if (user is null)
                        error = Errors.GitHubUserNotFound(login);
                    UpdateUser(user ?? new GitHubUser { Login = login });
                }
                return (error, user);
            }
        }

        public Task<(Error error, GitHubUser user)> GetByCommit(string authorEmail, string repoOwner, string repoName, string commitSha)
        {
            if (string.IsNullOrEmpty(authorEmail))
                return default;

            return Synchronized(GetByCommitCore);

            async Task<(Error, GitHubUser)> GetByCommitCore()
            {
                Telemetry.TrackCacheTotalCount(TelemetryName.GitHubUserCache);

                if (_usersByEmail.TryGetValue(authorEmail, out var existingUser) || string.IsNullOrEmpty(repoOwner) || string.IsNullOrEmpty(repoName))
                {
                    if (existingUser?.IsValid() ?? false)
                        return (null, existingUser);
                    return default;
                }

                Log.Write($"Calling GitHub commit API to resolve {authorEmail}");
                Telemetry.TrackCacheMissCount(TelemetryName.GitHubUserCache);

                var (error, users) = await _getUsersByCommitFromGitHub(repoOwner, repoName, commitSha);

                // When GetUserByCommit failed, it could either the commit is not found or the user is not found,
                // only mark the email as invalid when the user is not found
                if (users != null)
                {
                    UpdateUsers(users, preferExistingName: true);
                }

                return (error, _usersByEmail.TryGetValue(authorEmail, out var user) && user.IsValid() ? user : null);
            }
        }

        public Task<Error> SaveChanges(Config config)
        {
            if (!_updated)
            {
                return Task.FromResult<Error>(null);
            }

            return Synchronized(SaveChangesCore);

            async Task<Error> SaveChangesCore()
            {
                var remainingRetries = 3;
                var (error, collide) = await SaveChanges(config, _etag);
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
                    (error, collide) = await SaveChanges(config, response.Headers.ETag);
                }
                return error;
            }
        }

        public void Dispose()
        {
            _syncRoot.Dispose();
        }

        private async Task<(Error error, bool collide)> SaveChanges(Config config, EntityTagHeaderValue etag)
        {
            var file = JsonUtility.Serialize(new GitHubUserCacheFile { Users = Users.ToArray() });

            SaveLocal(file);
            if (config.GitHub.UpdateRemoteUserCache && _url != null)
            {
                return await SaveRemote(file);
            }
            return default;

            void SaveLocal(string content)
            {
                PathUtility.CreateDirectoryFromFilePath(_cachePath);
                ProcessUtility.WriteFile(_cachePath, content);
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

        /// <summary>
        /// Update the cache index by login/email. It can have 3 forms:
        ///
        /// 1. Valid user: { "id": 123, "login": "...", "emails": ["..."] }
        /// 2. User missing with the specified login: { "login": "..." }
        /// 3. User missing with the specified email: { "emails": [ "..." ] }
        /// </summary>
        private void UpdateUser(GitHubUser user, bool preferExistingName = false)
        {
            Debug.Assert(user != null);

            // Ensure `_usersByXXX` only contains none expired users.
            // User expiry is checked only once per application lifecycle.
            if (user.Expiry != null && user.Expiry < DateTime.UtcNow)
            {
                return;
            }

            if (user.Login != null)
            {
                if (_usersByLogin.TryGetValue(user.Login, out var existingUser))
                {
                    MergeUser(user, existingUser, preferExistingName);
                }
            }
            else
            {
                // Trying to find a valid GitHub user through email
                foreach (var email in user.Emails)
                {
                    if (_usersByEmail.TryGetValue(email, out var existingUser))
                    {
                        MergeUser(user, existingUser, preferExistingName);
                        break;
                    }
                }
            }

            if (user.Expiry is null)
                user.Expiry = NextExpiry();

            if (user.Login != null)
                _usersByLogin[user.Login] = user;

            foreach (var email in user.Emails)
                _usersByEmail[email] = user;

            _updated = true;
        }

        private static void MergeUser(GitHubUser user, GitHubUser existingUser, bool preferExistingName)
        {
            if (existingUser.IsValid())
            {
                if (user.Id is null)
                    user.Id = existingUser.Id;
                if (user.Login is null)
                    user.Login = existingUser.Login;
                if (user.Name is null)
                    user.Name = existingUser.Name;
                if (preferExistingName && existingUser.Name != null)
                    user.Name = existingUser.Name;
                user.Emails = user.Emails.Concat(existingUser.Emails).Distinct().ToArray();
            }
        }

        private void UpdateUsers(IEnumerable<GitHubUser> users, bool preferExistingName = false)
        {
            foreach (var user in users)
            {
                UpdateUser(user, preferExistingName);
            }
        }

        private DateTime NextExpiry()
            => DateTime.UtcNow.AddHours((_expirationInHours / 2) + (t_random.Value.NextDouble() * _expirationInHours / 2));

        private void ReadCacheFiles()
        {
            ReadCacheFile(_cachePath);

            if (!string.IsNullOrEmpty(_content))
            {
                ReadCache(_content);
            }

            void ReadCacheFile(string path)
            {
                if (path != null && File.Exists(path))
                {
                    var content = ProcessUtility.ReadFile(path);
                    ReadCache(content);
                }
            }
        }

        private void ReadCache(string content)
        {
            var users = JsonUtility.Deserialize<GitHubUserCacheFile>(content).Users;
            if (users != null)
            {
                UpdateUsers(users);
            }
        }

        private async Task<T> Synchronized<T>(Func<Task<T>> action)
        {
            try
            {
                await _syncRoot.WaitAsync();
                return await action();
            }
            finally
            {
                _syncRoot.Release();
            }
        }
    }
}
