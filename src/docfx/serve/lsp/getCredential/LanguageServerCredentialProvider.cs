// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;

namespace Microsoft.Docs.Build;

internal class LanguageServerCredentialProvider
{
    private readonly ConcurrentDictionary<string, HttpConfig> _credentials = new();
    private readonly ILanguageServerFacade _languageServer;

    private readonly SemaphoreSlim _credentialRefreshSemaphore = new(1);

    public LanguageServerCredentialProvider(ILanguageServerFacade languageServer)
    {
        _languageServer = languageServer;
    }

    public async Task<HttpConfig?> GetCredentials(string url, HttpConfig? httpConfigUsed, bool needRefresh)
    {
        if (needRefresh)
        {
            await RefreshCredential(url, httpConfigUsed);
        }
        return GetCredentials(url);
    }

    private async Task RefreshCredential(string url, HttpConfig? httpConfigUsed)
    {
        await _credentialRefreshSemaphore.WaitAsync(CancellationToken.None);
        var httpConfig = GetCredentials(url);

        if (httpConfig == null || httpConfig == httpConfigUsed)
        {
            var getCredentialResponse = await _languageServer.SendRequest(
                new GetCredentialParams() { Url = url },
                CancellationToken.None);
            foreach (var (key, value) in getCredentialResponse.Http)
            {
                _credentials[key] = value;
            }
        }

        _credentialRefreshSemaphore.Release();
    }

    private HttpConfig? GetCredentials(string url)
    {
        foreach (var (baseUrl, rule) in _credentials)
        {
            if (url.StartsWith(baseUrl, StringComparison.OrdinalIgnoreCase))
            {
                return rule;
            }
        }
        return default;
    }
}
