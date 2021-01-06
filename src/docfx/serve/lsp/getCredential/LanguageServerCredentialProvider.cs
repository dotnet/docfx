// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;

namespace Microsoft.Docs.Build
{
    internal class LanguageServerCredentialProvider
    {
        private readonly ConcurrentDictionary<string, HttpConfig> _credentials = new();
        private readonly ILanguageServerFacade _languageServer;
        private readonly ILogger _logger;

        private readonly SemaphoreSlim _credentialRefreshSemaphore = new(1);

        public LanguageServerCredentialProvider(ILoggerFactory loggerFactory, ILanguageServerFacade languageServer)
        {
            _languageServer = languageServer;
            _logger = loggerFactory.CreateLogger<LanguageServerCredentialProvider>();
        }

        public async Task<HttpConfig?> GetCredentials(HttpRequestMessage request, bool needRefresh)
        {
            var url = request.RequestUri?.ToString() ?? throw new InvalidOperationException();

            if (needRefresh)
            {
                await RefreshCredential(request, url);
            }
            return GetCredentials(url);
        }

        private async Task RefreshCredential(HttpRequestMessage request, string url)
        {
            await _credentialRefreshSemaphore.WaitAsync(CancellationToken.None);

            if (!IsCredentialOutOfDate(request, url))
            {
                try
                {
                    var getCredentialResponse = await _languageServer.SendRequest(
                        new GetCredentialParams() { Url = url },
                        CancellationToken.None);
                    foreach (var (key, value) in getCredentialResponse.Http)
                    {
                        _credentials[key] = value;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogCritical($"Get credential for '{url}' failed: {ex}");
                }
            }

            _credentialRefreshSemaphore.Release();
        }

        private bool IsCredentialOutOfDate(HttpRequestMessage request, string url)
        {
            var credentials = GetCredentials(url);
            if (credentials != null)
            {
                foreach (var (key, value) in credentials.Headers)
                {
                    if (!request.Headers.TryGetValues(key, out var values) || values.All(item => item != value))
                    {
                        return true;
                    }
                }
            }
            return false;
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
}
