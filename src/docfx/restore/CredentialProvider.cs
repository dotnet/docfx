// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal class CredentialProvider
    {
        private const int RetryCount = 3;

        private readonly Func<string, HttpConfig?>[] _credentials;
        private readonly ConcurrentDictionary<string, HttpConfig> _credentialCache = new();

        public CredentialProvider(params Func<string, HttpConfig?>[] credentials) => _credentials = credentials;

        public async Task<HttpResponseMessage> SendRequest(HttpRequestMessage request, Func<HttpRequestMessage, Task<HttpResponseMessage>> next)
        {
            HttpResponseMessage? response = null;

            var url = request.RequestUri?.ToString();

            for (var i = 0; i < RetryCount; i++)
            {
                ApplyCredentials(request, url);

                response = await next(request);

                if (response.StatusCode != HttpStatusCode.Unauthorized)
                {
                    break;
                }

                UpdateCredentials(url);
            }

            return response!;
        }

        private void ApplyCredentials(HttpRequestMessage request, string? url)
        {
            if (url != null && _credentialCache.TryGetValue(url, out var http))
            {
                foreach (var (key, value) in http.Headers)
                {
                    if (!request.Headers.Contains(key))
                    {
                        request.Headers.Add(key, value);
                    }
                }
            }
        }

        private void UpdateCredentials(string? url)
        {
            if (url != null)
            {
                foreach (var credential in _credentials)
                {
                    if (credential(url) is HttpConfig http)
                    {
                        _credentialCache[url] = http;
                        break;
                    }
                }
            }
        }
    }
}
