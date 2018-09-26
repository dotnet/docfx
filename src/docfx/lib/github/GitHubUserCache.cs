// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal class GitHubUserCache
    {
        private static int s_randomSeed = Environment.TickCount;
        private static ThreadLocal<Random> t_random = new ThreadLocal<Random>(() => new Random(Interlocked.Increment(ref s_randomSeed)));

        private readonly object _lock = new object();
        private readonly Dictionary<string, GitHubUser> _usersByLogin = new Dictionary<string, GitHubUser>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, GitHubUser> _usersByEmail = new Dictionary<string, GitHubUser>(StringComparer.OrdinalIgnoreCase);

        // Ensures we only call GitHub once for parallel requests with same input parameter
        private readonly ConcurrentDictionary<string, Task<(Error, GitHubUser)>> _outgoingGetUserByLoginRequests
                   = new ConcurrentDictionary<string, Task<(Error, GitHubUser)>>(StringComparer.OrdinalIgnoreCase);

        private readonly ConcurrentDictionary<string, Task<(Error, string)>> _outgoingGetLoginByCommitRequests
                   = new ConcurrentDictionary<string, Task<(Error, string)>>(StringComparer.OrdinalIgnoreCase);

        private readonly Config _config;
        private readonly GitHubAccessor _github;
        private readonly string _cachePath;
        private bool _updated = false;

        private GitHubUserCache(Docset docset, string token)
        {
            _config = docset.Config;
            _github = new GitHubAccessor(token);
            _cachePath = string.IsNullOrEmpty(_config.GitHub.UserCache)
                ? Path.Combine(AppData.CacheDir, "github-users.json")
                : docset.RestoreMap.GetUrlRestorePath(docset.DocsetPath, _config.GitHub.UserCache);
        }

        public static async Task<GitHubUserCache> Create(Docset docset, string token)
        {
            var result = new GitHubUserCache(docset, token);
            await result.ReadCacheFile();
            return result;
        }

        public async Task<(Error error, GitHubUser user)> GetByLogin(string login)
        {
            Error error;

            if (string.IsNullOrEmpty(login))
                return default;

            var user = TryGetByLogin(login);
            if (user != null)
                return (null, user.IsValid() ? user : null);

            (error, user) = await _outgoingGetUserByLoginRequests.GetOrAdd(login, _ => _github.GetUserByLogin(login));
            _outgoingGetUserByLoginRequests.TryRemove(login, out _);

            if (user != null)
                UpdateUser(user);

            if (error != null)
                return (error, null);

            return (null, TryGetByLogin(login));
        }

        public async Task<(Error, GitHubUser)> GetByCommit(string authorEmail, string repoOwner, string repoName, string commitSha)
        {
            if (string.IsNullOrEmpty(authorEmail))
                return default;

            var user = TryGetByEmail(authorEmail);
            if (user != null)
                return (null, user.IsValid() ? user : null);

            var (error, login) = await _outgoingGetLoginByCommitRequests.GetOrAdd(commitSha, _ => _github.GetLoginByCommit(repoOwner, repoName, commitSha));
            _outgoingGetLoginByCommitRequests.TryRemove(commitSha, out _);

            UpdateUser(new GitHubUser { Login = login, Emails = new[] { authorEmail } });

            if (login == null)
                return (error, null);

            return await GetByLogin(login);
        }

        public Task SaveChanges()
        {
            return _updated ? ProcessUtility.RunInsideMutex(_cachePath, WriteCache) : Task.CompletedTask;

            Task WriteCache()
            {
                lock (_lock)
                {
                    var users = _usersByLogin.Values.Concat(_usersByEmail.Values).Distinct().ToList();
                    JsonUtility.WriteJsonFile(_cachePath, users);
                }
                return Task.CompletedTask;
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
        ///   2.1 Valid login-email pair: { "login": "...", "emails": [ "..." ] }
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

            if (IsValidLoginEmailPair(user) && _usersByLogin.TryGetValue(user.Login, out var existingUser) && !existingUser.IsExpired())
            {
                existingUser.Emails = existingUser.Emails.Concat(user.Emails).Distinct().ToArray();
                user = existingUser;
            }

            if (user.Expiry == null)
                user.Expiry = NextExpiry();

            if (user.Login != null)
                _usersByLogin[user.Login] = user;

            foreach (var email in user.Emails)
                _usersByEmail[email] = user;

            bool IsValidLoginEmailPair(GitHubUser u) => !string.IsNullOrEmpty(u.Login) && u.Emails.Length > 0;
        }

        private DateTime NextExpiry()
        {
            var expirationInHours = (double)_config.GitHub.UserCacheExpirationInHours;

            return DateTime.UtcNow.AddHours((expirationInHours / 2) + (t_random.Value.NextDouble() * expirationInHours));
        }

        private Task ReadCacheFile()
        {
            return ProcessUtility.RunInsideMutex(_cachePath, UpdateCache);

            Task UpdateCache()
            {
                if (File.Exists(_cachePath))
                {
                    var users = JsonUtility.ReadJsonFile<GitHubUserCacheFile>(_cachePath)?.Users;
                    if (users != null)
                    {
                        lock (_lock)
                        {
                            foreach (var user in users)
                            {
                                UnsafeUpdateUser(user);
                            }
                        }
                    }
                }
                return Task.CompletedTask;
            }
        }
    }
}
