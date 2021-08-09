// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Identity;
using Microsoft.Docs.LearnValidation;
using Newtonsoft.Json;
using Polly;
using Polly.Extensions.Http;

namespace Microsoft.Docs.Build
{
    internal class OpsAccessor : ILearnServiceAccessor
    {
        private delegate Task<HttpResponseMessage> HttpMiddleware(HttpRequestMessage request, Func<HttpRequestMessage, Task<HttpResponseMessage>> next);

        public static readonly DocsEnvironment DocsEnvironment = GetDocsEnvironment();
        private static readonly CachedTokenCredential s_cachedTokenCredential = new(new DefaultAzureCredential());
        private static readonly string s_opsClientId = DocsEnvironment == DocsEnvironment.Prod
                ? "6befca88-4c28-430a-957e-f870b267bcfc"
                : "6ce33073-a071-4cf9-9936-f5a24d21a089";

        private static int s_validationRulesetReported;

        private readonly CredentialHandler _credentialHandler;
        private readonly ErrorBuilder _errors;
        private readonly HttpClient _http = new(new HttpClientHandler { CheckCertificateRevocationList = true });

        public OpsAccessor(ErrorBuilder errors, CredentialHandler credentialHandler)
        {
            _errors = errors;
            _credentialHandler = credentialHandler;
        }

        public Task<string> GetDocsetInfo(string repositoryUrl)
        {
            return FetchBuild($"/v2/Queries/Docsets?git_repo_url={repositoryUrl}&docset_query_status=Created", value404: "[]");
        }

        public Task<string> GetMonikerDefinition()
        {
            return FetchBuild("/v2/monikertrees/allfamiliesproductsmonikers");
        }

        public Task<string> GetDocumentUrls()
        {
            return Fetch(DocsEnvironment switch
            {
                DocsEnvironment.Prod => "https://docsvalidation-public.azurefd.net/errorcodes",
                _ => "https://docsvalidation-pubdev.azurefd.net/errorcodes",
            });
        }

        public async Task<string[]> GetXrefMaps(string tag, string xrefEndpoint, string xrefMapQueryParams)
        {
            var environment = xrefEndpoint.StartsWith("https://xref.docs.microsoft.com", StringComparison.OrdinalIgnoreCase)
                ? DocsEnvironment.Prod
                : DocsEnvironment;

            var response = await FetchBuild($"/v1/xrefmap{tag}{xrefMapQueryParams}", value404: "{}", environment);

            return JsonConvert.DeserializeAnonymousType(response, new { links = new[] { "" } })?.links ?? Array.Empty<string>();
        }

        public Task<string> GetMarkdownValidationRules((string repositoryUrl, string branch) tuple, bool fetchFullRules)
        {
            return FetchValidationRules($"/rulesets/contentrules", fetchFullRules, tuple.repositoryUrl, tuple.branch);
        }

        public Task<string> GetBuildValidationRules((string repositoryUrl, string branch) tuple, bool fetchFullRules)
        {
            return FetchValidationRules($"/rulesets/buildrules", fetchFullRules, tuple.repositoryUrl, tuple.branch);
        }

        public Task<string> GetAllowlists(DocsEnvironment environment = DocsEnvironment.Prod)
        {
            return Fetch(TaxonomyApi(environment) +
                "/taxonomies/simplified?name=ms.author&name=ms.devlang&name=ms.prod&name=ms.service&name=ms.topic&name=devlang&name=product");
        }

        public Task<string> GetSandboxEnabledModuleList()
        {
            return Fetch("https://docs.microsoft.com/api/resources/sandbox/verify");
        }

        public async Task<string> GetMetadataSchema((string repositoryUrl, string branch) tuple, bool fetchFullRules)
        {
            var metadataRules = FetchValidationRules($"/rulesets/metadatarules", fetchFullRules, tuple.repositoryUrl, tuple.branch);
            var allowlists = GetAllowlists();

            return OpsMetadataRuleConverter.GenerateJsonSchema(await metadataRules, await allowlists);
        }

        public Task<string> GetRegressionAllContentRules()
        {
            return FetchValidationRules("/rulesets/contentrules?name=_regression_all_", fetchFullRules: true, environment: DocsEnvironment.PPE);
        }

        public async Task<string> GetRegressionAllMetadataSchema()
        {
            var metadataRules = FetchValidationRules("/rulesets/metadatarules?name=_regression_all_", fetchFullRules: true, environment: DocsEnvironment.PPE);
            var allowlists = GetAllowlists(DocsEnvironment.PPE);

            return OpsMetadataRuleConverter.GenerateJsonSchema(await metadataRules, await allowlists);
        }

        public Task<string> GetRegressionAllBuildRules()
        {
            return FetchValidationRules("/rulesets/buildrules?name=_regression_all_", fetchFullRules: true, environment: DocsEnvironment.PPE);
        }

        public async Task<bool> CheckLearnPathItemExist(string branch, string locale, string uid, CheckItemType type)
        {
            var path = type == CheckItemType.Module ? $"modules/{uid}" : $"units/{uid}";
            var urlPath = $"/route/docs/api/hierarchy/{path}?branch={branch}&locale={locale}";

            return await FetchBuild(urlPath, value404: "404", middleware: (request, next) =>
            {
                request.Headers.Referrer = new("https://tcexplorer.azurewebsites.net");
                return next(request);
            }) != "404";
        }

