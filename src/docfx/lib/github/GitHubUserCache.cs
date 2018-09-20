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

        private GitHubUserCache(Config config, string token)
        {
            _config = config;
            _github = new GitHubAccessor(token);
            _cachePath = string.IsNullOrEmpty(config.Contribution.GitHubUserCache)
                ? Path.Combine(AppData.CacheDir, "github-users.json")
                : Path.GetFullPath(config.Contribution.GitHubUserCache);
        }

        public static async Task<GitHubUserCache> Create(Config config, string token)
        {
            var result = new GitHubUserCache(config, token);
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
                return (null, user);

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
                return (null, user);

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
                return _usersByLogin.TryGetValue(login, out var user) && user.IsValid() ? user : null;
            }
        }

        private GitHubUser TryGetByEmail(string email)
        {
            Debug.Assert(!string.IsNullOrEmpty(email));

            lock (_lock)
            {
                return _usersByEmail.TryGetValue(email, out var user) && user.IsValid() ? user : null;
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

        private void UnsafeUpdateUser(GitHubUser user)
        {
            Debug.Assert(user != null);
            Debug.Assert(!string.IsNullOrEmpty(user.Login) || user.Emails.Length > 0);

            if (user.Login != null && _usersByLogin.TryGetValue(user.Login, out var existingUser) && existingUser.IsValid())
            {
                existingUser.Emails = existingUser.Emails.Concat(user.Emails).Distinct().ToArray();
                user = existingUser;
            }

            user.Expiry = NextExpiry();

            if (user.Login != null)
            {
                _usersByLogin[user.Login] = user;
            }

            foreach (var email in user.Emails)
            {
                _usersByEmail[email] = user;
            }
        }

        private DateTime NextExpiry()
        {
            var expirationInHours = (double)_config.Contribution.GitHubUserCacheExpirationInHours;

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
