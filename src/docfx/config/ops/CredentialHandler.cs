// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
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

        public static readonly DocsEnvironment DocsEnvironment = GetDocsEnvironment();

        private readonly Action<HttpRequestMessage>? _credentialProvider;

        // TODO: use Azure front door endpoint when it is stable
        private static readonly string s_docsProdServiceEndpoint =
            Environment.GetEnvironmentVariable("DOCS_PROD_SERVICE_ENDPOINT") ?? "https://op-build-prod.azurewebsites.net";

        private static readonly string s_docsPPEServiceEndpoint =
            Environment.GetEnvironmentVariable("DOCS_PPE_SERVICE_ENDPOINT") ?? "https://op-build-sandbox2.azurewebsites.net";

        private static readonly string s_docsInternalServiceEndpoint =
            Environment.GetEnvironmentVariable("DOCS_INTERNAL_SERVICE_ENDPOINT") ?? "https://op-build-internal.azurewebsites.net";

        private static readonly string s_docsPerfServiceEndpoint =
            Environment.GetEnvironmentVariable("DOCS_PERF_SERVICE_ENDPOINT") ?? "https://op-build-perf.azurewebsites.net";

        private static readonly SecretClient s_secretClient = new(new("https://docfx.vault.azure.net"), new DefaultAzureCredential());

        private static readonly Lazy<Task<string>> s_opsTokenProd = new(() => GetSecret("OpsBuildTokenProd"));
        private static readonly Lazy<Task<string>> s_opsTokenSandbox = new(() => GetSecret("OpsBuildTokenSandbox"));

        public CredentialHandler(Action<HttpRequestMessage>? credentialProvider, HttpMessageHandler innerHandler)
            : base(innerHandler)
        {
            _credentialProvider = credentialProvider;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            using (PerfScope.Start($"[{nameof(OpsConfigAdapter)}] Executing request '{request.Method} {request.RequestUri}'"))
            {
                _credentialProvider?.Invoke(request);
                if (TryExtractDocsEnvironmentFromUrl(request.RequestUri?.AbsoluteUri, out _))
                {
                    return await HandleOpsRequest(request, cancellationToken);
                }

                return await base.SendAsync(request, cancellationToken);
            }
        }

        public static string BuildServiceEndpoint(DocsEnvironment? environment = null)
        {
            return (environment ?? DocsEnvironment) switch
            {
                DocsEnvironment.Prod => s_docsProdServiceEndpoint,
                DocsEnvironment.PPE => s_docsPPEServiceEndpoint,
                DocsEnvironment.Internal => s_docsInternalServiceEndpoint,
                DocsEnvironment.Perf => s_docsPerfServiceEndpoint,
                _ => throw new NotSupportedException(),
            };
        }

        private async Task<HttpResponseMessage> HandleOpsRequest(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Default header which allows fallback to public data when credential is not provided.
            request.Headers.TryAddWithoutValidation("X-OP-FallbackToPublicData", true.ToString());
            await FillOpsToken(request);
            return await base.SendAsync(request, cancellationToken);
        }

        private static bool TryExtractDocsEnvironmentFromUrl(string? url, out DocsEnvironment? docsEnvironment)
        {
            docsEnvironment = default;
            if (string.IsNullOrEmpty(url))
            {
                return false;
            }

            if (url.StartsWith(s_docsProdServiceEndpoint))
            {
                docsEnvironment = DocsEnvironment.Prod;
                return true;
            }
            else if (url.StartsWith(s_docsPPEServiceEndpoint))
            {
                docsEnvironment = DocsEnvironment.PPE;
                return true;
            }
            else if (url.StartsWith(s_docsInternalServiceEndpoint))
            {
                docsEnvironment = DocsEnvironment.Internal;
                return true;
            }
            else if (url.StartsWith(s_docsPerfServiceEndpoint))
            {
                docsEnvironment = DocsEnvironment.Perf;
                return true;
            }
            else
            {
                return false;
            }
        }

        private static async Task FillOpsToken(HttpRequestMessage request, DocsEnvironment? environment = null)
        {
            if (IsSameDocsEnvironmentRequest(request))
            {
                if (!request.Headers.Contains("X-OP-BuildUserToken")
                    && !string.IsNullOrEmpty(EnvironmentVariable.DocsOpsToken))
                {
                    UpdateDocsOPSToken(request, EnvironmentVariable.DocsOpsToken);
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
                    var tokenTask = (environment ?? DocsEnvironment) switch
                    {
                        DocsEnvironment.Prod => s_opsTokenProd,
                        DocsEnvironment.PPE => s_opsTokenSandbox,
                        _ => throw new InvalidOperationException(),
                    };
                    var token = await tokenTask.Value;
                    UpdateDocsOPSToken(request, token);
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
            Debug.Assert(TryExtractDocsEnvironmentFromUrl(url, out var docsEnvironment));
            return docsEnvironment == DocsEnvironment;
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

        private static DocsEnvironment GetDocsEnvironment()
        {
            return Enum.TryParse(Environment.GetEnvironmentVariable("DOCS_ENVIRONMENT"), true, out DocsEnvironment docsEnvironment)
                ? docsEnvironment
                : DocsEnvironment.Prod;
        }

        private static void UpdateDocsOPSToken(HttpRequestMessage request, string token)
        {
            if (request.Headers.Contains(DocsOPSTokenHeader))
            {
                request.Headers.Remove(DocsOPSTokenHeader);
            }

            request.Headers.Add(DocsOPSTokenHeader, token);
        }
    }
}
