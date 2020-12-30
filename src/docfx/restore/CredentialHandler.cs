// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

namespace Microsoft.Docs.Build
{
    internal class CredentialHandler : DelegatingHandler
    {
        private const string DocsOPSTokenHeader = "X-OP-BuildUserToken";

        private readonly IReadOnlyList<KeyValuePair<string, HttpConfig>> _credentials;

        private static readonly SecretClient s_secretClient = new(new("https://docfx.vault.azure.net"), new DefaultAzureCredential());

        private static readonly Lazy<Task<string>> s_opsTokenProd = new(() => GetSecret("OpsBuildTokenProd"));
        private static readonly Lazy<Task<string>> s_opsTokenSandbox = new(() => GetSecret("OpsBuildTokenSandbox"));

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
                FillInCredentials(request);
                await FillOpsToken(request);

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

        private static async Task FillOpsToken(HttpRequestMessage request)
        {
            // don't access key vault for osx since azure-cli will crash in osx
            // https://github.com/Azure/azure-cli/issues/7519
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX) &&
                request.RequestUri?.ToString() is string url &&
                OpsAccessor.TryExtractDocsEnvironmentFromUrl(url, out var docsEnvironment) &&
                !request.Headers.Contains(DocsOPSTokenHeader))
            {
                // For development usage
                try
                {
                    var token = docsEnvironment switch
                    {
                        DocsEnvironment.Prod => s_opsTokenProd,
                        DocsEnvironment.PPE => s_opsTokenSandbox,
                        _ => throw new InvalidOperationException(),
                    };

                    request.Headers.Add(DocsOPSTokenHeader, await token.Value);
                }
                catch (Exception ex)
                {
                    Log.Write($"Cannot get 'OPBuildUserToken' from azure key vault, please make sure you have been granted the permission to access.");
                    Log.Write(ex);
                }
            }
        }

        private static async Task<string> GetSecret(string secret)
        {
            var response = await s_secretClient.GetSecretAsync(secret);
            if (response.Value is null)
            {
                throw new HttpRequestException(response.GetRawResponse().ToString());
            }

            return response.Value.Value;
        }
    }
}
