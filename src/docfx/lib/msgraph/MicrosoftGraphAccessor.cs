// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using Microsoft.Graph;
using Polly;

namespace Microsoft.Docs.Build;

internal class MicrosoftGraphAccessor
{
    private readonly GraphServiceClient? _msGraphClient;
    private readonly MicrosoftGraphAuthenticationProvider? _microsoftGraphAuthenticationProvider;
    private readonly JsonDiskCache<Error, string, MicrosoftGraphUser> _aliasCache;
    private readonly SemaphoreSlim _syncRoot = new(1, 1);

    public MicrosoftGraphAccessor(Config config)
    {
        _aliasCache = new(AppData.MicrosoftGraphCachePath, TimeSpan.FromHours(config.MicrosoftGraphCacheExpirationInHours));

        if (!string.IsNullOrEmpty(config.MicrosoftGraphTenantId) &&
            !string.IsNullOrEmpty(config.MicrosoftGraphClientId) &&
            !string.IsNullOrEmpty(config.Secrets.MicrosoftGraphClientCertificate) &&
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            _microsoftGraphAuthenticationProvider = new(
                config.MicrosoftGraphTenantId, config.MicrosoftGraphClientId, config.Secrets.MicrosoftGraphClientCertificate);
            _msGraphClient = new(_microsoftGraphAuthenticationProvider);
        }
    }

    public Error? ValidateMicrosoftAlias(SourceInfo<string> alias, string name)
    {
        if (_msGraphClient is null)
        {
            // Mute error, when unable to connect to Microsoft Graph API
            return null;
        }

        var (error, user) = _aliasCache.GetOrAdd(alias.Value, GetMicrosoftGraphUserCore);

        return error ?? (user?.Id is null ? Errors.JsonSchema.MsAliasInvalid(alias, name) : null);
    }

    public Error[] Save()
    {
        return _aliasCache.Save();
    }

    private async Task<(Error?, MicrosoftGraphUser?)> GetMicrosoftGraphUserCore(string alias)
    {
        var options = new List<Option>
                {
                    new QueryOption("$select", "id,mailNickname"),
                    new QueryOption("$filter", $"mailNickname eq '{alias}'"),
                };

        try
        {
            var users = await Policy
                .Handle<ServiceException>()
                .RetryAsync(3)
                .ExecuteAsync(() => SendRequest(alias, () => _msGraphClient!.Users.Request(options).GetAsync()));

            return (null, new MicrosoftGraphUser { Alias = alias, Id = users?.FirstOrDefault()?.Id });
        }
        catch (Exception ex)
        {
            Log.Write(ex);
            return (Errors.System.MicrosoftGraphApiFailed(ex.Message), null);
        }
    }

    private async Task<T> SendRequest<T>(string api, Func<Task<T>> func)
    {
        await _syncRoot.WaitAsync();
        try
        {
            using (PerfScope.Start($"Calling Microsoft Graph API: {api}"))
            {
                return await func();
            }
        }
        finally
        {
            _syncRoot.Release();
        }
    }
}
