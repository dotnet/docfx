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
    internal class MicrosoftAliasCache : IDisposable
    {
        private readonly string _cachePath;
        private readonly double _expirationInHours;
        private readonly SemaphoreSlim _syncRoot = new SemaphoreSlim(1, 1);
        private readonly MicrosoftGraphAccessor _microsoftGraphAccessor;
        private Dictionary<string, MicrosoftAlias> _aliases = new Dictionary<string, MicrosoftAlias>();

        public MicrosoftAliasCache(Config config)
        {
            _cachePath = AppData.MicrosoftAliasCachePath;
            _expirationInHours = config.JsonSchema.MicrosoftAliasCacheExpirationInHours;

            _microsoftGraphAccessor = new MicrosoftGraphAccessor(
                config.JsonSchema.MicrosoftGraphTenantId,
                config.JsonSchema.MicrosoftGraphClientId,
                config.JsonSchema.MicrosoftGraphClientSecret);

            if (File.Exists(_cachePath))
            {
                var cacheContent = JsonUtility.Deserialize<MicrosoftAlias[]>(ProcessUtility.ReadFile(_cachePath), _cachePath);
                _aliases = cacheContent.Where(msAlias => msAlias.Expiry >= DateTime.UtcNow).ToDictionary(msAlias => msAlias.Alias);
            }
        }

        public Task<MicrosoftAlias> GetAsync(string alias)
        {
            if (string.IsNullOrEmpty(alias))
            {
                return null;
            }

            return Synchronized(GetAsyncCore);

            async Task<MicrosoftAlias> GetAsyncCore()
            {
                if (_aliases.TryGetValue(alias, out var msAlias))
                {
                    return msAlias;
                }
                else
                {
                    var newMsAlias = new MicrosoftAlias()
                    {
                        Alias = alias,
                        IsValid = await _microsoftGraphAccessor.ValidateAlias(alias),
                        Expiry = NextExpiry(),
                    };

                    _aliases.Add(alias, newMsAlias);
                    return newMsAlias;
                }
            }
        }

        public void Dispose()
        {
            _syncRoot.Dispose();
            _microsoftGraphAccessor.Dispose();
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
