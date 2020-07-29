// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using Azure;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Newtonsoft.Json;
using Polly;
using Polly.Extensions.Http;

namespace Microsoft.Docs.Build
{
    internal class OpsConfigAdapter : IDisposable
    {
        public const string BuildConfigApi = "https://ops/buildconfig/";
        private const string MonikerDefinitionApi = "https://ops/monikerDefinition/";
        private const string MetadataSchemaApi = "https://ops/metadataschema/";
        private const string MarkdownValidationRulesApi = "https://ops/markdownvalidationrules/";
        private const string AllowlistsApi = "https://ops/allowlists/";
        private const string DisallowlistsApi = "https://ops/disallowlists/";
        private const string RegressionAllContentRulesApi = "https://ops/regressionallcontentrules/";
        private const string RegressionAllMetadataSchemaApi = "https://ops/regressionallmetadataschema/";

        public static readonly DocsEnvironment DocsEnvironment = GetDocsEnvironment();
        private readonly Action<HttpRequestMessage> _credentialProvider;
        private readonly ErrorBuilder _errors;
        private readonly HttpClient _http = new HttpClient();
        private readonly (string, Func<Uri, Task<string>>)[] _apis;

        public OpsConfigAdapter(ErrorBuilder errors, Action<HttpRequestMessage> credentialProvider)
        {
            _errors = errors;
            _credentialProvider = credentialProvider;
            _apis = new (string, Func<Uri, Task<string>>)[]
            {
                (BuildConfigApi, GetBuildConfig),
                (MonikerDefinitionApi, GetMonikerDefinition),
                (MetadataSchemaApi, GetMetadataSchema),
                (MarkdownValidationRulesApi, GetMarkdownValidationRules),
                (AllowlistsApi, GetAllowlists),
                (DisallowlistsApi, GetDisallowlists),
                (RegressionAllContentRulesApi, _ => GetRegressionAllContentRules()),
                (RegressionAllMetadataSchemaApi, _ => GetRegressionAllMetadataSchema()),
            };
        }

        public async Task<HttpResponseMessage?> InterceptHttpRequest(HttpRequestMessage request)
        {
            foreach (var (baseUrl, rule) in _apis)
            {
                if (request.RequestUri.OriginalString.StartsWith(baseUrl))
                {
                    return new HttpResponseMessage { Content = new StringContent(await rule(request.RequestUri)) };
                }
            }
            return null;
        }

        public void Dispose()
        {
            _http.Dispose();
        }

        private async Task<string> GetBuildConfig(Uri url)
        {
            var queries = HttpUtility.ParseQueryString(url.Query);
            var name = queries["name"];
            var repository = queries["repository_url"];
            var branch = queries["branch"];
            var locale = queries["locale"];
            var xrefEndpoint = queries["xref_endpoint"];
            var xrefQueryTags = string.IsNullOrEmpty(queries["xref_query_tags"]) ? new List<string>() : queries["xref_query_tags"].Split(',').ToList();

            var fetchUrl = $"{BuildServiceEndpoint()}/v2/Queries/Docsets?git_repo_url={repository}&docset_query_status=Created";
            var docsetInfo = await Fetch(fetchUrl, value404: "[]");
            var docsets = JsonConvert.DeserializeAnonymousType(
                docsetInfo,
                new[] { new { name = "", base_path = default(BasePath), site_name = "", product_name = "", use_template = false } });

            var docset = docsets.FirstOrDefault(d => string.Equals(d.name, name, StringComparison.OrdinalIgnoreCase));
            if (docset is null)
            {
                throw Errors.Config.DocsetNotProvisioned(name).ToException(isError: false);
            }

            var metadataServiceQueryParams = $"?repository_url={HttpUtility.UrlEncode(repository)}&branch={HttpUtility.UrlEncode(branch)}";

            var xrefMapQueryParams = $"?site_name={docset.site_name}&branch_name={branch}&exclude_depot_name={docset.product_name}.{name}";
            var xrefMapApiEndpoint = GetXrefMapApiEndpoint(xrefEndpoint);
            if (!string.IsNullOrEmpty(docset.base_path))
            {
                xrefQueryTags.Add(docset.base_path.ValueWithLeadingSlash);
            }
            var xrefMaps = new List<string>();
            foreach (var tag in xrefQueryTags)
            {
                var links = await GetXrefMaps(xrefMapApiEndpoint, tag, xrefMapQueryParams);
                xrefMaps.AddRange(links);
            }

            var xrefHostName = GetXrefHostName(docset.site_name, branch);
            return JsonConvert.SerializeObject(new
            {
                product = docset.product_name,
                siteName = docset.site_name,
                hostName = GetHostName(docset.site_name),
                basePath = docset.base_path.ValueWithLeadingSlash,
                xrefHostName,
                monikerDefinition = MonikerDefinitionApi,
                markdownValidationRules = $"{MarkdownValidationRulesApi}{metadataServiceQueryParams}",
                metadataSchema = new[]
                {
                    Path.Combine(AppContext.BaseDirectory, "data/schemas/OpsMetadata.json"),
                    $"{MetadataSchemaApi}{metadataServiceQueryParams}",
                },
                allowlists = AllowlistsApi,
                disallowlists = DisallowlistsApi,
                xref = xrefMaps,
            });
        }

