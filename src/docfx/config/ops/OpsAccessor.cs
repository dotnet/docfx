// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
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

        private static readonly SecretClient s_secretClientPublic = new(new(
            Environment.GetEnvironmentVariable("DOCS_KV_PROD_ENDPOINT") ?? "https://docfxdevkvpub.vault.azure.net/"), new DefaultAzureCredential());

        private static readonly SecretClient s_secretClientPubDev = new(new("https://docfxdevkvpubdev.vault.azure.net/"), new DefaultAzureCredential());

        private static readonly SecretClient s_secretClientPerf = new(new("https://kv-opbuild-test.vault.azure.net/"), new DefaultAzureCredential());

        private static readonly Lazy<Task<string>> s_opsTokenPublic = new(() => GetSecret("OpsBuildUserToken", DocsEnvironment.Prod));
        private static readonly Lazy<Task<string>> s_opsTokenPubDev = new(() => GetSecret("OpsBuildUserToken", DocsEnvironment.PPE));
        private static readonly Lazy<Task<string>> s_opsTokenPerf = new(() => GetSecret("OpsBuildUserToken", DocsEnvironment.Perf));

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
                DocsEnvironment.Prod => "https://docsvalidation.azurefd.net/errorcodes",
                _ => "https://docsvalidationppe.azurefd.net/errorcodes",
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

        public Task<string> GetMarkdownValidationRules((string repositoryUrl, string branch) tuple)
        {
            return FetchValidationRules($"/route/validationmgt/rulesets/contentrules", tuple.repositoryUrl, tuple.branch);
        }

        public Task<string> GetBuildValidationRules((string repositoryUrl, string branch) tuple)
        {
            return FetchValidationRules($"/route/validationmgt/rulesets/buildrules", tuple.repositoryUrl, tuple.branch);
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

        public async Task<string> GetMetadataSchema((string repositoryUrl, string branch) tuple)
        {
            var metadataRules = FetchValidationRules($"/route/validationmgt/rulesets/metadatarules", tuple.repositoryUrl, tuple.branch);
            var allowlists = GetAllowlists();

            return OpsMetadataRuleConverter.GenerateJsonSchema(await metadataRules, await allowlists);
        }

        public Task<string> GetRegressionAllContentRules()
        {
            return FetchValidationRules("/route/validationmgt/rulesets/contentrules?name=_regression_all_", environment: DocsEnvironment.PPE);
        }

        public async Task<string> GetRegressionAllMetadataSchema()
        {
            var metadataRules = FetchValidationRules("/route/validationmgt/rulesets/metadatarules?name=_regression_all_", environment: DocsEnvironment.PPE);
            var allowlists = GetAllowlists(DocsEnvironment.PPE);

            return OpsMetadataRuleConverter.GenerateJsonSchema(await metadataRules, await allowlists);
        }

        public Task<string> GetRegressionAllBuildRules()
        {
            return FetchValidationRules("/route/validationmgt/rulesets/buildrules?name=_regression_all_", environment: DocsEnvironment.PPE);
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

        private async Task<string> FetchValidationRules(string urlPath, string repositoryUrl = "", string branch = "", DocsEnvironment? environment = null)
        {
            try
            {
                return await FetchBuild(urlPath, environment: environment, middleware: async (request, next) =>
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
                });
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

        private Task<string> FetchBuild(string urlPath, string? value404 = null, DocsEnvironment? environment = null, HttpMiddleware? middleware = null)
        {
            return Fetch(BuildApi(environment) + urlPath, value404, BuildMiddleware(environment, middleware));
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

        private static HttpMiddleware BuildMiddleware(DocsEnvironment? environment = null, HttpMiddleware? middleware = null)
        {
            return async (request, next) =>
            {
                // Default header which allows fallback to public data when credential is not provided.
                request.Headers.TryAddWithoutValidation("X-OP-FallbackToPublicData", "True");

                // don't access key vault for osx since azure-cli will crash in osx
                // https://github.com/Azure/azure-cli/issues/7519
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX) &&
                    !request.Headers.Contains("X-OP-BuildUserToken"))
                {
                    // For development usage
                    try
                    {
                        environment ??= DocsEnvironment;
                        var token = environment switch
                        {
                            DocsEnvironment.Prod => s_opsTokenPublic,
                            DocsEnvironment.PPE => s_opsTokenPubDev,
                            DocsEnvironment.Perf => s_opsTokenPerf,
                            _ => throw new InvalidOperationException(),
                        };

                        request.Headers.Add("X-OP-BuildUserToken", await token.Value);
                    }
                    catch (Exception ex)
                    {
                        Log.Write($"Cannot get 'OPBuildUserToken' from azure key vault, please make sure you have been granted the permission to access.");
                        Log.Write(ex);
                    }
                }

                return await (middleware != null ? middleware(request, next) : next(request));
            };
        }

        private static string BuildApi(DocsEnvironment? environment = null)
        {
            return (environment ?? DocsEnvironment) switch
            {
                DocsEnvironment.Prod => "https://buildapi.docs.microsoft.com",
                DocsEnvironment.PPE => "https://BuildApiPubDev.azurefd.net",
                DocsEnvironment.Perf => "https://op-build-test.azurewebsites.net",
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

        private static async Task<string> GetSecret(string secret, DocsEnvironment environment = DocsEnvironment.Prod)
        {
            var response = environment switch
            {
                DocsEnvironment.Prod => await s_secretClientPublic.GetSecretAsync(secret),
                DocsEnvironment.PPE => await s_secretClientPubDev.GetSecretAsync(secret),
                DocsEnvironment.Perf => await s_secretClientPerf.GetSecretAsync(secret),
                _ => throw new InvalidOperationException(),
            };

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
    }
}
