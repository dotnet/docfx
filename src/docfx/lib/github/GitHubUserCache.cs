// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
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

        private GitHubUserCache(Docset docset, string token)
        {
            var github = new GitHubAccessor(token);
            _getUserByLoginFromGitHub = github.GetUserByLogin;
            _getLoginByCommitFromGitHub = github.GetLoginByCommit;
            _cachePath = string.IsNullOrEmpty(docset.Config.GitHub.UserCache)
                ? AppData.GitHubUserCachePath
                : docset.GetFileRestorePath(docset.Config.GitHub.UserCache);
            _expirationInHours = docset.Config.GitHub.UserCacheExpirationInHours;
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
            {
                if (user.IsValid())
                    return ((Error)null, user);
                if (!user.IsPartial())
                    return (Errors.GitHubUserNotFound(login), null);
            }

            (error, user) = await _outgoingGetUserByLoginRequests.GetOrAdd(
                login,
                new Lazy<Task<(Error, GitHubUser)>>(
                    () => _getUserByLoginFromGitHub(login))).Value;

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

        public async Task<(Error, GitHubUser)> GetByCommit(string authorEmail, string repoOwner, string repoName, string commitSha)
        {
            if (string.IsNullOrEmpty(authorEmail))
                return default;

            var user = TryGetByEmail(authorEmail);
            if (user != null)
                return (null, user.IsValid() ? user : null);

            var (error, login) = await _outgoingGetLoginByCommitRequests.GetOrAdd(
                commitSha,
                new Lazy<Task<(Error, string)>>(
                    () => _getLoginByCommitFromGitHub(repoOwner, repoName, commitSha))).Value;

            if (error == null)
                UpdateUser(new GitHubUser { Login = login, Emails = new[] { authorEmail }, Expiry = NextExpiry() });

            if (login == null)
                return (error, null);

            return await GetByLogin(login);
        }

        public async Task SaveChanges(Config config)
        {
            if (!_updated)
            {
                return;
            }

            string file;
            lock (_lock)
            {
                file = JsonUtility.Serialize(new GitHubUserCacheFile
                {
                    Users = Users.ToArray(),
                });
            }
            await ProcessUtility.WriteFile(_cachePath, file);
            var url = config.GitHub.UserCache;
            if (config.GitHub.UpdateRemoteUserCache && HrefUtility.IsHttpHref(url))
            {
                try
                {
                    // TOOD: aware of ETag to handle conflicts
                    await HttpClientUtility.PutAsync(config.GitHub.UserCache, new StringContent(file), config);
                }
                catch (HttpRequestException ex)
                {
                    throw Errors.UploadFailed(url, ex.Message).ToException();
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
            => DateTime.UtcNow.AddHours((_expirationInHours / 2) + (t_random.Value.NextDouble() * _expirationInHours));

        private async Task ReadCacheFile()
        {
            if (File.Exists(_cachePath))
            {
                var content = await ProcessUtility.ReadFile(_cachePath);
                var users = JsonUtility.Deserialize<GitHubUserCacheFile>(content).Users;
                if (users != null)
                {
                    lock (_lock)
                    {
                        UnsafeUpdateUsers(users);
                    }
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
