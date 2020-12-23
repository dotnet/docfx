// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

namespace Microsoft.Docs.Build
{
    internal class OpsCredentialHandler : DelegatingHandler
    {
        private const string DocsOPSTokenHeader = "X-OP-BuildUserToken";

        private static readonly SecretClient s_secretClient = new(new("https://docfx.vault.azure.net"), new DefaultAzureCredential());

        private static readonly Lazy<Task<string>> s_opsTokenProd = new(() => GetSecret("OpsBuildTokenProd"));
        private static readonly Lazy<Task<string>> s_opsTokenSandbox = new(() => GetSecret("OpsBuildTokenSandbox"));

        public OpsCredentialHandler(HttpMessageHandler innerHandler)
            : base(innerHandler)
        {
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Default header which allows fallback to public data when credential is not provided.
            request.Headers.TryAddWithoutValidation("X-OP-FallbackToPublicData", true.ToString());
            await FillOpsToken(request);
            return await base.SendAsync(request, cancellationToken);
        }

        private static async Task FillOpsToken(HttpRequestMessage request, DocsEnvironment? environment = null)
        {
            if (IsSameDocsEnvironmentRequest(request))
            {
                if (!request.Headers.Contains("X-OP-BuildUserToken")
                    && !string.IsNullOrEmpty(EnvironmentVariable.DocsOpsToken))
                {
                    request.AddOrUpdateHeader(DocsOPSTokenHeader, EnvironmentVariable.DocsOpsToken);
                }
            }

            // don't access key vault for osx since azure-cli will crash in osx
            // https://github.com/Azure/azure-cli/issues/7519
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX) &&
                !request.Headers.Contains(DocsOPSTokenHeader))
            {
                // For development usage
                try
                {
                    var tokenTask = (environment ?? OpsAccessor.DocsEnvironment) switch
                    {
                        DocsEnvironment.Prod => s_opsTokenProd,
                        DocsEnvironment.PPE => s_opsTokenSandbox,
                        _ => throw new InvalidOperationException(),
                    };
                    var token = await tokenTask.Value;
                    request.AddOrUpdateHeader(DocsOPSTokenHeader, token);
                }
                catch (Exception ex)
                {
                    Log.Write($"Cannot get 'OPBuildUserToken' from azure key vault, please make sure you have been granted the permission to access.");
                    Log.Write(ex);
                }
            }
        }

        private static bool IsSameDocsEnvironmentRequest(HttpRequestMessage request)
        {
            var url = request.RequestUri?.ToString();
            var docsEnvironment = OpsAccessor.ExtractDocsEnvironmentFromUrl(url);
            return docsEnvironment == OpsAccessor.DocsEnvironment;
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
