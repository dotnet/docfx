// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Azure.Core;
using Azure.Identity;
using Microsoft.Docs.LearnValidation;
using Newtonsoft.Json;
using Polly;
using Polly.Extensions.Http;

namespace Microsoft.Docs.Build;

internal class OpsAccessor : ILearnServiceAccessor
{
    private delegate Task<HttpResponseMessage> HttpMiddleware(HttpRequestMessage request, Func<HttpRequestMessage, Task<HttpResponseMessage>> next);

    public static readonly DocsEnvironment DocsEnvironment = GetDocsEnvironment();

    private readonly CredentialHandler _credentialHandler;
    private readonly ErrorBuilder _errors;
    private readonly HttpClient _http = new(new HttpClientHandler { CheckCertificateRevocationList = true });
    private readonly HttpClient _longHttp = new(new HttpClientHandler { CheckCertificateRevocationList = true });

    private static readonly Lazy<ValueTask<AccessToken>> s_accessTokenPublic = new(() => GetAccessTokenAsync(DocsEnvironment.Prod));
    private static readonly Lazy<ValueTask<AccessToken>> s_accessTokenPubDev = new(() => GetAccessTokenAsync(DocsEnvironment.PPE));
    private static readonly Lazy<ValueTask<AccessToken>> s_accessTokenPerf = new(() => GetAccessTokenAsync(DocsEnvironment.Perf));

    private static readonly bool s_fallbackToPublicData =
        bool.TryParse(Environment.GetEnvironmentVariable("DOCS_FALLBACK_TO_PUBLIC_DATA"), out var fallback) && fallback;

