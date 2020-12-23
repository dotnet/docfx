// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Docs.LearnValidation;
using Newtonsoft.Json;
using Polly;
using Polly.Extensions.Http;

namespace Microsoft.Docs.Build
{
    internal class OpsAccessor : ILearnServiceAccessor
    {
        private const string TaxonomyServiceProdPath = "https://taxonomyservice.azurefd.net/taxonomies/simplified?" +
            "name=ms.author&name=ms.devlang&name=ms.prod&name=ms.service&name=ms.topic&name=devlang&name=product";

        private const string TaxonomyServicePPEPath = "https://taxonomyserviceppe.azurefd.net/taxonomies/simplified?" +
            "name=ms.author&name=ms.devlang&name=ms.prod&name=ms.service&name=ms.topic&name=devlang&name=product";

        private const string SandboxEnabledModuleListPath = "https://docs.microsoft.com/api/resources/sandbox/verify";

        public static readonly DocsEnvironment DocsEnvironment = GetDocsEnvironment();

        // TODO: use Azure front door endpoint when it is stable
        public static readonly string DocsProdServiceEndpoint =
            Environment.GetEnvironmentVariable("DOCS_PROD_SERVICE_ENDPOINT") ?? "https://op-build-prod.azurewebsites.net";

        public static readonly string DocsPPEServiceEndpoint =
            Environment.GetEnvironmentVariable("DOCS_PPE_SERVICE_ENDPOINT") ?? "https://op-build-sandbox2.azurewebsites.net";

        public static readonly string DocsInternalServiceEndpoint =
            Environment.GetEnvironmentVariable("DOCS_INTERNAL_SERVICE_ENDPOINT") ?? "https://op-build-internal.azurewebsites.net";

        public static readonly string DocsPerfServiceEndpoint =
            Environment.GetEnvironmentVariable("DOCS_PERF_SERVICE_ENDPOINT") ?? "https://op-build-perf.azurewebsites.net";

        private readonly ErrorBuilder _errors;
        private readonly HttpClient _httpClient;
        private readonly HttpClient _opsHttpClient;

