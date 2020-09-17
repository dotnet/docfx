// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
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
            var fetchUrl = $"{BuildServiceEndpoint()}/v2/Queries/Docsets?git_repo_url={repositoryUrl}&docset_query_status=Created";
            return await Fetch(fetchUrl, value404: "[]");
        }

        public Task<string> GetMonikerDefinition()
        {
            return Fetch($"{BuildServiceEndpoint()}/v2/monikertrees/allfamiliesproductsmonikers");
        }

        public async Task<string[]> GetXrefMaps(string tag, string xrefMapQueryParams, DocsEnvironment? xrefMapBuildServiceEndpoint)
        {
            var response = await Fetch($"{BuildServiceEndpoint(xrefMapBuildServiceEndpoint)}/v1/xrefmap{tag}{xrefMapQueryParams}", value404: "{}");
            return JsonConvert.DeserializeAnonymousType(response, new { links = new[] { "" } }).links
                ?? Array.Empty<string>();
        }

        public async Task<string> GetMarkdownValidationRules((string repositoryUrl, string branch) tuple)
        {
            return await FetchValidationRules($"{BuildServiceEndpoint()}/route/validationmgt/rulesets/contentrules", tuple.repositoryUrl, tuple.branch);
        }

        public async Task<string> GetAllowlists((string repositoryUrl, string branch) tuple)
        {
            return await FetchValidationRules($"{BuildServiceEndpoint()}/route/validationmgt/validation/allowlists", tuple.repositoryUrl, tuple.branch);
        }

        public async Task<string> GetDisallowlists((string repositoryUrl, string branch) tuple)
        {
            return await FetchValidationRules($"{BuildServiceEndpoint()}/route/validationmgt/validation/disallowlists", tuple.repositoryUrl, tuple.branch);
        }

        public async Task<string> GetMetadataSchema((string repositoryUrl, string branch) tuple)
        {
            var metadataRules = FetchValidationRules($"{BuildServiceEndpoint()}/route/validationmgt/rulesets/metadatarules", tuple.repositoryUrl, tuple.branch);
            var allowlists = FetchValidationRules($"{BuildServiceEndpoint()}/route/validationmgt/validation/allowlists", tuple.repositoryUrl, tuple.branch);

            return OpsMetadataRuleConverter.GenerateJsonSchema(await metadataRules, await allowlists);
        }

        public async Task<string> GetRegressionAllContentRules()
        {
            return await FetchValidationRules(
                $"{BuildServiceEndpoint(DocsEnvironment.PPE)}/route/validationmgt/rulesets/contentrules?name=_regression_all_",
                environment: DocsEnvironment.PPE);
        }

        public async Task<string> GetRegressionAllMetadataSchema()
        {
            var metadataRules = FetchValidationRules(
                $"{BuildServiceEndpoint(DocsEnvironment.PPE)}/route/validationmgt/rulesets/metadatarules?name=_regression_all_",
                environment: DocsEnvironment.PPE);
            var allowlists = FetchValidationRules(
                $"{BuildServiceEndpoint(DocsEnvironment.PPE)}/route/validationmgt/validation/allowlists", environment: DocsEnvironment.PPE);

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

        private async Task<string> Fetch(string url, IReadOnlyDictionary<string, string>? headers = null, string? value404 = null)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            if (headers != null)
            {
                foreach (var (key, value) in headers)
                {
                    request.Headers.TryAddWithoutValidation(key, value);
                }
            }
            var response = await SendRequest(request);

            if (value404 != null && response.StatusCode == HttpStatusCode.NotFound)
            {
                return value404;
            }
            return await response.EnsureSuccessStatusCode().Content.ReadAsStringAsync();
        }

        private async Task<HttpResponseMessage> SendRequest(HttpRequestMessage request)
        {
            using (PerfScope.Start($"[{nameof(OpsConfigAdapter)}] Executing request '{request.Method} {request.RequestUri}'"))
            {
                _credentialProvider?.Invoke(request);

                await FillOpsToken(request.RequestUri.AbsoluteUri, request);

                return await _http.SendAsync(request);
            }
        }

        private async Task<string> FetchValidationRules(string url, string repositoryUrl = "", string branch = "", DocsEnvironment? environment = null)
        {
            try
            {
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

                           request.Headers.TryAddWithoutValidation("X-Metadata-RepositoryUrl", repositoryUrl);
                           request.Headers.TryAddWithoutValidation("X-Metadata-RepositoryBranch", branch);

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
