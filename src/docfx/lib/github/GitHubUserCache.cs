// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal class GitHubUserCache
    {
        // Default user cache expiration is a week
        private static readonly int s_userExpiry = int.TryParse(
            Environment.GetEnvironmentVariable("DOCFX_GITHUB_USER_EXPIRY_MINUTES"), out var n) ? n : 7 * 24 * 60;

        private static int s_randomSeed = Environment.TickCount;
        private static ThreadLocal<Random> t_random = new ThreadLocal<Random>(() => new Random(Interlocked.Increment(ref s_randomSeed)));

        private readonly object _lock = new object();
        private readonly Dictionary<string, GitHubUser> _usersByLogin = new Dictionary<string, GitHubUser>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, GitHubUser> _usersByEmail = new Dictionary<string, GitHubUser>(StringComparer.OrdinalIgnoreCase);
        private readonly GitHubAccessor _github;

        public GitHubUserCache(string token = null) => _github = new GitHubAccessor(token);

        public async Task<(Error error, GitHubUser user)> GetByLogin(string login)
        {
            Error error;

            var user = TryGetByLogin(login);
            if (user != null)
                return (null, user);

            (error, user) = await _github.GetUserByLogin(login);

            if (user != null)
                Update(user);

            if (error != null)
                return (error, null);

            return (null, TryGetByLogin(login));
        }

        public async Task<(Error, GitHubUser)> GetByCommit(string authorEmail, string repoOwner, string repoName, string commitSha)
        {
            var user = TryGetByEmail(authorEmail);
            if (user != null)
                return (null, user);

            var (error, login) = await _github.GetLoginByCommit(repoOwner, repoName, commitSha);

            Update(new GitHubUser { Login = login, Emails = new[] { authorEmail } });

            if (login == null)
                return (error, null);

            return await GetByLogin(login);
        }

        public void Update(IEnumerable<GitHubUser> users)
        {
            lock (_lock)
            {
                foreach (var user in users)
                {
                    UnsafeUpdate(user);
                }
            }
        }

        public List<GitHubUser> Export()
        {
            lock (_lock)
            {
                return _usersByLogin.Values.Concat(_usersByEmail.Values).Distinct().ToList();
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

        private void Update(GitHubUser user)
        {
            lock (_lock)
            {
                UnsafeUpdate(user);
            }
        }

        private void UnsafeUpdate(GitHubUser user)
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

        private static DateTime NextExpiry()
        {
            return DateTime.UtcNow.AddMinutes((s_userExpiry / 2) + t_random.Value.Next(s_userExpiry));
        }
    }
}
