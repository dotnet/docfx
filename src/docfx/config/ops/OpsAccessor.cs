// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Docs.LearnValidation;
using Newtonsoft.Json;
using Polly;
using Polly.Extensions.Http;

namespace Microsoft.Docs.Build
{
    internal class OpsAccessor : IDisposable, ILearnServiceAccessor
    {
        public static readonly DocsEnvironment DocsEnvironment = GetDocsEnvironment();

        // TODO: use Azure front door endpoint when it is stable
        private static readonly string DocsProdServiceEndpoint =
            Environment.GetEnvironmentVariable("DOCS_PROD_SERVICE_ENDPOINT") ?? "https://op-build-prod.azurewebsites.net";

        private static readonly string DocsPPEServiceEndpoint =
            Environment.GetEnvironmentVariable("DOCS_PPE_SERVICE_ENDPOINT") ?? "https://op-build-sandbox2.azurewebsites.net";

        private static readonly string DocsInternalServiceEndpoint =
            Environment.GetEnvironmentVariable("DOCS_INTERNAL_SERVICE_ENDPOINT") ?? "https://op-build-internal.azurewebsites.net";

        private static readonly string DocsPerfServiceEndpoint =
            Environment.GetEnvironmentVariable("DOCS_PERF_SERVICE_ENDPOINT") ?? "https://op-build-perf.azurewebsites.net";

        private static readonly SecretClient s_secretClient = new SecretClient(new Uri("https://docfx.vault.azure.net"), new DefaultAzureCredential());
        private static readonly Lazy<Task<string>> s_opsTokenProd = new Lazy<Task<string>>(() => GetSecret("OpsBuildTokenProd"));
        private static readonly Lazy<Task<string>> s_opsTokenSandbox = new Lazy<Task<string>>(() => GetSecret("OpsBuildTokenSandbox"));

        private readonly Action<HttpRequestMessage> _credentialProvider;
        private readonly ErrorBuilder _errors;
        private readonly HttpClient _http = new HttpClient();

        public OpsAccessor(ErrorBuilder errors, Action<HttpRequestMessage> credentialProvider)
        {
            _errors = errors;
            _credentialProvider = credentialProvider;
        }

        public async Task<string> GetDocsetInfo(string repositoryUrl)
        {
            var fetchUrl = $"/v2/Queries/Docsets?git_repo_url={repositoryUrl}&docset_query_status=Created";
            return await Fetch(fetchUrl, value404: "[]");
        }

        public Task<string> GetMonikerDefinition()
        {
            return Fetch("/v2/monikertrees/allfamiliesproductsmonikers");
        }

        public async Task<string[]> GetXrefMaps(string tag, string xrefEndpoint, string xrefMapQueryParams)
        {
            var xrefMapDocsEnvironment = GetXrefMapEnvironment(xrefEndpoint);
            var response = await Fetch(
                $"/v1/xrefmap{tag}{xrefMapQueryParams}", value404: "{}", environment: xrefMapDocsEnvironment);
            return JsonConvert.DeserializeAnonymousType(response, new { links = new[] { "" } }).links
                ?? Array.Empty<string>();
        }

        public async Task<string> GetMarkdownValidationRules((string repositoryUrl, string branch) tuple)
        {
            return await FetchValidationRules($"/route/validationmgt/rulesets/contentrules", tuple.repositoryUrl, tuple.branch);
        }

        public async Task<string> GetAllowlists((string repositoryUrl, string branch) tuple)
        {
            return await FetchValidationRules(
                   $"/taxonomies/simplified?" +
                   $"name=ms.author&name=ms.devlang&name=ms.prod&name=ms.service&name=ms.topic&name=devlang&name=product",
                   tuple.repositoryUrl,
                   tuple.branch);
        }

        public async Task<string> GetMetadataSchema((string repositoryUrl, string branch) tuple)
        {
            var metadataRules = FetchValidationRules($"/route/validationmgt/rulesets/metadatarules", tuple.repositoryUrl, tuple.branch);
            var allowlists = FetchValidationRules(
                $"/taxonomies/simplified?" +
                $"name=ms.author&name=ms.devlang&name=ms.prod&name=ms.service&name=ms.topic&name=devlang&name=product",
                tuple.repositoryUrl,
                tuple.branch);

            return OpsMetadataRuleConverter.GenerateJsonSchema(await metadataRules, await allowlists);
        }

        public async Task<string> GetRegressionAllContentRules()
        {
            return await FetchValidationRules(
                "/route/validationmgt/rulesets/contentrules?name=_regression_all_",
                environment: DocsEnvironment.PPE);
        }

