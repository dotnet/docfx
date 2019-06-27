// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal class MicrosoftGraphCache : IDisposable
    {
        private readonly string _cachePath;
        private readonly double _expirationInHours;
        private readonly SemaphoreSlim _syncRoot = new SemaphoreSlim(1, 1);
        private readonly MicrosoftGraphAccessor _microsoftGraphAccessor = null;
        private Dictionary<string, MicrosoftAlias> _aliases = new Dictionary<string, MicrosoftAlias>();
        private readonly List<string> _tempInvalidAliases = new List<string>();
        private bool _needUpdate = false;

        public MicrosoftGraphCache(Config config)
        {
            _cachePath = AppData.MicrosoftGraphCachePath;
            _expirationInHours = config.MicrosoftGraph.MicrosoftGraphCacheExpirationInHours;

            if (!string.IsNullOrEmpty(config.MicrosoftGraph.MicrosoftGraphTenantId) &&
                !string.IsNullOrEmpty(config.MicrosoftGraph.MicrosoftGraphClientId) &&
                !string.IsNullOrEmpty(config.MicrosoftGraph.MicrosoftGraphClientSecret))
            {
                _microsoftGraphAccessor = new MicrosoftGraphAccessor(
                    config.MicrosoftGraph.MicrosoftGraphTenantId,
                    config.MicrosoftGraph.MicrosoftGraphClientId,
                    config.MicrosoftGraph.MicrosoftGraphClientSecret);
            }

            if (File.Exists(_cachePath))
            {
                var cacheFile = JsonUtility.Deserialize<MicrosoftGraphCacheFile>(ProcessUtility.ReadFile(_cachePath), _cachePath);
                _aliases = cacheFile.Aliases.Where(msAlias => msAlias.Expiry >= DateTime.UtcNow).ToDictionary(msAlias => msAlias.Alias);
            }
        }

        public Task<(Error error, MicrosoftAlias msAlias)> GetMicrosoftAliasAsync(string alias)
        {
            if (string.IsNullOrEmpty(alias))
            {
                return new Task<(Error error, MicrosoftAlias msAlias)>(null, null);
            }

            return Synchronized(GetAsyncCore);

            async Task<(Error error, MicrosoftAlias msAlias)> GetAsyncCore()
            {
                if (_aliases.TryGetValue(alias, out var msAlias))
                {
                    return (null, msAlias);
                }

                if (_tempInvalidAliases.Contains(alias) || _microsoftGraphAccessor == null)
                {
                    return default;
                }

                Telemetry.TrackCacheTotalCount(TelemetryName.GitHubUserCache);

                var (error, isValid) = await _microsoftGraphAccessor.ValidateAlias(alias);

                Log.Write($"Calling Microsoft Graph API to validate {alias}");
                Telemetry.TrackCacheMissCount(TelemetryName.GitHubUserCache);

                if (error != null)
                {
                    return (error, null);
                }

                if (isValid)
                {
                    var newMsAlias = new MicrosoftAlias()
                    {
                        Alias = alias,
                        Expiry = NextExpiry(),
                    };

                    _aliases.Add(alias, new MicrosoftAlias() { Alias = alias, Expiry = NextExpiry() });
                    _needUpdate = true;

                    return (error, newMsAlias);
                }
                else
                {
                    _tempInvalidAliases.Add(alias);
                    return (error, null);
                }
            }
        }

        public void Save()
        {
            if (_needUpdate)
            {
                _syncRoot.Wait();

                try
                {
                    var content = JsonUtility.Serialize(new MicrosoftGraphCacheFile { Aliases = _aliases.Values.ToArray() });

                    PathUtility.CreateDirectoryFromFilePath(_cachePath);
                    ProcessUtility.WriteFile(_cachePath, content);
                    _needUpdate = false;
                }
                finally
                {
                    _syncRoot.Release();
                }
            }
        }

        public void Dispose()
        {
            _syncRoot.Dispose();
            _microsoftGraphAccessor?.Dispose();
        }

        private DateTime NextExpiry() => DateTime.UtcNow.AddHours(_expirationInHours);

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
