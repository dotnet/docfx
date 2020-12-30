// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal class CredentialHandler : DelegatingHandler
    {
        private readonly IReadOnlyList<KeyValuePair<string, HttpConfig>> _credentials;

        public CredentialHandler(IReadOnlyList<KeyValuePair<string, HttpConfig>> credentials, HttpMessageHandler innerHandler)
            : base(innerHandler)
        {
            _credentials = credentials;
        }

        public CredentialHandler Create(HttpMessageHandler innerHandler)
        {
            return new CredentialHandler(_credentials, innerHandler);
        }

        internal void Handle(HttpRequestMessage request)
        {
            FillInCredentials(request);
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            using (PerfScope.Start($"[{nameof(OpsConfigAdapter)}] Executing request '{request.Method} {request.RequestUri}'"))
            {
                Handle(request);

                return await base.SendAsync(request, cancellationToken);
            }
        }

        private void FillInCredentials(HttpRequestMessage request)
        {
            if (request.RequestUri?.ToString() is string url)
            {
                foreach (var (baseUrl, rule) in _credentials)
                {
                    if (url.StartsWith(baseUrl, StringComparison.OrdinalIgnoreCase))
                    {
                        foreach (var header in rule.Headers)
                        {
                            request.AddOrUpdateHeader(header.Key, header.Value);
                        }
                        break;
                    }
                }
            }
        }
    }
}
