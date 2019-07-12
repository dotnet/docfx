// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
        private readonly ConcurrentBag<string> _tempInvalidAliases = new ConcurrentBag<string>();

        private ConcurrentDictionary<string, MicrosoftAlias> _aliases = new ConcurrentDictionary<string, MicrosoftAlias>();
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
                _aliases = new ConcurrentDictionary<string, MicrosoftAlias>(cacheFile.Aliases.Where(msAlias => msAlias.Expiry >= DateTime.UtcNow).ToDictionary(msAlias => msAlias.Alias));
            }
        }

        public async Task<(Error error, MicrosoftAlias msAlias)> GetMicrosoftAliasAsync(string alias)
        {
            if (string.IsNullOrEmpty(alias))
            {
                return default;
            }

            if (_aliases.TryGetValue(alias, out var msAlias))
            {
                return (null, msAlias);
            }

            if (_tempInvalidAliases.Contains(alias) || _microsoftGraphAccessor == null)
            {
                return default;
            }

            Telemetry.TrackCacheTotalCount(TelemetryName.MicrosoftGraphAlias);

            var (error, isValid) = await _microsoftGraphAccessor.ValidateAlias(alias);

            Log.Write($"Calling Microsoft Graph API to validate {alias}");
            Telemetry.TrackCacheMissCount(TelemetryName.MicrosoftGraphAlias);

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

                _aliases.TryAdd(alias, new MicrosoftAlias() { Alias = alias, Expiry = NextExpiry() });
                _needUpdate = true;

                return (error, newMsAlias);
            }
            else
            {
                _tempInvalidAliases.Add(alias);
                return (error, null);
            }
        }

        public bool IsConnectedToGraphApi()
        {
            return _microsoftGraphAccessor != null;
        }

        public void Save()
        {
            if (_needUpdate)
            {
                var content = JsonUtility.Serialize(new MicrosoftGraphCacheFile { Aliases = _aliases.Values.ToArray() });
                PathUtility.CreateDirectoryFromFilePath(_cachePath);
                ProcessUtility.WriteFile(_cachePath, content);
                _needUpdate = false;
            }
        }

        public void Dispose()
        {
            _microsoftGraphAccessor?.Dispose();
        }

        private DateTime NextExpiry() => DateTime.UtcNow.AddHours(_expirationInHours);
    }
}
