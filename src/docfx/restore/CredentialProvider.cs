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

        private readonly Func<string, bool, HttpConfig?>[] _credentials;
        private readonly ConcurrentDictionary<string, HttpConfig?> _credentialCache = new();

        public CredentialProvider(params Func<string, bool, HttpConfig?>[] credentials) => _credentials = credentials;

        public async Task<HttpResponseMessage> SendRequest(HttpRequestMessage request, Func<HttpRequestMessage, Task<HttpResponseMessage>> next)
        {
            HttpResponseMessage? response = null;

            var isUnauthorized = false;
            var url = request.RequestUri?.ToString() ?? throw new InvalidOperationException();

            for (var i = 0; i < RetryCount; i++)
            {
                ApplyCredentials(url, request, isUnauthorized);

                response = await next(request);

                if (response.StatusCode != HttpStatusCode.Unauthorized)
                {
                    break;
                }

                isUnauthorized = true;
                _credentialCache.TryRemove(url, out _);
            }

            return response!;
        }

        private void ApplyCredentials(string url, HttpRequestMessage request, bool isUnauthorized)
        {
            if (_credentialCache.GetOrAdd(url, key => GetCredential(key, isUnauthorized)) is HttpConfig http)
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

        private HttpConfig? GetCredential(string url, bool isUnauthorized)
        {
            foreach (var credential in _credentials)
            {
                if (credential(url, isUnauthorized) is HttpConfig http)
                {
                    return http;
                }
            }
            return default;
        }
    }
}