    public OpsAccessor(ErrorBuilder errors, CredentialHandler credentialHandler)
    {
        _errors = errors;
        _credentialHandler = credentialHandler;
        _longHttp.Timeout = TimeSpan.FromSeconds(300);
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
            "/taxonomies/simplified?name=ms.author&name=ms.devlang&name=ms.prod&name=ms.service&name=ms.topic&name=devlang&name=product&name=microsoft.domain");
    }

    public Task<string> GetTrustedDomain(DocsEnvironment environment = DocsEnvironment.Prod)
    {
        return Fetch(TaxonomyApi(environment) + "/taxonomies/simplified?name=allowedDomain");
    }

    public Task<string> GetAllowedHtml(DocsEnvironment environment = DocsEnvironment.Prod)
    {
        return Fetch(TaxonomyApi(environment) +
            "/taxonomies/simplified?name=allowedHTML");
    }

    public Task<string> GetSandboxEnabledModuleList()
    {
        return Fetch("https://docs.microsoft.com/api/resources/sandbox/verify");
    }

    public async Task<string> GetMetadataSchema((string repositoryUrl, string branch) tuple, bool fetchFullRules)
    {
        var metadataRules = FetchValidationRules($"/rulesets/metadatarules", fetchFullRules, tuple.repositoryUrl, tuple.branch);
        var allowlists = GetAllowlists();

        return OpsMetadataRuleConverter.GenerateJsonSchema(await metadataRules, await allowlists, _errors);
    }

    public Task<string> GetRegressionAllContentRules()
    {
        return FetchValidationRules("/rulesets/contentrules?name=_regression_all_", fetchFullRules: true, environment: DocsEnvironment.PPE);
    }

    public async Task<string> GetRegressionAllMetadataSchema()
    {
        var metadataRules = FetchValidationRules("/rulesets/metadatarules?name=_regression_all_", fetchFullRules: true, environment: DocsEnvironment.PPE);
        var allowlists = GetAllowlists(DocsEnvironment.PPE);

        return OpsMetadataRuleConverter.GenerateJsonSchema(await metadataRules, await allowlists, _errors);
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
            middleware: BuildMiddleware(),
            httpClient: _longHttp,
            retry: 0);
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

            if (response.Headers.TryGetValues("X-Metadata-Version", out var metadataVersion))
            {
                var documentUrl = response.Headers.TryGetValues("X-Ruleset-DocumentURL", out var url) ? string.Join(",", url) : "";
                var rulesetList = string.Join(',', metadataVersion);
                if (request.RequestUri!.AbsolutePath.Contains("/metadatarules"))
                {
                    _errors.Add(Errors.System.MetadataValidationRuleset(rulesetList, documentUrl));
                    Log.Write($"Metadata validation ruleset used: {rulesetList}. Document url: {documentUrl}");
                }
                else if (request.RequestUri!.AbsolutePath.Contains("/contentrules"))
                {
                    Log.Write($"Content validation ruleset used: {rulesetList}. Document url: {documentUrl}");
                }
                else
                {
                    Log.Write($"Build validation ruleset used: {rulesetList}. Document url: {documentUrl}");
                }
            }

            return response;
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

    private async Task<string> Fetch(
        Func<HttpRequestMessage> requestFactory,
        string? value404 = null,
        HttpMiddleware? middleware = null,
        HttpClient? httpClient = null,
        int retry = 3)
    {
        string? requestUrl = null;
        using var response = await HttpPolicyExtensions
           .HandleTransientHttpError()
           .Or<OperationCanceledException>()
           .Or<IOException>()
           .RetryAsync(retry)
           .ExecuteAsync(() => _credentialHandler.SendRequest(
               requestFactory,
               request => middleware != null ? middleware(request, SendRequest) : SendRequest(request)));

        if (value404 != null && response.StatusCode == HttpStatusCode.NotFound)
        {
            return value404;
        }

        try
        {
            return await response.EnsureSuccessStatusCode().Content.ReadAsStringAsync();
        }
        catch (Exception ex)
        {
            ex.Data["RequestUrl"] = requestUrl;
            throw;
        }

        async Task<HttpResponseMessage> SendRequest(HttpRequestMessage request)
        {
            using (PerfScope.Start($"[{nameof(OpsAccessor)}] '{request.Method} {UrlUtility.SanitizeUrl(request.RequestUri?.ToString())}'"))
            {
                request.Headers.TryAddWithoutValidation("User-Agent", "docfx");
                requestUrl = request.RequestUri?.ToString();
                return await (httpClient ?? _http).SendAsync(request);
            }
        }
    }

    private static HttpMiddleware BuildMiddleware(DocsEnvironment? environment = null, HttpMiddleware? middleware = null)
    {
        return async (request, next) =>
        {
            if (s_fallbackToPublicData)
            {
                request.Headers.TryAddWithoutValidation("X-OP-FallbackToPublicData", "True");
            }

            if (!request.Headers.Contains("X-OP-BuildUserToken"))
            {
                try
                {
                    environment ??= DocsEnvironment;
                    var accessToken = environment switch
                    {
                        DocsEnvironment.Prod => s_accessTokenPublic,
                        DocsEnvironment.PPE => s_accessTokenPubDev,
                        DocsEnvironment.Perf => s_accessTokenPerf,
                        _ => throw new InvalidOperationException(),
                    };
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", (await accessToken.Value).Token);
                }
                catch (Exception ex)
                {
                    Log.Write($"Failed to get AAD access token<{environment}>");
                    if (s_fallbackToPublicData)
                    {
                        Log.Write(ex);
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            return await (middleware != null ? middleware(request, next) : next(request));
        };
    }

    private static ValueTask<AccessToken> GetAccessTokenAsync(DocsEnvironment? environment = null)
    {
        var defaultAzureCredential = new DefaultAzureCredential();
        return defaultAzureCredential.GetTokenAsync(
            new TokenRequestContext(new[] { $"{DocsBuildApiAADClientId(environment)}/.default" }));
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

    private static string DocsBuildApiAADClientId(DocsEnvironment? environment = null)
    {
        return (environment ?? DocsEnvironment) switch
        {
            DocsEnvironment.Prod => "6befca88-4c28-430a-957e-f870b267bcfc",
            DocsEnvironment.PPE => "6ce33073-a071-4cf9-9936-f5a24d21a089",
            DocsEnvironment.Perf => "ec05099b-1462-406c-8883-0ea74a90e82b",
            _ => throw new InvalidOperationException(),
        };
    }

    private static string TaxonomyApi(DocsEnvironment? environment = null)
    {
        return (environment ?? DocsEnvironment) switch
        {
            DocsEnvironment.Prod => "https://taxonomy.docs.microsoft.com",
            _ => "https://taxonomy.ppe.docs.microsoft.com",
        };
    }

    private static DocsEnvironment GetDocsEnvironment()
    {
        return Enum.TryParse(Environment.GetEnvironmentVariable("DOCS_ENVIRONMENT"), true, out DocsEnvironment docsEnvironment)
            ? docsEnvironment
            : DocsEnvironment.Prod;
    }
}
