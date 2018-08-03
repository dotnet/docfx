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

        public UserProfileCache(IDictionary<string, UserProfile> cache, string path)
        {
            Debug.Assert(cache != null);
            Debug.Assert(!string.IsNullOrEmpty(path));

            _cachePath = path;
            _cacheByName = new ConcurrentDictionary<string, UserProfile>(cache);
            _cacheByEmail = new ConcurrentDictionary<string, UserProfile>(
                from profile in cache.Values
                where profile?.UserEmails != null
                from email in profile.UserEmails.Split(";")
                group profile by email into g
                select new KeyValuePair<string, UserProfile>(g.Key, g.First()));
        }

        /// <summary>
        /// Get user profile by user name from user profile cache or GitHub API
        /// </summary>
        /// <exception cref="DocfxException">Thrown when user doesn't exist or GitHub rate limit exceeded</exception>
        public async Task<UserProfile> GetByUserName(string userName)
        {
            Debug.Assert(!string.IsNullOrEmpty(userName));

            if (!_cacheByName.TryGetValue(userName, out var userProfile))
            {
                userProfile = await GitHubAccessor.GetUserProfileByName(userName);
                AddToCache(userProfile);
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
        public static UserProfileCache Create(string cachePath)
        {
            Debug.Assert(!string.IsNullOrEmpty(cachePath));

            var json = "{}";
            if (File.Exists(cachePath))
                json = File.ReadAllText(cachePath);

            try
            {
                var (_, cache) = JsonUtility.Deserialize<Dictionary<string, UserProfile>>(json);
                return new UserProfileCache(cache, cachePath);
            }
            catch (Exception ex)
            {
                throw Errors.InvalidUserProfileCache(cachePath, ex).ToException(ex);
            }
        }

        private void AddToCache(UserProfile profile)
        {
            if (profile == null)
                return;

            if (!string.IsNullOrEmpty(profile.Name))
            {
                _cacheByName.TryAdd(profile.Name, profile);
            }

            if (profile.UserEmails != null)
            {
                foreach (var email in profile.UserEmails.Split(";"))
                {
                    _cacheByEmail.TryAdd(email, profile);
                }
            }
        }
    }
}
