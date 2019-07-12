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
        private readonly ErrorLog _errorLog;
        private readonly PublishModelBuilder _publishModelBuilder;
        private readonly MicrosoftGraphAccessor _microsoftGraphAccessor = null;
        private readonly ConcurrentDictionary<Document, KeyValuePair<string, Error>> _aliasesForOnlineValidation = new ConcurrentDictionary<Document, KeyValuePair<string, Error>>();

        private ConcurrentDictionary<string, MicrosoftAlias> _aliases = new ConcurrentDictionary<string, MicrosoftAlias>();
        private bool _needUpdate = false;

        public MicrosoftGraphCache(Config config, ErrorLog errorLog, PublishModelBuilder publishModelBuilder)
        {
            _cachePath = AppData.MicrosoftGraphCachePath;
            _expirationInHours = config.MicrosoftGraph.MicrosoftGraphCacheExpirationInHours;
            _errorLog = errorLog;
            _publishModelBuilder = publishModelBuilder;

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

        public void AddMicrosoftAliasInfoForValidation(Document file, string alias, Error error)
        {
            if (!string.IsNullOrEmpty(alias) && !_aliases.ContainsKey(alias))
            {
                _aliasesForOnlineValidation.TryAdd(file, new KeyValuePair<string, Error>(alias, error));
            }
        }

        public async Task ValidateAliases()
        {
            if (_microsoftGraphAccessor == null)
            {
                return;
            }

            Telemetry.TrackCacheTotalCount(TelemetryName.MicrosoftGraphAlias);
            var (validationResults, networkError) = await _microsoftGraphAccessor.ValidateAliases(_aliasesForOnlineValidation.Values.Select(info => info.Key).ToHashSet());
            Log.Write($"Calling Microsoft Graph API to validate {_aliasesForOnlineValidation.Keys}");
            Telemetry.TrackCacheMissCount(TelemetryName.MicrosoftGraphAlias);

            if (networkError != null)
            {
                _errorLog.Write(networkError);
                return;
            }

            foreach (var (file, info) in _aliasesForOnlineValidation)
            {
                var alias = info.Key;
                var error = info.Value;

                if (validationResults.TryGetValue(alias, out bool isValid))
                {
                    if (isValid)
                    {
                        var newMsAlias = new MicrosoftAlias()
                        {
                            Alias = alias,
                            Expiry = NextExpiry(),
                        };

                        _aliases.TryAdd(alias, new MicrosoftAlias() { Alias = alias, Expiry = NextExpiry() });
                        _needUpdate = true;
                    }
                    else
                    {
                        if (_errorLog.Write(error))
                        {
                            _publishModelBuilder.MarkError(file);
                        }
                    }
                }
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
