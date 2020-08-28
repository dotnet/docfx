// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;

namespace Microsoft.Docs.Build
{
    internal class OpsConfigAdapter
    {
        public const string BuildConfigApi = "https://ops/buildconfig/";
        private const string MonikerDefinitionApi = "https://ops/monikerDefinition/";
        private const string MetadataSchemaApi = "https://ops/metadataschema/";
        private const string MarkdownValidationRulesApi = "https://ops/markdownvalidationrules/";
        private const string AllowlistsApi = "https://ops/allowlists/";
        private const string DisallowlistsApi = "https://ops/disallowlists/";
        private const string RegressionAllContentRulesApi = "https://ops/regressionallcontentrules/";
        private const string RegressionAllMetadataSchemaApi = "https://ops/regressionallmetadataschema/";

        private readonly (string, Func<HttpRequestMessage, Task<string>>)[] _apis;
        private readonly OpsInterceptor _opsInterceptor;

        public OpsConfigAdapter(OpsInterceptor opsInterceptor)
        {
            _opsInterceptor = opsInterceptor;
            _apis = new (string, Func<HttpRequestMessage, Task<string>>)[]
            {
                (BuildConfigApi, GetBuildConfig),
                (MonikerDefinitionApi, _ => GetMonikerDefinition()),
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
                    return new HttpResponseMessage { Content = new StringContent(await rule(request)) };
                }
            }
            return null;
        }

        private async Task<string> GetBuildConfig(HttpRequestMessage request)
        {
            var url = request.RequestUri;
            var queries = HttpUtility.ParseQueryString(url.Query);
            var name = queries["name"];
            var repository = queries["repository_url"];
            var branch = queries["branch"];
            var locale = queries["locale"];
            var xrefEndpoint = queries["xref_endpoint"];
            var xrefQueryTags = string.IsNullOrEmpty(queries["xref_query_tags"]) ? new List<string>() : queries["xref_query_tags"].Split(',').ToList();

            var fetchUrl = $"/v2/Queries/Docsets?git_repo_url={repository}&docset_query_status=Created";
            var docsetInfo = await _opsInterceptor.Fetch(fetchUrl, value404: "[]");
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
            var xrefMapBuildServiceEndpoint = GetXrefMapBuildServiceEndpoint(xrefEndpoint);
            if (!string.IsNullOrEmpty(docset.base_path))
            {
                xrefQueryTags.Add(docset.base_path.ValueWithLeadingSlash);
            }
            var xrefMaps = new List<string>();
            foreach (var tag in xrefQueryTags)
            {
                var links = await GetXrefMaps(tag, xrefMapQueryParams, xrefMapBuildServiceEndpoint);
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

        private static DocsEnvironment? GetXrefMapBuildServiceEndpoint(string xrefEndpoint)
        {
            if (!string.IsNullOrEmpty(xrefEndpoint) &&
                string.Equals(xrefEndpoint.TrimEnd('/'), "https://xref.docs.microsoft.com", StringComparison.OrdinalIgnoreCase))
            {
                return DocsEnvironment.Prod;
            }
            return null;
        }

        private async Task<string[]> GetXrefMaps(string tag, string xrefMapQueryParams, DocsEnvironment? xrefMapBuildServiceEndpoint)
        {
            var url = $"/v1/xrefmap{tag}{xrefMapQueryParams}";
            var response = await _opsInterceptor.Fetch(url, value404: "{}", environment: xrefMapBuildServiceEndpoint);
            return JsonConvert.DeserializeAnonymousType(response, new { links = new[] { "" } }).links
                ?? Array.Empty<string>();
        }

        private Task<string> GetMonikerDefinition()
        {
            return _opsInterceptor.Fetch($"/v2/monikertrees/allfamiliesproductsmonikers");
        }

        private async Task<string> GetMarkdownValidationRules(HttpRequestMessage request)
        {
            var headers = GetValidationServiceHeaders(request.RequestUri);

            return await _opsInterceptor.FetchValidationRules($"/route/validationmgt/rulesets/contentrules", headers);
        }

        private async Task<string> GetAllowlists(HttpRequestMessage request)
        {
            var headers = GetValidationServiceHeaders(request.RequestUri);

            return await _opsInterceptor.FetchValidationRules($"/route/validationmgt/validation/allowlists", headers);
        }

        private async Task<string> GetDisallowlists(HttpRequestMessage request)
        {
            var headers = GetValidationServiceHeaders(request.RequestUri);

            return await _opsInterceptor.FetchValidationRules($"/route/validationmgt/validation/disallowlists", headers);
        }

        private async Task<string> GetMetadataSchema(HttpRequestMessage request)
        {
            var headers = GetValidationServiceHeaders(request.RequestUri);
            var metadataRules = _opsInterceptor.FetchValidationRules($"/route/validationmgt/rulesets/metadatarules", headers);
            var allowlists = _opsInterceptor.FetchValidationRules($"/route/validationmgt/validation/allowlists", headers);

            return OpsMetadataRuleConverter.GenerateJsonSchema(await metadataRules, await allowlists);
        }

        private async Task<string> GetRegressionAllContentRules()
        {
            return await _opsInterceptor.FetchValidationRules(
                $"/route/validationmgt/rulesets/contentrules?name=_regression_all_", null, DocsEnvironment.PPE);
        }

        private async Task<string> GetRegressionAllMetadataSchema()
        {
            var metadataRules = _opsInterceptor.FetchValidationRules(
                $"/route/validationmgt/rulesets/metadatarules?name=_regression_all_", null, DocsEnvironment.PPE);
            var allowlists = _opsInterceptor.FetchValidationRules(
                $"/route/validationmgt/validation/allowlists", null, DocsEnvironment.PPE);

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

        private static string GetHostName(string siteName)
        {
            return siteName switch
            {
                "DocsAzureCN" => OpsInterceptor.DocsEnvironment switch
                {
                    DocsEnvironment.Prod => "docs.azure.cn",
                    DocsEnvironment.PPE => "ppe.docs.azure.cn",
                    DocsEnvironment.Internal => "ppe.docs.azure.cn",
                    DocsEnvironment.Perf => "ppe.docs.azure.cn",
                    _ => throw new NotSupportedException(),
                },
                "dev.microsoft.com" => OpsInterceptor.DocsEnvironment switch
                {
                    DocsEnvironment.Prod => "developer.microsoft.com",
                    DocsEnvironment.PPE => "devmsft-sandbox.azurewebsites.net",
                    DocsEnvironment.Internal => "devmsft-sandbox.azurewebsites.net",
                    DocsEnvironment.Perf => "devmsft-sandbox.azurewebsites.net",
                    _ => throw new NotSupportedException(),
                },
                "rd.microsoft.com" => OpsInterceptor.DocsEnvironment switch
                {
                    DocsEnvironment.Prod => "rd.microsoft.com",
                    _ => throw new NotSupportedException(),
                },
                _ => OpsInterceptor.DocsEnvironment switch
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
            return !IsLive(branch) && OpsInterceptor.DocsEnvironment == DocsEnvironment.Prod ? $"review.{GetHostName(siteName)}" : GetHostName(siteName);
        }

        private static bool IsLive(string branch)
        {
            return branch == "live" || branch == "live-sxs";
        }
    }
}
