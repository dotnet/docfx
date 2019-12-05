// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Graph;
using Polly;

namespace Microsoft.Docs.Build
{
    internal class MicrosoftGraphAccessor : IDisposable
    {
        private readonly IGraphServiceClient _msGraphClient;
        private readonly MicrosoftGraphAuthenticationProvider _microsoftGraphAuthenticationProvider;
        private readonly JsonDiskCache<Error, string, MicrosoftGraphUser> _aliasCache;

        public MicrosoftGraphAccessor(Config config)
        {
            _aliasCache = new JsonDiskCache<Error, string, MicrosoftGraphUser>(
                AppData.MicrosoftGraphCachePath, TimeSpan.FromHours(config.MicrosoftGraph.MicrosoftGraphCacheExpirationInHours));

            if (!string.IsNullOrEmpty(config.MicrosoftGraph.MicrosoftGraphTenantId) &&
                !string.IsNullOrEmpty(config.MicrosoftGraph.MicrosoftGraphClientId) &&
                !string.IsNullOrEmpty(config.MicrosoftGraph.MicrosoftGraphClientSecret))
            {
                _microsoftGraphAuthenticationProvider = new MicrosoftGraphAuthenticationProvider(
                    config.MicrosoftGraph.MicrosoftGraphTenantId,
                    config.MicrosoftGraph.MicrosoftGraphClientId,
                    config.MicrosoftGraph.MicrosoftGraphClientSecret);

                _msGraphClient = new GraphServiceClient(_microsoftGraphAuthenticationProvider);
            }
        }

        public async Task<Error> ValidateMicrosoftAlias(SourceInfo<string> alias, string name = null)
        {
            if (_msGraphClient is null)
            {
                // Mute error, when unable to connect to Microsoft Graph API
                return null;
            }

            var (error, user) = await _aliasCache.GetOrAdd(alias.Value, GetMicrosoftGraphUserCore);

            return error ?? (user is null ? Errors.MsAliasInvalid(alias, name) : null);
        }

        public Task<Error[]> Save()
        {
            return _aliasCache.Save();
        }

        public void Dispose()
        {
            _microsoftGraphAuthenticationProvider?.Dispose();
        }

        private async Task<(Error, MicrosoftGraphUser)> GetMicrosoftGraphUserCore(string alias)
        {
            if (string.IsNullOrWhiteSpace(alias))
            {
                return default;
            }

            var options = new List<Option>
            {
                new QueryOption("$select", "mailNickname"),
                new QueryOption("$filter", $"mailNickname eq '{alias}'"),
            };

            try
            {
                var users = await Policy
                    .Handle<ServiceException>()
                    .RetryAsync(3)
                    .ExecuteAsync(() => _msGraphClient.Users.Request(options).GetAsync());

                return (null, users != null && users.Count > 0 ? new MicrosoftGraphUser { Alias = alias } : null);
            }
            catch (Exception e)
            {
                return (Errors.MicrosoftGraphApiFailed(e.Message), null);
            }
        }
    }
}
