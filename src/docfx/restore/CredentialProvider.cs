// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;

namespace Microsoft.Docs.Build
{
    internal class CredentialProvider
    {
        private readonly IReadOnlyDictionary<string, HttpConfig> _credentials;
        private readonly ConcurrentDictionary<string, Dictionary<string, string>> _credentialCache = new();

        public CredentialProvider(IReadOnlyDictionary<string, HttpConfig> credentials)
        {
            _credentials = credentials;
        }

        public Dictionary<string, string> GetCredentials(HttpRequestMessage request)
        {
            if (request.RequestUri?.ToString() is string url)
            {
                return _credentialCache.GetOrAdd(url, _ =>
                {
                    var credentials = new Dictionary<string, string>();
                    GetCredentialsCore(credentials, url, _credentials);
                    GetCredentialsCore(credentials, url, OpsAccessor.FallBackCredentials);
                    return credentials;
                });
            }
            return new Dictionary<string, string>();
        }

        private static void GetCredentialsCore(Dictionary<string, string> credentials, string url, IReadOnlyDictionary<string, HttpConfig> providedCredentials)
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

        private static void GetCredentialsCore(
            Dictionary<string, string> credentials, string url, IReadOnlyDictionary<string, LazyHttpConfig> providedCredentials)
        {
            foreach (var (baseUrl, rule) in providedCredentials)
            {
                if (url.StartsWith(baseUrl, StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var header in rule.Headers)
                    {
                        if (!credentials.ContainsKey(header.Key) && !string.IsNullOrEmpty(header.Value.Value))
                        {
                            credentials.Add(header.Key, header.Value.Value);
                        }
                    }
                    break;
                }
            }
        }
    }
}
