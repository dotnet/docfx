// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json.Linq;
using Xunit;
using Yunit;

namespace Microsoft.Docs.Build;

public static class OpsConfigAdapterTest
{
    public static TheoryData<string, string> TestData => new()
    {
        {
            "https://ops/buildconfig/?name=e2eppe-azure-documents&repository_url=https://github.com/OPS-E2E-PPE/azure-docs-pr&branch=master",
            "{'product':'MSDN','siteName':'Docs','hostName':'dev.learn.microsoft.com','alternativeHostName':'ppe.docs.microsoft.com','basePath':'/e2eppe-azure-documents','xrefHostName':'dev.learn.microsoft.com'}"
        },
        {
            "https://ops/buildconfig/?name=e2eppe-azure-documents&repository_url=https://github.com/OPS-E2E-PPE/azure-docs-pr&branch=live",
            "{'product':'MSDN','siteName':'Docs','hostName':'dev.learn.microsoft.com','alternativeHostName':'ppe.docs.microsoft.com','basePath':'/e2eppe-azure-documents','xrefHostName':'dev.learn.microsoft.com'}"
        },
        {
            "https://ops/buildconfig/?name=E2E_DocFxV3&repository_url=https://github.com/OPS-E2E-PPE/E2E_DocFxV3/&branch=master",
            "{'product':'MSDN','siteName':'Docs','hostName':'dev.learn.microsoft.com','alternativeHostName':'ppe.docs.microsoft.com','basePath':'/E2E_DocFxV3','xrefHostName':'dev.learn.microsoft.com'}"
        },
    };

    [SkippableTheory]
    [MemberData(nameof(TestData))]
    public static async Task AdaptOpsServiceConfig(string url, string expectedJson)
    {
        var token = Environment.GetEnvironmentVariable("DOCS_OPS_TOKEN");
        Skip.If(string.IsNullOrEmpty(token));

        var credentialHandler = new CredentialHandler((_, _, _) =>
        {
            return Task.FromResult<HttpConfig>(new() { Headers = new() { ["X-OP-BuildUserToken"] = token } });
        });
        var accessor = new OpsAccessor(null, credentialHandler);
        var adapter = new OpsConfigAdapter(accessor);
        using var request = new HttpRequestMessage { RequestUri = new Uri(url) };
        var response = await adapter.InterceptHttpRequest(request);
        var actualConfig = await response.EnsureSuccessStatusCode().Content.ReadAsStringAsync();

        var actualConfigToken = JToken.Parse(actualConfig);
        new JsonDiffBuilder().UseAdditionalProperties().Build().Verify(
            JToken.Parse(expectedJson.Replace('\'', '"')),
            actualConfigToken);

        var allowedHtml = JsonUtility.ToObject<Config>(ErrorBuilder.Null, actualConfigToken).AllowedHtml;
        Assert.True(allowedHtml.ContainsKey("*"));
        Assert.True(allowedHtml["*"].Count > 5);
        Assert.True(allowedHtml.ContainsKey("a"));
    }

    [SkippableTheory]
    [MemberData(nameof(TestData))]
    public static async Task AdaptOpsServiceConfigWithAAD(string url, string expectedJson)
    {
        Skip.If(
            bool.TryParse(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"), out var isGithubAction)
            && isGithubAction);
        var accessor = new OpsAccessor(null, new CredentialHandler());
        var adapter = new OpsConfigAdapter(accessor);
        using var request = new HttpRequestMessage { RequestUri = new Uri(url) };
        var response = await adapter.InterceptHttpRequest(request);
        var actualConfig = await response.EnsureSuccessStatusCode().Content.ReadAsStringAsync();

        new JsonDiffBuilder().UseAdditionalProperties().Build().Verify(
            JToken.Parse(expectedJson.Replace('\'', '"')),
            JToken.Parse(actualConfig));
    }
}