        private string GetXrefMapApiEndpoint(string xrefEndpoint)
        {
            var environment = DocsEnvironment;
            if (!string.IsNullOrEmpty(xrefEndpoint) &&
                string.Equals(xrefEndpoint.TrimEnd('/'), "https://xref.docs.microsoft.com", StringComparison.OrdinalIgnoreCase))
            {
                environment = DocsEnvironment.Prod;
            }
            return environment switch
            {
                DocsEnvironment.Prod => "https://op-build-prod.azurewebsites.net",
                DocsEnvironment.PPE => "https://op-build-sandbox2.azurewebsites.net",
                DocsEnvironment.Internal => "https://op-build-internal.azurewebsites.net",
                DocsEnvironment.Perf => "https://op-build-perf.azurewebsites.net",
                _ => throw new NotSupportedException(),
            };
        }

        private async Task<string[]> GetXrefMaps(string xrefMapApiEndpoint, string tag, string xrefMapQueryParams)
        {
            var url = $"{xrefMapApiEndpoint}/v1/xrefmap{tag}{xrefMapQueryParams}";
            var response = await Fetch(url, value404: "{}");
            return JsonConvert.DeserializeAnonymousType(response, new { links = new[] { "" } }).links
                ?? Array.Empty<string>();
        }

        private Task<string> GetMonikerDefinition(Uri url)
        {
            return Fetch($"{BuildServiceEndpoint()}/v2/monikertrees/allfamiliesproductsmonikers");
        }

        private async Task<string> GetMarkdownValidationRules(Uri url)
        {
            var headers = GetValidationServiceHeaders(url);

            return await FetchValidationRules($"{ValidationServiceEndpoint()}/rulesets/contentrules", headers);
        }

        private async Task<string> GetAllowlists(Uri url)
        {
            var headers = GetValidationServiceHeaders(url);

            return await FetchValidationRules($"{ValidationServiceEndpoint()}/validation/allowlists", headers);
        }

        private async Task<string> GetDisallowlists(Uri url)
        {
            var headers = GetValidationServiceHeaders(url);

            return await FetchValidationRules($"{ValidationServiceEndpoint()}/validation/disallowlists", headers);
        }

        private async Task<string> GetMetadataSchema(Uri url)
        {
            var headers = GetValidationServiceHeaders(url);
            var metadataRules = FetchValidationRules($"{ValidationServiceEndpoint()}/rulesets/metadatarules", headers);
            var allowlists = FetchValidationRules($"{ValidationServiceEndpoint()}/validation/allowlists", headers);

            return OpsMetadataRuleConverter.GenerateJsonSchema(await metadataRules, await allowlists);
        }

        private async Task<string> GetRegressionAllContentRules()
        {
            return await FetchValidationRules(
                $"{ValidationServiceEndpoint(DocsEnvironment.PPE)}/rulesets/contentrules?name=_regression_all_", null, DocsEnvironment.PPE);
        }

        private async Task<string> GetRegressionAllMetadataSchema()
        {
            var metadataRules = FetchValidationRules(
                $"{ValidationServiceEndpoint(DocsEnvironment.PPE)}/rulesets/metadatarules?name=_regression_all_", null, DocsEnvironment.PPE);
            var allowlists = FetchValidationRules(
                $"{ValidationServiceEndpoint(DocsEnvironment.PPE)}/validation/allowlists", null, DocsEnvironment.PPE);

            return OpsMetadataRuleConverter.GenerateJsonSchema(await metadataRules, await allowlists);
        }

