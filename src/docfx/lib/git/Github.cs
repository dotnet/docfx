// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Octokit;

namespace Microsoft.Docs
{
    internal static class GitHub
    {
        private static readonly ProductHeaderValue s_header = new ProductHeaderValue("ops", "1.0");
        private static readonly ConcurrentDictionary<string, GitHubUser> s_usersByLogin = new ConcurrentDictionary<string, GitHubUser>(StringComparer.OrdinalIgnoreCase);
        private static readonly ConcurrentDictionary<string, GitHubUser> s_usersByEmail = new ConcurrentDictionary<string, GitHubUser>(StringComparer.OrdinalIgnoreCase);
        private static readonly ConcurrentDictionary<string, GitHubUser> s_usersByName = new ConcurrentDictionary<string, GitHubUser>(StringComparer.OrdinalIgnoreCase);

        static GitHub() => LoadProfileCache();

        public static bool TryParse(string remote, out (string owner, string name, string fragment) info)
        {
            if (Uri.TryCreate(remote, UriKind.Absolute, out var uri) && uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase))
            {
                var segments = uri.LocalPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (segments.Length >= 2)
                {
                    info = (segments[0], segments[1], uri.Fragment.Length > 0 ? uri.Fragment.Substring(1) : null);
                    return true;
                }
            }
            info = default;
            return false;
        }

        public static GitHubUser GetUser(string name)
            => s_usersByLogin.TryGetValue(name, out var user) ? user :
               s_usersByEmail.TryGetValue(name, out user) ? user :
               s_usersByName.TryGetValue(name, out user) ? user : null;

        public static Task<List<GitHubUser>> GetUsers(List<GitCommit> commits, int max = int.MaxValue, string[] excludes = null, string[] tokenPool = null)
        {
            var res = new List<GitHubUser>();
            var github = new GitHubClient(s_header, new CredentialStore(tokenPool));

            foreach (var commit in commits)
            {
                if (s_usersByEmail.TryGetValue(commit.AuthorEmail ?? "", out var author) ||
                    s_usersByName.TryGetValue(commit.AuthorName ?? "", out author))
                {
                    if (!res.Contains(author) && (excludes == null || !excludes.Contains(author.Login)))
                    {
                        res.Add(author);
                        if (res.Count >= max)
                        {
                            break;
                        }
                    }
                }
            }
            return Task.FromResult(res);
        }

        private static async Task<(GitHubUser author, GitHubUser committer)> GetUserFromCommit(string owner, string name, string reference, GitHubClient github)
        {
            const int retry = 5;
            for (var i = 0; i <= 5; i++)
            {
                try
                {
                    var commit = await github.Repository.Commit.Get(owner, name, reference);
                    return (ToUser(commit?.Author, commit.Commit.Author), ToUser(commit?.Committer, commit.Commit.Committer));
                }
                catch (ApiException e) when (IsNonRetryable(e.StatusCode))
                {
                    throw;
                }
                catch when (i != retry)
                {
                    await Task.Delay(1000);
                }
            }
            throw new Exception("should never reach here");

            bool IsNonRetryable(HttpStatusCode code) => code == HttpStatusCode.NotFound || code == HttpStatusCode.Forbidden;
        }

        private static GitHubUser ToUser(Author author, Committer committer) => author == null ? null : new GitHubUser
        {
            Login = author.Login,
            Name = committer.Name,
            Email = committer.Email,
        };

        private static void LoadProfileCache()
        {
            // This is a temporary solution to github profile cache, it is read-only.
            // In the future, it should be read/write to a cache component
            var dir = AppContext.BaseDirectory;
            var cache = JObject.Parse(File.ReadAllText(Path.Combine(dir, "data/github-user.json")));
            foreach (var (login, value) in cache)
            {
                var name = value.Value<string>("display_name") ?? value.Value<string>("name");
                var email = value.Value<string>("email_address");
                var emails = value.Value<string>("user_emails")?.Split(';', StringSplitOptions.RemoveEmptyEntries);
                if (string.IsNullOrEmpty(name))
                {
                    continue;
                }
                var user = new GitHubUser { Login = login, Email = email, Name = name };
                s_usersByLogin.TryAdd(user.Login, user);
                s_usersByName.TryAdd(user.Name, user);
                if (!string.IsNullOrEmpty(user.Email))
                {
                    s_usersByEmail.TryAdd(user.Email, user);
                }
                if (emails != null && emails.Length > 1)
                {
                    for (var i = 1; i < emails.Length; i++)
                        s_usersByEmail.TryAdd(emails[i], user);
                }
            }
        }

        private class CredentialStore : ICredentialStore
        {
            private readonly Credentials[] _credentials;
            private int _i = Environment.TickCount;

            public CredentialStore(IReadOnlyList<string> tokenPool)
                => _credentials = (tokenPool ?? Array.Empty<string>()).Select(token => new Credentials(token)).ToArray();

            public Task<Credentials> GetCredentials()
                => Task.FromResult(_credentials.Length <= 0
                    ? Credentials.Anonymous
                    : _credentials[Interlocked.Increment(ref _i) % _credentials.Length]);
        }
    }
}
