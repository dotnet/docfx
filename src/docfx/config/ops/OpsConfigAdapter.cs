// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Web;
using Microsoft.Docs.Validation;
using Newtonsoft.Json;

namespace Microsoft.Docs.Build;

internal class OpsConfigAdapter
{
    public const string BuildConfigApi = "https://ops/buildconfig/";

    private const string AllowedDomain = "allowedDomain";
    private const string AllowedHtml = "allowedHTML";

    private const string MonikerDefinitionApi = "https://ops/monikerDefinition/";
    private const string OpsMetadataApi = "https://ops/opsmetadatas/";
    private const string PublicMetadataSchemaApi = "https://ops/publicmetadataschema/";
    private const string PublicMarkdownValidationRulesApi = "https://ops/publicmarkdownvalidationrules/";
    private const string PublicBuildValidationRulesApi = "https://ops/publicbuildvalidationrules/";
    private const string FullMetadataSchemaApi = "https://ops/fullmetadataschema/";
    private const string FullMarkdownValidationRulesApi = "https://ops/fullmarkdownvalidationrules/";
    private const string FullBuildValidationRulesApi = "https://ops/fullbuildvalidationrules/";
    private const string AllowlistsApi = "https://ops/taxonomy-allowlists/";
    private const string TrustedDomainApi = "https://ops/taxonomy-allowedDomain/";
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
                (PublicMetadataSchemaApi, url => _opsAccessor.GetMetadataSchema(GetValidationServiceParameters(url), fetchFullRules: false)),
                (PublicMarkdownValidationRulesApi, url => _opsAccessor.GetMarkdownValidationRules(GetValidationServiceParameters(url), fetchFullRules: false)),
                (PublicBuildValidationRulesApi, url => _opsAccessor.GetBuildValidationRules(GetValidationServiceParameters(url), fetchFullRules: false)),
                (FullMetadataSchemaApi, url => _opsAccessor.GetMetadataSchema(GetValidationServiceParameters(url), fetchFullRules: true)),
                (FullMarkdownValidationRulesApi, url => _opsAccessor.GetMarkdownValidationRules(GetValidationServiceParameters(url), fetchFullRules: true)),
                (FullBuildValidationRulesApi, url => _opsAccessor.GetBuildValidationRules(GetValidationServiceParameters(url), fetchFullRules: true)),
                (AllowlistsApi, _ => _opsAccessor.GetAllowlists()),
                (TrustedDomainApi, _ => _opsAccessor.GetTrustedDomain()),
                (SandboxEnabledModuleListApi, _ => _opsAccessor.GetSandboxEnabledModuleList()),
                (RegressionAllAllowlistsApi, _ => _opsAccessor.GetAllowlists(DocsEnvironment.PPE)),
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
        var repository = queries["publish_repository_url"] ?? "";
        var branch = queries["publish_repository_url"] != queries["repository_url"] ? "main" : queries["repository_branch"] ?? "";
        var locale = queries["locale"] ?? "";
        var xrefEndpoint = queries["xref_endpoint"] ?? "";
        var xrefQueryTags =
            (queries["xref_query_tags"] ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var getDocsetInfo = s_docsetInfoCache.GetOrAdd(repository, new Lazy<Task<string>>(() => _opsAccessor.GetDocsetInfo(repository)));
        var docsetInfo = await getDocsetInfo.Value;

        var docsets = JsonConvert.DeserializeAnonymousType(
            docsetInfo,
            new[] { new { name = "", base_path = default(BasePath), site_name = "", product_name = "", use_template = false } });

        var docset = docsets?.FirstOrDefault(d => string.Equals(d.name, name, StringComparison.OrdinalIgnoreCase));
        if (docsets is null || docset is null)
        {
            throw Errors.Config.DocsetNotProvisioned(name).ToException();
        }

        var metadataServiceQueryParams = $"?repository_url={HttpUtility.UrlEncode(repository)}&branch={HttpUtility.UrlEncode(branch)}";

        var xrefMapQueryParams = $"?site_name={docset.site_name}&branch_name={branch}&exclude_depot_name={docset.product_name}.{name}";
        if (!string.IsNullOrEmpty(docset.base_path))
        {
            xrefQueryTags.Add(docset.base_path.ValueWithLeadingSlash);

            // Handle share base path change during archive
            xrefQueryTags.Add($"/previous-versions{docset.base_path.ValueWithLeadingSlash}");
        }

        var xrefMaps = new List<string>();
        foreach (var tag in xrefQueryTags)
        {
            var links = await _opsAccessor.GetXrefMaps(tag, xrefEndpoint, xrefMapQueryParams);
            xrefMaps.AddRange(links);
        }

        var xrefHostName = GetXrefHostName(docset.site_name, branch);
        var documentUrls = JsonConvert.DeserializeAnonymousType(
                await _opsAccessor.GetDocumentUrls(), new[] { new { log_code = "", document_url = "" } })
            ?.ToDictionary(item => item.log_code, item => item.document_url);
        var trustedDomains = ConvertTrustedDomain(await _opsAccessor.GetTrustedDomain());
        var allowedHTML = ConvertAllowedHtml(await _opsAccessor.GetAllowedHtml());

        return JsonConvert.SerializeObject(new
        {
            product = docset.product_name,
            siteName = docset.site_name,
            hostName = GetHostName(docset.site_name),
            basePath = docset.base_path.ValueWithLeadingSlash,
            xrefHostName,
            monikerDefinition = MonikerDefinitionApi,
            documentUrls,
            markdownValidationRules = $"{PublicMarkdownValidationRulesApi}{metadataServiceQueryParams}",
            buildValidationRules = $"{PublicBuildValidationRulesApi}{metadataServiceQueryParams}",
            metadataSchema = new[]
            {
                OpsMetadataApi,
                $"{PublicMetadataSchemaApi}{metadataServiceQueryParams}",
            },
            allowlists = AllowlistsApi,
            allowedHTML,
            trustedDomains,
            sandboxEnabledModuleList = SandboxEnabledModuleListApi,
            xref = xrefMaps,
            isReferenceRepository = docsets.Any(d => d.use_template),
        });
    }

