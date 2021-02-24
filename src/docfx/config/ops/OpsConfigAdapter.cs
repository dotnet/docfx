// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
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
        private const string OpsMetadataApi = "https://ops/opsmetadatas/";
        private const string MetadataSchemaApi = "https://ops/metadataschema/";
        private const string MarkdownValidationRulesApi = "https://ops/markdownvalidationrules/";
        private const string BuildValidationRulesApi = "https://ops/buildvalidationrules/";
        private const string AllowlistsApi = "https://ops/taxonomy-allowlists/";
        private const string SandboxEnabledModuleListApi = "https://ops/sandboxEnabledModuleList/";
        private const string RegressionAllAllowlistsApi = "https://ops/regressionalltaxonomy-allowlists/";
        private const string RegressionAllContentRulesApi = "https://ops/regressionallcontentrules/";
        private const string RegressionAllMetadataSchemaApi = "https://ops/regressionallmetadataschema/";
        private const string RegressionAllBuildRulesApi = "https://ops/regressionallbuildrules/";

        private readonly (string, Func<Uri, Task<string>>)[] _apis;
        private readonly OpsAccessor _opsAccessor;

        private static readonly ConcurrentDictionary<string, Lazy<Task<string>>> s_docsetInfoCache = new();

        public OpsConfigAdapter(OpsAccessor opsAccessor)
        {
            _opsAccessor = opsAccessor;
            _apis = new (string, Func<Uri, Task<string>>)[]
            {
                (BuildConfigApi, GetBuildConfig),
                (MonikerDefinitionApi, _ => _opsAccessor.GetMonikerDefinition()),
                (OpsMetadataApi, _ => GetOpsMetadata()),
                (MetadataSchemaApi, url => _opsAccessor.GetMetadataSchema(GetValidationServiceParameters(url))),
                (MarkdownValidationRulesApi, url => _opsAccessor.GetMarkdownValidationRules(GetValidationServiceParameters(url))),
                (BuildValidationRulesApi, url => _opsAccessor.GetBuildValidationRules(GetValidationServiceParameters(url))),
                (AllowlistsApi, _ => _opsAccessor.GetAllowlists()),
                (SandboxEnabledModuleListApi, _ => _opsAccessor.GetSandboxEnabledModuleList()),
                (RegressionAllAllowlistsApi, _ => _opsAccessor.GetRegressionAllAllowlists()),
                (RegressionAllContentRulesApi, _ => _opsAccessor.GetRegressionAllContentRules()),
                (RegressionAllBuildRulesApi, _ => _opsAccessor.GetRegressionAllBuildRules()),
                (RegressionAllMetadataSchemaApi, _ => _opsAccessor.GetRegressionAllMetadataSchema()),
            };
        }

        public async Task<HttpResponseMessage?> InterceptHttpRequest(HttpRequestMessage request)
        {
            foreach (var (baseUrl, rule) in _apis)
            {
                if (request.RequestUri is Uri uri && uri.ToString().StartsWith(baseUrl))
                {
                    return new HttpResponseMessage { Content = new StringContent(await rule(request.RequestUri)) };
                }
            }
            return null;
        }

        private async Task<string> GetBuildConfig(Uri url)
        {
            var queries = HttpUtility.ParseQueryString(url.Query);
            var name = queries["name"] ?? "";
            var repository = queries["repository_url"] ?? "";
            var branch = queries["branch"] ?? "";
            var locale = queries["locale"] ?? "";
            var xrefEndpoint = queries["xref_endpoint"] ?? "";
            var xrefQueryTags = (queries["xref_query_tags"] ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

            var allDocsets = JsonConvert.DeserializeAnonymousType(
                File.ReadAllText("C:/docsetinfo.json"),
                Enumerable.Repeat(new[] { new { name = "", base_path = default(BasePath), site_name = "", product_name = "" } }, 1)
                .ToDictionary(_ => "")).ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.OrdinalIgnoreCase);

            if (!repository.StartsWith("https://github.com/"))
            {
                if (!allDocsets.ContainsKey(repository))
                {
                    var repositoryUri = new Uri(repository);
                    var uriSchemePrefix = Uri.UriSchemeHttp + Uri.SchemeDelimiter;
                    repository = new Uri(new Uri(new Uri(uriSchemePrefix + repositoryUri.Host), "DefaultCollection") + repositoryUri.AbsolutePath).AbsoluteUri;
                }
            }

            if (!allDocsets.ContainsKey(repository))
            {
                throw new InvalidOperationException($"Docet info not found for {repository}");
            }

            var docsets = allDocsets[repository];
            var docset = docsets.FirstOrDefault(d => string.Equals(d.name, name, StringComparison.OrdinalIgnoreCase));
            if (docset is null || string.IsNullOrEmpty(docset.name))
            {
                throw Errors.Config.DocsetNotProvisioned(name).ToException();
            }

            var metadataServiceQueryParams = $"?repository_url={HttpUtility.UrlEncode(repository)}&branch={HttpUtility.UrlEncode(branch)}";

            var xrefHostName = GetXrefHostName(docset.site_name, branch);
            return JsonConvert.SerializeObject(new
            {
                product = docset.product_name,
                siteName = docset.site_name,
                hostName = GetHostName(docset.site_name),
                basePath = docset.base_path.ValueWithLeadingSlash,
                xrefHostName,
                monikerDefinition = "C:/monikerdefinition.json",
                markdownValidationRules = $"{MarkdownValidationRulesApi}{metadataServiceQueryParams}",
                buildValidationRules = $"{BuildValidationRulesApi}{metadataServiceQueryParams}",
                metadataSchema = new[]
                {
                    OpsMetadataApi,
                    $"{MetadataSchemaApi}{metadataServiceQueryParams}",
                },
                allowlists = AllowlistsApi,
                sandboxEnabledModuleList = SandboxEnabledModuleListApi,
            });
        }

        private static Task<string> GetOpsMetadata()
        {
            return File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "data/schemas/OpsMetadata.json"));
        }

        private static (string repository, string branch) GetValidationServiceParameters(Uri url)
        {
            var queries = HttpUtility.ParseQueryString(url.Query);

            return (queries["repository_url"] ?? "", queries["branch"] ?? "");
        }

        private static string GetHostName(string siteName)
        {
            return siteName switch
            {
                "DocsAzureCN" => OpsAccessor.DocsEnvironment switch
                {
                    DocsEnvironment.Prod => "docs.azure.cn",
                    DocsEnvironment.PPE => "ppe.docs.azure.cn",
                    DocsEnvironment.Internal => "ppe.docs.azure.cn",
                    DocsEnvironment.Perf => "ppe.docs.azure.cn",
                    _ => throw new NotSupportedException(),
                },
                "dev.microsoft.com" => OpsAccessor.DocsEnvironment switch
                {
                    DocsEnvironment.Prod => "developer.microsoft.com",
                    DocsEnvironment.PPE => "devmsft-sandbox.azurewebsites.net",
                    DocsEnvironment.Internal => "devmsft-sandbox.azurewebsites.net",
                    DocsEnvironment.Perf => "devmsft-sandbox.azurewebsites.net",
                    _ => throw new NotSupportedException(),
                },
                "rd.microsoft.com" => OpsAccessor.DocsEnvironment switch
                {
                    DocsEnvironment.Prod => "rd.microsoft.com",
                    _ => throw new NotSupportedException(),
                },
                "Startups" => OpsAccessor.DocsEnvironment switch
                {
                    DocsEnvironment.Prod => "startups.microsoft.com",
                    DocsEnvironment.PPE => "ppe.startups.microsoft.com",
                    _ => throw new NotSupportedException(),
                },
                _ => OpsAccessor.DocsEnvironment switch
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
            return !IsLive(branch) && OpsAccessor.DocsEnvironment == DocsEnvironment.Prod ? $"review.{GetHostName(siteName)}" : GetHostName(siteName);
        }

        private static bool IsLive(string branch)
        {
            return branch == "live" || branch == "live-sxs";
        }
    }
}
