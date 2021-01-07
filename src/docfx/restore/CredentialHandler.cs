// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
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
                _credentialCache.TryRemove(url, out _);
                request = await CopyHttpRequestMessage(request);
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
                foreach (var credentialProvider in _credentialProviders)
                {
                    var httpConfig = await credentialProvider.Invoke(request, needRefresh);
                    if (httpConfig != null)
                    {
                        return httpConfig.Headers;
                    }
                }

                return new();
            });
        }

        private static async Task<HttpRequestMessage> CopyHttpRequestMessage(HttpRequestMessage req)
        {
            var clone = new HttpRequestMessage(req.Method, req.RequestUri);

            using var ms = new MemoryStream();
            if (req.Content != null)
            {
                await req.Content.CopyToAsync(ms).ConfigureAwait(false);
                ms.Position = 0;
                clone.Content = new StreamContent(ms);

                if (req.Content.Headers != null)
                {
                    foreach (var h in req.Content.Headers)
                    {
                        clone.Content.Headers.Add(h.Key, h.Value);
                    }
                }
            }


            clone.Version = req.Version;

            foreach (var prop in req.Options)
            {
                clone.Options.Set(new(prop.Key), prop.Value);
            }

            foreach (var header in req.Headers)
            {
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            req.Dispose();
            return clone;
        }
    }
}
