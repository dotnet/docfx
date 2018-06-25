// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal class GitUserProfileCache
    {
        private readonly ConcurrentDictionary<string, GitUserProfile> _cacheByName;
        private readonly ConcurrentDictionary<string, GitUserProfile> _cacheByEmail;
        private readonly string _cachePath;

        public GitUserProfileCache(IDictionary<string, GitUserProfile> cache, string path)
        {
            Debug.Assert(cache != null);
            Debug.Assert(!string.IsNullOrEmpty(path));

            _cachePath = path;
            _cacheByName = new ConcurrentDictionary<string, GitUserProfile>(cache);
            _cacheByEmail = new ConcurrentDictionary<string, GitUserProfile>(
                from profile in cache.Values
                where profile?.UserEmails != null
                from email in profile.UserEmails.Split(";")
                group profile by email into g
                select new KeyValuePair<string, GitUserProfile>(g.Key, g.First()));
        }

        public GitUserProfile GetByUserName(string userName)
        {
            Debug.Assert(!string.IsNullOrEmpty(userName));

            if (_cacheByName.TryGetValue(userName, out var userProfile))
                return userProfile;
            else
                return null;
        }

        public GitUserProfile GetByUserEmail(string userEmail)
        {
            Debug.Assert(!string.IsNullOrEmpty(userEmail));

            if (_cacheByEmail.TryGetValue(userEmail, out var userProfile))
                return userProfile;
            else
                return null;
        }

        /// <summary>
        /// Create an instance of <see cref="GitUserProfileCache"/> from local cache
        /// </summary>
        /// <param name="cachePath">the path of the cache file</param>
        public static GitUserProfileCache Create(string cachePath)
        {
            Debug.Assert(!string.IsNullOrEmpty(cachePath));

            var json = "{}";
            if (File.Exists(cachePath))
                json = File.ReadAllText(cachePath);

            try
            {
                var cache = JsonUtility.Deserialize<Dictionary<string, GitUserProfile>>(json);
                return new GitUserProfileCache(cache, cachePath);
            }
            catch (Exception ex)
            {
                throw Errors.InvalidUserProfileCache(cachePath, ex).ToException(ex);
            }
        }
    }
}
