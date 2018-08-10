// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

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
        /// <exception cref="DocfxException">Thrown when user doesn't exist or GitHub rate limit exceeded</exception>
        public async Task<UserProfile> GetByUserName(string userName)
        {
            Debug.Assert(!string.IsNullOrEmpty(userName));

            if (!_cacheByName.TryGetValue(userName, out var userProfile))
            {
                userProfile = await _github.GetUserProfileByName(userName);
                TryAdd(userName, userProfile);
            }

            return userProfile;
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
                var (_, cache) = JsonUtility.Deserialize<Dictionary<string, UserProfile>>(json);
                return new UserProfileCache(cache, cachePath, github);
            }
            catch (Exception ex)
            {
                throw Errors.InvalidUserProfileCache(cachePath, ex).ToException(ex);
            }
        }

        public bool TryAdd(string userName, UserProfile profile)
        {
            Debug.Assert(!string.IsNullOrEmpty(userName));
            Debug.Assert(profile != null);

            var result = _cacheByName.TryAdd(userName, profile);
            foreach (var email in profile.GetUserEmails())
            {
                _cacheByEmail.TryAdd(email, profile);
            }
            return result;
        }

        public UserProfile AddOrUpdate(string userName, UserProfile value, Func<string, UserProfile, UserProfile> updateValueFactory)
        {
            var result = _cacheByName.AddOrUpdate(userName, value, updateValueFactory);
            foreach (var email in result.GetUserEmails())
            {
                _cacheByEmail[email] = result;
            }
            return result;
        }

        public UserProfile AddOrUpdate(
            string userName,
            Func<string, UserProfile> addValueFactory,
            Func<string, UserProfile, UserProfile> updateValueFactory)
        {
            var result = _cacheByName.AddOrUpdate(userName, addValueFactory, updateValueFactory);
            foreach (var email in result.GetUserEmails())
            {
                _cacheByEmail[email] = result;
            }
            return result;
        }

        private UserProfileCache(IDictionary<string, UserProfile> cache, string path, GitHubAccessor github)
        {
            Debug.Assert(cache != null);
            Debug.Assert(!string.IsNullOrEmpty(path));
            Debug.Assert(github != null);

            _cachePath = path;
            _cacheByName = new ConcurrentDictionary<string, UserProfile>(cache);
            _cacheByEmail = new ConcurrentDictionary<string, UserProfile>(
                from profile in cache.Values
                where profile?.UserEmails != null
                from email in profile.UserEmails.Split(";")
                group profile by email into g
                select new KeyValuePair<string, UserProfile>(g.Key, g.First()));
            _github = github;
        }

    }
}