    private static Dictionary<string, HashSet<string>?> ConvertAllowedHtml(string json)
    {
        var taxonomies = JsonConvert.DeserializeObject<Taxonomies>(json) ?? new();
        if (taxonomies.TryGetValue(AllowedHtml, out var taxonomy))
        {
            var allowedHtml = taxonomy.NestedTaxonomy.dic
                .Select(item => (item.Key, Value: item.Value.Where(i => !"(empty)".Equals(i, StringComparison.OrdinalIgnoreCase)).ToArray()))
                .ToDictionary(
                    i => i.Key,
                    i => i.Value.Length > 0 ? new HashSet<string>(i.Value) : null);
            return allowedHtml;
        }

        return new();
    }

    private static Dictionary<string, string[]> ConvertTrustedDomain(string json)
    {
        var taxonomies = JsonConvert.DeserializeObject<Taxonomies>(json) ?? new();
        if (taxonomies.TryGetValue(AllowedDomain, out var taxonomy))
        {
            var cleanTrustedDomain = new Dictionary<string, string[]>();
            foreach (var item in taxonomy.NestedTaxonomy.dic)
            {
                // Remove '(empty)' entity passed from pool party
                var domainCol = (from domain in item.Value
                                 where !domain.Equals("(empty)", StringComparison.OrdinalIgnoreCase)
                                 select domain).ToArray();

                cleanTrustedDomain.Add(item.Key, domainCol);
            }
            return cleanTrustedDomain;
        }

        return new();
    }

    private static Task<string> GetOpsMetadata()
    {
        return File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "data/docs/metadata.json"));
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
                _ => "ppe.docs.azure.cn",
            },
            "dev.microsoft.com" => OpsAccessor.DocsEnvironment switch
            {
                DocsEnvironment.Prod => "developer.microsoft.com",
                _ => "devmsft-sandbox.azurewebsites.net",
            },
            "rd.microsoft.com" => "rd.microsoft.com",
            "Startups" => OpsAccessor.DocsEnvironment switch
            {
                DocsEnvironment.Prod => "startups.microsoft.com",
                _ => "ppe.startups.microsoft.com",
            },
            _ => OpsAccessor.DocsEnvironment switch
            {
                DocsEnvironment.Prod => "docs.microsoft.com",
                _ => "ppe.docs.microsoft.com",
            },
        };
    }

    private static string GetXrefHostName(string siteName, string branch)
    {
        return branch != "live" && OpsAccessor.DocsEnvironment == DocsEnvironment.Prod ? $"review.{GetHostName(siteName)}" : GetHostName(siteName);
    }
}
