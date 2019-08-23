// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal class GitHubUserCache : IDisposable
    {
        public IEnumerable<GitHubUser> Users => _usersByLogin.Values.Concat(_usersByEmail.Values).Distinct();

        private readonly Dictionary<string, GitHubUser> _usersByLogin
            = new Dictionary<string, GitHubUser>(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, GitHubUser> _usersByEmail
            = new Dictionary<string, GitHubUser>(StringComparer.OrdinalIgnoreCase);

        private readonly SemaphoreSlim _syncRoot = new SemaphoreSlim(1, 1);

        private readonly GitHubAccessor _githubAccessor;
        private readonly string _cachePath;
        private readonly double _expirationInHours;

        // calls GitHubAccessor.GetUserByLogin, which only for private use, and tests can swap this out
        internal Func<string, Task<(Error, GitHubUser)>> _getUserByLoginFromGitHub;

        // calls GitHubAccessor.GetLoginByCommit, which ohly for private use, and tests can swap this out
        internal Func<string, string, string, string, Task<(Error, IEnumerable<GitHubUser>)>> _getUsersByCommitFromGitHub;

        private bool _updated = false;

        public GitHubUserCache(Config config)
        {
            _cachePath = AppData.GitHubUserCachePath;
            _expirationInHours = config.GitHub.UserCacheExpirationInHours;

            if (!string.IsNullOrEmpty(config.GitHub.AuthToken))
            {
                _githubAccessor = new GitHubAccessor(config.GitHub.AuthToken);
                _getUserByLoginFromGitHub = _githubAccessor.GetUserByLogin;
                _getUsersByCommitFromGitHub = _githubAccessor.GetUsersByCommit;
            }

            if (File.Exists(_cachePath))
            {
                var cache = JsonUtility.Deserialize<GitHubUserCacheFile>(ProcessUtility.ReadFile(_cachePath), new FilePath(_cachePath));
                UpdateUsers(cache.Users);
            }
        }

        /// <summary>
        /// Only for test purpose
        /// </summary>
        internal GitHubUserCache(GitHubUser[] users, string cachePath, double expirationInHours)
        {
            _cachePath = cachePath;
            _expirationInHours = expirationInHours;
            UpdateUsers(users);
        }

        public Task<(Error error, GitHubUser user)> GetByLogin(SourceInfo<string> login)
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
                    return (Errors.AuthorNotFound(login), null);
                }

                Telemetry.TrackCacheMissCount(TelemetryName.GitHubUserCache);

                if (_getUserByLoginFromGitHub == null)
                {
                    return default;
                }

                var (error, user) = await _getUserByLoginFromGitHub.Invoke(login);
                if (error is null)
                {
                    if (user is null)
                        error = Errors.AuthorNotFound(login);
                    UpdateUser(user ?? new GitHubUser { Login = login });
                }
                return (error, user);
            }
        }

        public bool GetByEmail(string authorEmail, out GitHubUser githubUser)
            => _usersByEmail.TryGetValue(authorEmail, out githubUser) && githubUser.IsValid();

        public Task<(Error error, GitHubUser user)> GetByCommit(string authorEmail, string repoOwner, string repoName, string commitSha)
        {
            if (string.IsNullOrEmpty(authorEmail))
                return default;

            return Synchronized(GetByCommitCore);

            async Task<(Error, GitHubUser)> GetByCommitCore()
            {
                Telemetry.TrackCacheTotalCount(TelemetryName.GitHubUserCache);

                if (_usersByEmail.TryGetValue(authorEmail, out var existingUser)
                    || string.IsNullOrEmpty(repoOwner) || string.IsNullOrEmpty(repoName))
                {
                    if (existingUser?.IsValid() ?? false)
                        return (null, existingUser);
                    return default;
                }

                Telemetry.TrackCacheMissCount(TelemetryName.GitHubUserCache);

                if (_getUsersByCommitFromGitHub == null)
                {
                    return default;
                }

                var (error, users) = await _getUsersByCommitFromGitHub(repoOwner, repoName, commitSha, authorEmail);

                // When GetUserByCommit failed, it could either the commit is not found or the user is not found,
                // only mark the email as invalid when the user is not found
                if (users != null)
                {
                    UpdateUsers(users);
                }

                return (error, _usersByEmail.TryGetValue(authorEmail, out var user) && user.IsValid() ? user : null);
            }
        }

        public void Save()
        {
            if (_updated)
            {
                _syncRoot.Wait();

                try
                {
                    var content = JsonUtility.Serialize(new GitHubUserCacheFile { Users = Users.ToArray() });

                    PathUtility.CreateDirectoryFromFilePath(_cachePath);
                    ProcessUtility.WriteFile(_cachePath, content);
                    _updated = false;
                }
                finally
                {
                    _syncRoot.Release();
                }
            }
        }

        public void Dispose()
        {
            _githubAccessor?.Dispose();
            _syncRoot.Dispose();
        }

        /// <summary>
        /// Update the cache index by login/email. It can have 3 forms:
        ///
        /// 1. Valid user: { "id": 123, "login": "...", "emails": ["..."] }
        /// 2. User missing with the specified login: { "login": "..." }
        /// 3. User missing with the specified email: { "emails": [ "..." ] }
        /// </summary>
        private void UpdateUser(GitHubUser user)
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
                    MergeUser(user, existingUser);
                }
            }
            else
            {
                // Trying to find a valid GitHub user through email
                foreach (var email in user.Emails)
                {
                    if (_usersByEmail.TryGetValue(email, out var existingUser))
                    {
                        MergeUser(user, existingUser);
                        break;
                    }
                }
            }

            if (user.Expiry is null)
                user.Expiry = DateTime.UtcNow.AddHours(RandomUtility.NextEvenDistribution(_expirationInHours));

            if (user.Login != null)
                _usersByLogin[user.Login] = user;

            foreach (var email in user.Emails)
                _usersByEmail[email] = user;

            _updated = true;
        }

        private static void MergeUser(GitHubUser user, GitHubUser existingUser)
        {
            if (existingUser.IsValid())
            {
                if (user.Id is null)
                    user.Id = existingUser.Id;
                if (user.Login is null)
                    user.Login = existingUser.Login;
                if (user.Name is null)
                    user.Name = existingUser.Name;
                user.Emails = user.Emails.Concat(existingUser.Emails).Distinct().ToArray();
            }
        }

        private void UpdateUsers(IEnumerable<GitHubUser> users)
        {
            foreach (var user in users)
            {
                UpdateUser(user);
            }
        }

        private async Task<T> Synchronized<T>(Func<Task<T>> action)
        {
            await _syncRoot.WaitAsync();
            try
            {
                return await action();
            }
            finally
            {
                _syncRoot.Release();
            }
        }
    }
}