        public async Task<string> GetRegressionAllMetadataSchema()
        {
            var metadataRules = FetchValidationRules(
                "/route/validationmgt/rulesets/metadatarules?name=_regression_all_",
                environment: DocsEnvironment.PPE);
            var allowlists = FetchValidationRules(
                $"/taxonomies/simplified?" +
                $"name=ms.author&name=ms.devlang&name=ms.prod&name=ms.service&name=ms.topic&name=devlang&name=product",
                environment: DocsEnvironment.PPE);

            return OpsMetadataRuleConverter.GenerateJsonSchema(await metadataRules, await allowlists);
        }

        public async Task<string> HierarchyDrySync(string body)
        {
            using var request = new HttpRequestMessage
            {
                RequestUri = new Uri($"{BuildServiceEndpoint()}/route/mslearnhierarchy/api/OnDemandHierarchyDrySync"),
                Method = HttpMethod.Post,
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };

            var response = await SendRequest(request);
            return await response.EnsureSuccessStatusCode().Content.ReadAsStringAsync();
        }

        public async Task<bool> CheckLearnPathItemExist(string branch, string locale, string uid, CheckItemType type)
        {
            var path = type == CheckItemType.Module ? $"modules/{uid}" : $"units/{uid}";
            var url = $"{BuildServiceEndpoint()}/route/docs/api/hierarchy/{path}?branch={branch}&locale={locale}";
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.TryAddWithoutValidation("Referer", "https://tcexplorer.azurewebsites.net");

            var response = await SendRequest(request);

            Console.WriteLine("[LearnValidationPlugin] check {0} call: {1}", type, response.RequestMessage.RequestUri.AbsoluteUri);
            Console.WriteLine("[LearnValidationPlugin] check {0} result: {1}", type, response.IsSuccessStatusCode);
            return response.IsSuccessStatusCode;
        }

        public void Dispose()
        {
            _http.Dispose();
        }

        private async Task<string> Fetch(
            string routePath,
            IReadOnlyDictionary<string, string>? headers = null,
            string? value404 = null,
            DocsEnvironment? environment = null)
        {
            Debug.Assert(routePath.StartsWith("/"));
            var url = BuildServiceEndpoint(environment) + routePath;
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

        private async Task<string> FetchValidationRules(string routePath, string repositoryUrl = "", string branch = "", DocsEnvironment? environment = null)
        {
            try
            {
                Debug.Assert(routePath.StartsWith("/"));
                var url = BuildServiceEndpoint(environment) + routePath;
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

                           request.Headers.TryAddWithoutValidation("X-Metadata-RepositoryUrl", repositoryUrl);
                           request.Headers.TryAddWithoutValidation("X-Metadata-RepositoryBranch", branch);

                           var response = await SendRequest(request, environment);
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

        private async Task<HttpResponseMessage> SendRequest(HttpRequestMessage request, DocsEnvironment? environment = null)
        {
            using (PerfScope.Start($"[{nameof(OpsConfigAdapter)}] Executing request '{request.Method} {request.RequestUri}'"))
            {
                // Default header which allows fallback to public data when credential is not provided.
                request.Headers.TryAddWithoutValidation("X-OP-FallbackToPublicData", true.ToString());

                _credentialProvider?.Invoke(request);

                await FillOpsToken(request.RequestUri.AbsoluteUri, request, environment);

                return await _http.SendAsync(request);
            }
        }

        private static string BuildServiceEndpoint(DocsEnvironment? environment = null)
        {
            return (environment ?? DocsEnvironment) switch
            {
                DocsEnvironment.Prod => DocsProdServiceEndpoint,
                DocsEnvironment.PPE => DocsPPEServiceEndpoint,
                DocsEnvironment.Internal => DocsInternalServiceEndpoint,
                DocsEnvironment.Perf => DocsPerfServiceEndpoint,
                _ => throw new NotSupportedException(),
            };
        }

        private static DocsEnvironment GetXrefMapEnvironment(string xrefEndpoint)
        {
            if (!string.IsNullOrEmpty(xrefEndpoint) &&
                string.Equals(xrefEndpoint.TrimEnd('/'), "https://xref.docs.microsoft.com", StringComparison.OrdinalIgnoreCase))
            {
                return DocsEnvironment.Prod;
            }
            return DocsEnvironment;
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
            // don't access key vault for osx since azure-cli will crash in osx
            // https://github.com/Azure/azure-cli/issues/7519
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                && url.StartsWith(BuildServiceEndpoint(environment))
                && !request.Headers.Contains("X-OP-BuildUserToken"))
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