        private static Dictionary<string, string> GetValidationServiceHeaders(Uri url)
        {
            var queries = HttpUtility.ParseQueryString(url.Query);

            return new Dictionary<string, string>()
            {
                { "X-Metadata-RepositoryUrl", queries["repository_url"] },
                { "X-Metadata-RepositoryBranch", queries["branch"] },
            };
        }

        private async Task<string> FetchValidationRules(string url, IReadOnlyDictionary<string, string>? headers = null, DocsEnvironment? environment = null)
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

        private async Task<string> Fetch(string url, IReadOnlyDictionary<string, string>? headers = null, string? value404 = null)
        {
            using (PerfScope.Start($"[{nameof(OpsConfigAdapter)}] Fetching '{url}'"))
            using (var request = new HttpRequestMessage(HttpMethod.Get, url))
            {
                _credentialProvider?.Invoke(request);

                if (headers != null)
                {
                    foreach (var (key, value) in headers)
                    {
                        request.Headers.TryAddWithoutValidation(key, value);
                    }
                }

                await FillOpsToken(url, request);

                var response = await _http.SendAsync(request);

                if (value404 != null && response.StatusCode == HttpStatusCode.NotFound)
                {
                    return value404;
                }
                return await response.EnsureSuccessStatusCode().Content.ReadAsStringAsync();
            }
        }

        private static string BuildServiceEndpoint(DocsEnvironment? environment = null)
            => (environment ?? DocsEnvironment) switch
            {
                DocsEnvironment.Prod => "https://op-build-prod.azurewebsites.net",
                DocsEnvironment.PPE => "https://op-build-sandbox2.azurewebsites.net",
                DocsEnvironment.Internal => "https://op-build-internal.azurewebsites.net",
                DocsEnvironment.Perf => "https://op-build-perf.azurewebsites.net",
                _ => throw new NotSupportedException(),
            };

        private static string ValidationServiceEndpoint(DocsEnvironment? environment = null) =>
            $"{BuildServiceEndpoint(environment)}/route/validationmgt";

        private static Lazy<Task<Response<KeyVaultSecret>>> OpBuildUserToken(DocsEnvironment? environment = null)
            => new Lazy<Task<Response<KeyVaultSecret>>>(
            () => new SecretClient(new Uri("https://docfx.vault.azure.net"), new DefaultAzureCredential()).GetSecretAsync(
            (environment ?? DocsEnvironment) switch
            {
                DocsEnvironment.Prod => "OpsBuildTokenProd",
                DocsEnvironment.PPE => "OpsBuildTokenSandbox",
                _ => throw new NotSupportedException(),
            }));

        private static string GetHostName(string siteName)
        {
            return siteName switch
            {
                "DocsAzureCN" => DocsEnvironment switch
                {
                    DocsEnvironment.Prod => "docs.azure.cn",
                    DocsEnvironment.PPE => "ppe.docs.azure.cn",
                    DocsEnvironment.Internal => "ppe.docs.azure.cn",
                    DocsEnvironment.Perf => "ppe.docs.azure.cn",
                    _ => throw new NotSupportedException(),
                },
                "dev.microsoft.com" => DocsEnvironment switch
                {
                    DocsEnvironment.Prod => "developer.microsoft.com",
                    DocsEnvironment.PPE => "devmsft-sandbox.azurewebsites.net",
                    DocsEnvironment.Internal => "devmsft-sandbox.azurewebsites.net",
                    DocsEnvironment.Perf => "devmsft-sandbox.azurewebsites.net",
                    _ => throw new NotSupportedException(),
                },
                "rd.microsoft.com" => DocsEnvironment switch
                {
                    DocsEnvironment.Prod => "rd.microsoft.com",
                    _ => throw new NotSupportedException(),
                },
                _ => DocsEnvironment switch
                {
                    DocsEnvironment.Prod => "docs.microsoft.com",
                    DocsEnvironment.PPE => "ppe.docs.microsoft.com",
                    DocsEnvironment.Internal => "ppe.docs.microsoft.com",
                    DocsEnvironment.Perf => "ppe.docs.microsoft.com",
                    _ => throw new NotSupportedException(),
                },
            };
        }

        private static string GetXrefHostName(string siteName, string branch)
        {
            return !IsLive(branch) && DocsEnvironment == DocsEnvironment.Prod ? $"review.{GetHostName(siteName)}" : GetHostName(siteName);
        }

        private static bool IsLive(string branch)
        {
            return branch == "live" || branch == "live-sxs";
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
                    request.Headers.Add("X-OP-BuildUserToken", (await OpBuildUserToken(environment).Value).Value.Value);
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
