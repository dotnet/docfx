// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Net.Http;

namespace Microsoft.Docs.Build
{
    internal class CredentialProvider
    {
        private readonly IReadOnlyDictionary<string, HttpConfig> _credentials;

        public CredentialProvider(IReadOnlyDictionary<string, HttpConfig> credentials)
        {
            _credentials = credentials;
        }

        public Dictionary<string, string> GetCredentials(HttpRequestMessage request)
        {
            var credentials = new Dictionary<string, string>();
            if (request.RequestUri?.ToString() is string url)
            {
                GetCredentialsCore(credentials, url, _credentials);
                GetCredentialsCore(credentials, url, OpsAccessor.FallBackCredentials);
            }
            return credentials;
        }

        private static void GetCredentialsCore(Dictionary<string, string> credentials, string url, IReadOnlyDictionary<string, HttpConfig> providedCredentials)
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
