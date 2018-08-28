// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class UserProfileCache
    {
        private readonly ConcurrentDictionary<string, UserProfile> _cacheByName;
        private readonly ConcurrentDictionary<string, UserProfile> _cacheByEmail;
        private readonly string _cachePath;
        private readonly GitHubAccessor _github;

        /// <summary>
        /// Get user profile by user name from user profile cache or GitHub API
        /// </summary>
        public async Task<(Error error, UserProfile profile)> GetByUserName(string userName)
        {
            Debug.Assert(!string.IsNullOrEmpty(userName));

            Error error = null;
            if (!_cacheByName.TryGetValue(userName, out var userProfile))
            {
                (error, userProfile) = await _github.GetUserProfileByName(userName);
                TryAdd(userName, userProfile);
            }

            return (error, userProfile);
        }

        public UserProfile GetByUserEmail(string userEmail)
        {
            Debug.Assert(!string.IsNullOrEmpty(userEmail));

            if (_cacheByEmail.TryGetValue(userEmail, out var userProfile))
                return userProfile;
            else
                return null;
        }

        /// <summary>
        /// Add user profiles of all authors of commits to cache if not exist before
        /// </summary>
        public async Task<List<Error>> AddUsersForCommits(GitCommit[] commits, Repository repo)
        {
            Debug.Assert(commits != null);
            Debug.Assert(repo != null);

            var errors = new List<Error>();
            foreach (var commit in commits)
            {
                await UpdateCacheByCommit(commit, repo, errors);
            }
            return errors;
        }

        /// <summary>
        /// Create an instance of <see cref="UserProfileCache"/> from local cache
        /// </summary>
        /// <param name="cachePath">the path of the cache file</param>
        /// <param name="github">the GitHubAccessor to fetch information when missing in cache</param>
        public static UserProfileCache Create(string cachePath, GitHubAccessor github)
        {
            Debug.Assert(!string.IsNullOrEmpty(cachePath));
            Debug.Assert(github != null);

            var json = "{}";
            if (File.Exists(cachePath))
                json = File.ReadAllText(cachePath);

            try
            {
                var (_, jObject) = JsonUtility.Deserialize<JObject>(json);
                var (_, cache) = JsonUtility.ToObject<Dictionary<string, UserProfile>>(Normalize(jObject));
                return new UserProfileCache(cache, cachePath, github);
            }
            catch (Exception ex)
            {
                throw Errors.InvalidUserProfileCache(cachePath, ex).ToException(ex);
            }
        }

        private static JObject Normalize(JObject cache)
        {
            foreach (var pair in cache)
            {
                if (pair.Value is JObject profile
                    && profile.TryGetValue("user_emails", out var userEmails)
                    && userEmails is JValue userEmailsValue)
                {
                    profile["user_emails"] = new JArray(userEmailsValue.ToString().Split(';'));
                }
            }
            return cache;
        }

        private async Task UpdateCacheByCommit(GitCommit commit, Repository repo, List<Error> errors)
        {
            var email = commit.AuthorEmail;
            if (string.IsNullOrEmpty(email) || GetByUserEmail(email) != null)
                return;

            var (authorError, authorName) = await _github.GetNameByCommit(repo.Owner, repo.Name, commit.Sha);
            if (authorError != null)
                errors.Add(authorError);
            if (authorName == null)
                return;

            if (_cacheByName.TryGetValue(authorName, out var existingProfile))
            {
                AddEmailToUserProfile(email, existingProfile);
                return;
            }

            var (getProfileError, profile) = await _github.GetUserProfileByName(authorName);
            if (getProfileError != null)
                errors.Add(getProfileError);
            if (profile == null)
                return;

            TryAdd(authorName, profile);
            AddEmailToUserProfile(email, profile);
        }

        private void AddEmailToUserProfile(string email, UserProfile profile)
        {
            profile.UserEmails.Add(email);
            _cacheByEmail[email] = profile;
        }

        private UserProfileCache(IDictionary<string, UserProfile> cache, string path, GitHubAccessor github)
        {
            Debug.Assert(cache != null);
            Debug.Assert(!string.IsNullOrEmpty(path));
            Debug.Assert(github != null);

            _cachePath = path;
            _cacheByName = new ConcurrentDictionary<string, UserProfile>(cache, StringComparer.OrdinalIgnoreCase);
            _cacheByEmail = new ConcurrentDictionary<string, UserProfile>(
                from profile in cache.Values
                where profile?.UserEmails != null
                from email in profile.UserEmails
                group profile by email into g
                select new KeyValuePair<string, UserProfile>(g.Key, g.First()));
            _github = github;
        }

        private void TryAdd(string userName, UserProfile profile)
        {
            if (profile == null)
                return;

            Debug.Assert(!string.IsNullOrEmpty(userName));

            var result = _cacheByName.TryAdd(userName, profile);
            foreach (var email in profile.UserEmails)
            {
                _cacheByEmail.TryAdd(email, profile);
            }
        }
    }
}
