// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Net;

namespace Microsoft.Docs.Build;

internal class CredentialHandler
{
    private const int RetryCount = 3;

    private readonly CredentialProvider[] _credentialProviders;
    private readonly ConcurrentDictionary<string, Task<HttpConfig>> _credentialCache = new();

    public CredentialHandler(params CredentialProvider[] credentialProviders) => _credentialProviders = credentialProviders;

    public async Task<HttpResponseMessage> SendRequest(Func<HttpRequestMessage> requestFactory, Func<HttpRequestMessage, Task<HttpResponseMessage>> next)
    {
        HttpResponseMessage? response = null;

        var needRefresh = false;
        HttpConfig? httpConfigUsed = null;

        for (var i = 0; i < RetryCount; i++)
        {
            using var request = requestFactory();
            var url = request.RequestUri?.ToString() ?? throw new InvalidOperationException();

            if (i > 0)
            {
                Log.Write($"[{nameof(CredentialHandler)}] Retry '{request.Method} {UrlUtility.SanitizeUrl(url)}'");
            }

            var httpConfig = await GetCredentials(url, httpConfigUsed, needRefresh);
            FillInCredentials(request, httpConfig);

            response = await next(request);
            if (response.StatusCode != HttpStatusCode.Unauthorized)
            {
                break;
            }

            needRefresh = true;
            _credentialCache.TryRemove(url, out _);
            httpConfigUsed = httpConfig;
        }

        return response!;
    }

    internal static void FillInCredentials(HttpRequestMessage request, HttpConfig httpConfig)
    {
        foreach (var (key, value) in httpConfig.Headers)
        {
            if (request.Headers.Contains(key))
            {
                request.Headers.Remove(key);
            }
            request.Headers.Add(key, value);
        }
    }

    private Task<HttpConfig> GetCredentials(string url, HttpConfig? httpConfigUsed, bool needRefresh)
    {
        return _credentialCache.GetOrAdd(url, async _ =>
        {
            foreach (var credentialProvider in _credentialProviders)
            {
                var httpConfig = await credentialProvider.Invoke(url, httpConfigUsed, needRefresh);
                if (httpConfig != null)
                {
                    return httpConfig;
                }
            }

            return new();
        });
    }
}
