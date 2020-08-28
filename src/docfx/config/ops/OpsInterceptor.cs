// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Polly;
using Polly.Extensions.Http;

namespace Microsoft.Docs.Build
{
    internal class OpsInterceptor : IDisposable
    {
        public static readonly DocsEnvironment DocsEnvironment = GetDocsEnvironment();

        private static readonly SecretClient s_secretClient = new SecretClient(new Uri("https://docfx.vault.azure.net"), new DefaultAzureCredential());
        private static readonly Lazy<Task<string>> s_opsTokenProd = new Lazy<Task<string>>(() => GetSecret("OpsBuildTokenProd"));
        private static readonly Lazy<Task<string>> s_opsTokenSandbox = new Lazy<Task<string>>(() => GetSecret("OpsBuildTokenSandbox"));

        private readonly Action<HttpRequestMessage> _credentialProvider;
        private readonly ErrorBuilder _errors;
        private readonly HttpClient _http = new HttpClient();

        public OpsInterceptor(ErrorBuilder errors, Action<HttpRequestMessage> credentialProvider)
        {
            _errors = errors;
            _credentialProvider = credentialProvider;
        }

        public async Task<string> Fetch(
            string url,
            IReadOnlyDictionary<string, string>? headers = null,
            string? value404 = null,
            DocsEnvironment? environment = null)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            if (headers != null)
            {
                foreach (var (key, value) in headers)
                {
                    request.Headers.TryAddWithoutValidation(key, value);
                }
            }
            var response = await SendRequest(request, environment);

            if (value404 != null && response.StatusCode == HttpStatusCode.NotFound)
            {
                return value404;
            }
            return await response.EnsureSuccessStatusCode().Content.ReadAsStringAsync();
        }

        public async Task<HttpResponseMessage> SendRequest(HttpRequestMessage request, DocsEnvironment? environment = null)
        {
            if (!request.RequestUri.IsAbsoluteUri)
            {
                request.RequestUri = new Uri(PrependEndpoint(request.RequestUri.OriginalString, environment));
            }
            using (PerfScope.Start($"[{nameof(OpsConfigAdapter)}] Executing request '{request.Method} {request.RequestUri}'"))
            {
                _credentialProvider?.Invoke(request);

                await FillOpsToken(request.RequestUri.AbsoluteUri, request);

                return await _http.SendAsync(request);
            }
        }

        public async Task<string> FetchValidationRules(string url, IReadOnlyDictionary<string, string>? headers = null, DocsEnvironment? environment = null)
        {
            try
            {
                url = PrependEndpoint(url, environment);
                using (PerfScope.Start($"[{nameof(OpsConfigAdapter)}] Fetching '{url}'"))
                {
                    using var response = await HttpPolicyExtensions
                       .HandleTransientHttpError()
                       .Or<OperationCanceledException>()
                       .Or<IOException>()
                       .RetryAsync(3, onRetry: (_, i) => Log.Write($"[{i}] Retrying '{url}'"))
                       .ExecuteAsync(async () =>
                       {
                           using var request = new HttpRequestMessage(HttpMethod.Get, url);
                           _credentialProvider?.Invoke(request);
                           if (headers != null)
                           {
                               foreach (var (key, value) in headers)
                               {
                                   request.Headers.TryAddWithoutValidation(key, value);
                               }
                           }
                           await FillOpsToken(url, request, environment);
                           var response = await _http.SendAsync(request);
                           if (response.Headers.TryGetValues("X-Metadata-Version", out var metadataVersion))
                           {
                               _errors.Add(Errors.System.MetadataValidationRuleset(string.Join(',', metadataVersion)));
                           }
                           return response;
                       });

                    return await response.EnsureSuccessStatusCode().Content.ReadAsStringAsync();
                }
            }
            catch (Exception ex)
            {
                // Getting validation rules failure should not block build proceeding,
                // catch and log the exception without rethrow.
                Log.Write(ex);
                _errors.Add(Errors.System.ValidationIncomplete());
                return "{}";
            }
        }

        public void Dispose()
        {
            _http.Dispose();
        }

        private static string PrependEndpoint(string url, DocsEnvironment? environment) => $"{BuildServiceEndpoint(environment)}/{url.TrimStart('/')}";

        private static string BuildServiceEndpoint(DocsEnvironment? environment = null)
        {
            return (environment ?? DocsEnvironment) switch
            {
                DocsEnvironment.Prod => "https://op-build-prod.azurewebsites.net",
                DocsEnvironment.PPE => "https://op-build-sandbox2.azurewebsites.net",
                DocsEnvironment.Internal => "https://op-build-internal.azurewebsites.net",
                DocsEnvironment.Perf => "https://op-build-perf.azurewebsites.net",
                _ => throw new NotSupportedException(),
            };
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

        private static async Task FillOpsToken(string url, HttpRequestMessage request, DocsEnvironment? environment = null)
        {
            if (url.StartsWith(BuildServiceEndpoint(environment)) && !request.Headers.Contains("X-OP-BuildUserToken"))
            {
                // For development usage
                try
                {
                    var token = (environment ?? DocsEnvironment) switch
                    {
                        DocsEnvironment.Prod => s_opsTokenProd,
                        DocsEnvironment.PPE => s_opsTokenSandbox,
                        _ => throw new InvalidOperationException(),
                    };

                    request.Headers.Add("X-OP-BuildUserToken", await token.Value);
                }
                catch (Exception ex)
                {
                    Log.Write(
                        $"Cannot get 'OPBuildUserToken' from azure key vault, please make sure you have been granted the permission to access: {ex.Message}");
                }
            }
        }
    }
}