        public OpsAccessor(ErrorBuilder errors, CredentialHandler credentialHandler)
        {
            _errors = errors;
            _httpClient = new(credentialHandler);
#pragma warning disable CA2000 // Dispose objects before losing scope
            _opsHttpClient = new HttpClient(credentialHandler.Create(new OpsCredentialHandler(new HttpClientHandler())), true);
#pragma warning restore CA2000 // Dispose objects before losing scope
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

        public async Task<string> GetBuildValidationRules((string repositoryUrl, string branch) tuple)
        {
            return await FetchValidationRules($"/route/validationmgt/rulesets/buildrules", tuple.repositoryUrl, tuple.branch);
        }

        public async Task<string> GetAllowlists()
        {
            return await FetchTaxonomies();
        }

        public async Task<string> GetSandboxEnabledModuleList()
        {
            return await FetchGetSandboxEnabledModuleList();
        }

        public async Task<string> GetRegressionAllAllowlists()
        {
            return await FetchTaxonomies(DocsEnvironment.PPE);
        }

        public async Task<string> GetMetadataSchema((string repositoryUrl, string branch) tuple)
        {
            var metadataRules = FetchValidationRules($"/route/validationmgt/rulesets/metadatarules", tuple.repositoryUrl, tuple.branch);
            var allowlists = FetchTaxonomies();

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

            var allowlists = FetchTaxonomies(DocsEnvironment.PPE);

            return OpsMetadataRuleConverter.GenerateJsonSchema(await metadataRules, await allowlists);
        }

        public async Task<string> GetRegressionAllBuildRules()
        {
            return await FetchValidationRules(
                "/route/validationmgt/rulesets/buildrules?name=_regression_all_",
                environment: DocsEnvironment.PPE);
        }

        public async Task<string> HierarchyDrySync(string body)
        {
            using var request = new HttpRequestMessage
            {
                RequestUri = new Uri($"{BuildServiceEndpoint()}/route/mslearnhierarchy/api/OnDemandHierarchyDrySync"),
                Method = HttpMethod.Post,
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };

            var response = await _opsHttpClient.SendAsync(request);
            return await response.EnsureSuccessStatusCode().Content.ReadAsStringAsync();
        }

        public async Task<bool> CheckLearnPathItemExist(string branch, string locale, string uid, CheckItemType type)
        {
            var path = type == CheckItemType.Module ? $"modules/{uid}" : $"units/{uid}";
            var url = $"{BuildServiceEndpoint()}/route/docs/api/hierarchy/{path}?branch={branch}&locale={locale}";
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.TryAddWithoutValidation("Referer", "https://tcexplorer.azurewebsites.net");

            var response = await _opsHttpClient.SendAsync(request);

            Console.WriteLine("[LearnValidationPlugin] check {0} call: {1}", type, url);
            Console.WriteLine("[LearnValidationPlugin] check {0} result: {1}", type, response.IsSuccessStatusCode);
            return response.IsSuccessStatusCode;
        }

        public static DocsEnvironment ExtractDocsEnvironmentFromUrl(string? url)
        {
            if (string.IsNullOrEmpty(url))
            {
                throw new NotSupportedException();
            }

            if (url.StartsWith(DocsProdServiceEndpoint))
            {
                return DocsEnvironment.Prod;
            }
            else if (url.StartsWith(DocsPPEServiceEndpoint))
            {
                return DocsEnvironment.PPE;
            }
            else if (url.StartsWith(DocsInternalServiceEndpoint))
            {
                return DocsEnvironment.Internal;
            }
            else if (url.StartsWith(DocsPerfServiceEndpoint))
            {
                return DocsEnvironment.Perf;
            }
            else
            {
                throw new NotSupportedException();
            }
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
            var response = await _opsHttpClient.SendAsync(request);

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

                           var response = await _opsHttpClient.SendAsync(request);
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

        private async Task<string> FetchTaxonomies(DocsEnvironment? environment = null)
        {
            try
            {
                var url = TaxonomyServicePath(environment);
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
                           request.Headers.TryAddWithoutValidation("User-Agent", "Docfx v3");
                           var response = await _httpClient.SendAsync(request);
                           return response;
                       });

                    return await response.EnsureSuccessStatusCode().Content.ReadAsStringAsync();
                }
            }
            catch (Exception ex)
            {
                // Getting taxonomies failure should not block build proceeding,
                // catch and log the exception without rethrow.
                Log.Write(ex);
                _errors.Add(Errors.System.ValidationIncomplete());
                return "{}";
            }
        }

        private async Task<string> FetchGetSandboxEnabledModuleList()
        {
            try
            {
                var url = SandboxEnabledModuleListPath;
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
                           request.Headers.TryAddWithoutValidation("User-Agent", "Docfx v3");
                           var response = await _httpClient.SendAsync(request);
                           return response;
                       });

                    return await response.EnsureSuccessStatusCode().Content.ReadAsStringAsync();
                }
            }
            catch (Exception ex)
            {
                // Getting taxonomies failure should not block build proceeding,
                // catch and log the exception without rethrow.
                Log.Write(ex);
                _errors.Add(Errors.System.ValidationIncomplete());
                return "{}";
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

        private static string TaxonomyServicePath(DocsEnvironment? environment = null)
        {
            return (environment ?? DocsEnvironment) switch
            {
                DocsEnvironment.Prod => TaxonomyServiceProdPath,
                DocsEnvironment.PPE => TaxonomyServicePPEPath,
                DocsEnvironment.Internal => TaxonomyServicePPEPath,
                DocsEnvironment.Perf => TaxonomyServicePPEPath,
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

        private static DocsEnvironment GetDocsEnvironment()
        {
            return Enum.TryParse(Environment.GetEnvironmentVariable("DOCS_ENVIRONMENT"), true, out DocsEnvironment docsEnvironment)
                ? docsEnvironment
                : DocsEnvironment.Prod;
        }
    }
}
