// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal class CredentialHandler
    {
        private const int RetryCount = 3;

        private readonly Func<HttpRequestMessage, bool, Task<HttpConfig?>>[] _credentialProviders;
        private readonly ConcurrentDictionary<string, Task<Dictionary<string, string>>> _credentialCache = new();

        public CredentialHandler(params Func<HttpRequestMessage, bool, Task<HttpConfig?>>[] credentialProviders) => _credentialProviders = credentialProviders;

        public async Task<HttpResponseMessage> SendRequest(HttpRequestMessage request, Func<HttpRequestMessage, Task<HttpResponseMessage>> next)
        {
            HttpResponseMessage? response = null;
            var url = request.RequestUri?.ToString() ?? throw new InvalidOperationException();

            var needRefresh = false;
            for (var i = 0; i < RetryCount; i++)
            {
                await FillInCredentials(request, url, needRefresh);

                response = await next(request);
                if (response.StatusCode != HttpStatusCode.Unauthorized)
                {
                    break;
                }

                needRefresh = true;
                _credentialCache.Clear();
            }

            return response!;
        }

        internal async Task FillInCredentials(HttpRequestMessage request, string url, bool needRefresh)
        {
            foreach (var (key, value) in await GetCredentials(request, url, needRefresh))
            {
                if (request.Headers.Contains(key))
                {
                    request.Headers.Remove(key);
                }
                request.Headers.Add(key, value);
            }
        }

        private Task<Dictionary<string, string>> GetCredentials(HttpRequestMessage request, string url, bool needRefresh)
        {
            return _credentialCache.GetOrAdd(url, async _ =>
            {
                var credentials = new Dictionary<string, string>();
                foreach (var credentialProvider in _credentialProviders)
                {
                    var httpConfig = await credentialProvider.Invoke(request, needRefresh);
                    if (httpConfig != null)
                    {
                        foreach (var header in httpConfig.Headers)
                        {
                            if (!credentials.ContainsKey(header.Key) && !string.IsNullOrEmpty(header.Value))
                            {
                                credentials.Add(header.Key, header.Value);
                            }
                        }
                    }
                }

                return credentials;
            });
        }
    }
}