        public Task<string> HierarchyDrySync(string body)
        {
            return Fetch(
                () => new HttpRequestMessage
                {
                    RequestUri = new Uri($"{BuildApi()}/route/mslearnhierarchy/api/OnDemandHierarchyDrySync"),
                    Method = HttpMethod.Post,
                    Content = new StringContent(body, Encoding.UTF8, "application/json"),
                },
                middleware: BuildMiddleware());
        }

        private async Task<string> FetchValidationRules(
            string urlPath,
            bool fetchFullRules,
            string repositoryUrl = "",
            string branch = "",
            DocsEnvironment? environment = null)
        {
            try
            {
                return fetchFullRules
                        ? await FetchBuild("/route/validationmgt" + urlPath, environment: environment, middleware: ValidationMiddleware)
                        : await Fetch(PublicValidationApi(environment) + urlPath, middleware: ValidationMiddleware);
            }
            catch (Exception ex)
            {
                // Getting validation rules failure should not block build proceeding,
                // catch and log the exception without rethrow.
                Log.Write(ex);
                _errors.Add(Errors.System.ValidationIncomplete());
                return "{}";
            }

            async Task<HttpResponseMessage> ValidationMiddleware(HttpRequestMessage request, Func<HttpRequestMessage, Task<HttpResponseMessage>> next)
            {
                request.Headers.TryAddWithoutValidation("X-Metadata-RepositoryUrl", repositoryUrl);
                request.Headers.TryAddWithoutValidation("X-Metadata-RepositoryBranch", branch);

                var response = await next(request);

                if (response.Headers.TryGetValues("X-Metadata-Version", out var metadataVersion) &&
                    Interlocked.Exchange(ref s_validationRulesetReported, 1) == 0)
                {
                    _errors.Add(Errors.System.MetadataValidationRuleset(string.Join(',', metadataVersion)));
                }

                return response;
            }
        }

        private Task<string> FetchBuild(string urlPath, string? value404 = null, DocsEnvironment? environment = null, HttpMiddleware? middleware = null)
        {
            return Fetch(BuildApi(environment) + urlPath, value404, BuildMiddleware(middleware));
        }

        private Task<string> Fetch(string url, string? value404 = null, HttpMiddleware? middleware = null)
        {
            return Fetch(() => new HttpRequestMessage(HttpMethod.Get, url), value404, middleware);
        }

        private async Task<string> Fetch(Func<HttpRequestMessage> requestFactory, string? value404 = null, HttpMiddleware? middleware = null)
        {
            using var response = await HttpPolicyExtensions
               .HandleTransientHttpError()
               .Or<OperationCanceledException>()
               .Or<IOException>()
               .RetryAsync(3)
               .ExecuteAsync(() => _credentialHandler.SendRequest(
                   requestFactory,
                   request => middleware != null ? middleware(request, SendRequest) : SendRequest(request)));

            if (value404 != null && response.StatusCode == HttpStatusCode.NotFound)
            {
                return value404;
            }

            return await response.EnsureSuccessStatusCode().Content.ReadAsStringAsync();

            async Task<HttpResponseMessage> SendRequest(HttpRequestMessage request)
            {
                using (PerfScope.Start($"[{nameof(OpsAccessor)}] '{request.Method} {UrlUtility.SanitizeUrl(request.RequestUri?.ToString())}'"))
                {
                    request.Headers.TryAddWithoutValidation("User-Agent", "docfx");
                    return await _http.SendAsync(request);
                }
            }
        }

        private static HttpMiddleware BuildMiddleware(HttpMiddleware? middleware = null)
        {
            return async (request, next) =>
            {
                // Default header which allows fallback to public data when credential is not provided.
                request.Headers.TryAddWithoutValidation("X-OP-FallbackToPublicData", "True");
                if (!request.Headers.Contains("X-OP-BuildUserToken"))
                {
                    request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {await GetAccessTokenAsync()}");
                }

                return await (middleware != null ? middleware(request, next) : next(request));
            };
        }

        private static async Task<string> GetAccessTokenAsync()
        {
            var accessToken = await s_cachedTokenCredential
                .GetTokenAsync(new TokenRequestContext(new[] { $"{s_opsClientId}/.default" }), default)
                .ConfigureAwait(false);
            return accessToken.Token;
        }

        private static string BuildApi(DocsEnvironment? environment = null)
        {
            return (environment ?? DocsEnvironment) switch
            {
                DocsEnvironment.Prod => "https://buildapi.docs.microsoft.com",
                DocsEnvironment.PPE => "https://buildapi.ppe.docs.microsoft.com",
                DocsEnvironment.Perf => "https://op-build-test.azurewebsites.net",
                _ => throw new InvalidOperationException(),
            };
        }

        private static string PublicValidationApi(DocsEnvironment? environment = null)
        {
            return (environment ?? DocsEnvironment) switch
            {
                DocsEnvironment.Prod => "https://docsvalidation-public.azurefd.net",
                DocsEnvironment.PPE => "https://docsvalidation-pubdev.azurefd.net",
                DocsEnvironment.Perf => "https://docsvalidation-pubdev.azurefd.net",
                _ => throw new InvalidOperationException(),
            };
        }

        private static string TaxonomyApi(DocsEnvironment? environment = null)
        {
            return (environment ?? DocsEnvironment) switch
            {
                DocsEnvironment.Prod => "https://taxonomyservice.azurefd.net",
                _ => "https://taxonomyserviceppe.azurefd.net",
            };
        }

        private static DocsEnvironment GetDocsEnvironment()
        {
            return Enum.TryParse(Environment.GetEnvironmentVariable("DOCS_ENVIRONMENT"), true, out DocsEnvironment docsEnvironment)
                ? docsEnvironment
                : DocsEnvironment.Prod;
        }
    }
}
