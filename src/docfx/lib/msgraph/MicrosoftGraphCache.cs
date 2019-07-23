// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal class MicrosoftGraphCache : IDisposable
    {
        private readonly string _cachePath;
        private readonly double _expirationInHours;
        private readonly MicrosoftGraphAccessor _microsoftGraphAccessor = null;

        private readonly ConcurrentDictionary<string, Task<(Error error, MicrosoftAlias alias)>> _aliases
                   = new ConcurrentDictionary<string, Task<(Error error, MicrosoftAlias alias)>>();

        private bool _needUpdate = false;

        public bool IsConnectedToGraphApi => _microsoftGraphAccessor != null;

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
                var now = DateTime.UtcNow;
                var cacheFile = JsonUtility.Deserialize<MicrosoftGraphCacheFile>(ProcessUtility.ReadFile(_cachePath), new FilePath(_cachePath));

                foreach (var msAlias in cacheFile.Aliases)
                {
                    if (msAlias.Expiry > now)
                    {
                        _aliases.TryAdd(msAlias.Alias, Task.FromResult((default(Error), msAlias)));
                    }
                }
            }
        }

        public Task<(Error error, MicrosoftAlias msAlias)> GetMicrosoftAlias(string alias)
        {
            if (string.IsNullOrEmpty(alias) || _microsoftGraphAccessor == null)
            {
                return Task.FromResult(default((Error, MicrosoftAlias)));
            }

            Telemetry.TrackCacheTotalCount(TelemetryName.MicrosoftGraphCache);

            return _aliases.GetOrAdd(alias, GetMicrosoftAliasCore);
        }

        public void Save()
        {
            if (_needUpdate)
            {
                var aliases = _aliases.Values
                    .Where(item => item.IsCompletedSuccessfully && !string.IsNullOrEmpty(item.Result.alias?.Alias))
                    .Select(item => item.Result.alias)
                    .ToArray();

                var content = JsonUtility.Serialize(new MicrosoftGraphCacheFile { Aliases = aliases });

                PathUtility.CreateDirectoryFromFilePath(_cachePath);
                ProcessUtility.WriteFile(_cachePath, content);
                _needUpdate = false;
            }
        }

        public void Dispose()
        {
            _microsoftGraphAccessor?.Dispose();
        }

        private async Task<(Error error, MicrosoftAlias msAlias)> GetMicrosoftAliasCore(string alias)
        {
            Log.Write($"Calling Microsoft Graph API to validate {alias}");
            Telemetry.TrackCacheMissCount(TelemetryName.MicrosoftGraphCache);

            var (error, isValid) = await _microsoftGraphAccessor.ValidateAlias(alias);
            if (error != null || !isValid)
            {
                return (error, null);
            }

            _needUpdate = true;

            var result = new MicrosoftAlias
            {
                Alias = alias,
                Expiry = DateTime.UtcNow.AddHours(RandomUtility.NextEvenDistribution(_expirationInHours)),
            };

            return (null, result);
        }
    }
}
