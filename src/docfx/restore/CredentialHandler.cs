// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal class CredentialHandler
    {
        private const int RetryCount = 3;

        private static readonly Dictionary<string, HttpConfig> s_refreshedCredentials = new();
        private static readonly SemaphoreSlim s_credentialRefreshSemaphore = new(1);

        private static readonly ConcurrentDictionary<string, Dictionary<string, string>> s_credentialCache = new();

        private readonly IReadOnlyDictionary<string, HttpConfig> _configCredentials;
        private readonly Func<string, Task<Dictionary<string, HttpConfig>>>? _getCredential;

        public bool SupportRefreshToken => _getCredential != null;

        public CredentialHandler(
            IReadOnlyDictionary<string, HttpConfig>? configCredentials = null, Func<string, Task<Dictionary<string, HttpConfig>>>? getCredential = null)
        {
            _configCredentials = configCredentials ?? new Dictionary<string, HttpConfig>();
            _getCredential = getCredential;
        }

        public async Task<HttpResponseMessage> SendRequest(HttpRequestMessage request, Func<HttpRequestMessage, Task<HttpResponseMessage>> next)
        {
            HttpResponseMessage? response = null;
            FillInCredentials(request);

            for (var i = 0; i < RetryCount; i++)
            {
                response = await next(request);

                if (response.StatusCode != System.Net.HttpStatusCode.Unauthorized
                    && _getCredential == null)
                {
                    break;
                }
                await RefreshCredential(request);
            }

            return response!;
        }

        internal bool FillInCredentials(HttpRequestMessage request)
        {
            var credentialUpdated = false;
            if (request.RequestUri?.ToString() is string url)
            {
                var credentials = GetCredentials(url);

                foreach (var (key, value) in credentials)
                {
                    if (request.Headers.TryGetValues(key, out var originalCredentials))
                    {
                        request.Headers.Remove(key);
                        if (originalCredentials.All(t => t != value))
                        {
                            credentialUpdated = true;
                        }
                    }
                    else
                    {
                        credentialUpdated = true;
                    }
                    request.Headers.Add(key, value);
                }
            }
            return credentialUpdated;
        }

        private async Task RefreshCredential(HttpRequestMessage request)
        {
            if (request.RequestUri?.ToString() is string url && _getCredential != null)
            {
                await s_credentialRefreshSemaphore.WaitAsync(CancellationToken.None);
                if (!FillInCredentials(request))
                {
                    var newCredentials = await _getCredential.Invoke(url);
                    foreach (var (key, value) in newCredentials)
                    {
                        s_refreshedCredentials[key] = value;
                    }
                    s_credentialCache.Clear();
                    FillInCredentials(request);
                }
                s_credentialRefreshSemaphore.Release();
            }
        }

        private Dictionary<string, string> GetCredentials(string url)
        {
            return s_credentialCache.GetOrAdd(url, _ =>
            {
                var credentials = new Dictionary<string, string>();
                GetCredentialsCore(credentials, url, s_refreshedCredentials);
                GetCredentialsCore(credentials, url, _configCredentials);
                return credentials;
            });
        }

        private static void GetCredentialsCore(
            Dictionary<string, string> credentials, string url, IEnumerable<KeyValuePair<string, HttpConfig>> providedCredentials)
        {
            // TODO: Merge with the following function
            foreach (var (baseUrl, rule) in providedCredentials)
            {
                if (url.StartsWith(baseUrl, StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var header in rule.Headers)
                    {
                        if (!credentials.ContainsKey(header.Key) && !string.IsNullOrEmpty(header.Value))
                        {
                            credentials.Add(header.Key, header.Value);
                        }
                    }
                    break;
                }
            }
        }
    }
}
