// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Xunit;
using Yunit;

namespace Microsoft.Docs.Build
{
    public static class OpsConfigAdapterTest
    {
        [SkippableTheory]
        [InlineData(
            "https://ops/buildconfig/?name=e2eppe-azure-documents&repository_url=https://github.com/OPS-E2E-PPE/azure-docs-pr&branch=master",
            "{'product':'MSDN','siteName':'Docs','hostName':'ppe.docs.microsoft.com','basePath':'/e2eppe-azure-documents','xrefHostName':'ppe.docs.microsoft.com'}")]
        [InlineData(
            "https://ops/buildconfig/?name=e2eppe-azure-documents&repository_url=https://github.com/OPS-E2E-PPE/azure-docs-pr&branch=live",
            "{'product':'MSDN','siteName':'Docs','hostName':'ppe.docs.microsoft.com','basePath':'/e2eppe-azure-documents','xrefHostName':'ppe.docs.microsoft.com'}")]
        [InlineData(
            "https://ops/buildconfig/?name=e2eppe-azure-documents&repository_url=https://github.com/OPS-E2E-PPE/azure-docs-pr.cs-cz&branch=live-sxs",
            "{'product':'MSDN','siteName':'Docs','hostName':'ppe.docs.microsoft.com','basePath':'/e2eppe-azure-documents','xrefHostName':'ppe.docs.microsoft.com'}")]
        [InlineData(
            "https://ops/buildconfig/?name=e2e-ppe-dotnetapidocs&repository_url=https://github.com/OPS-E2E-PPE/dotnet-api-docs/&branch=master",
            "{'product':'MSDN','siteName':'Docs','hostName':'ppe.docs.microsoft.com','basePath':'/e2eppe-core-docs','xrefHostName':'ppe.docs.microsoft.com'}")]
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

            new JsonDiffBuilder().UseAdditionalProperties().Build().Verify(
                JToken.Parse(expectedJson.Replace('\'', '"')),
                JToken.Parse(actualConfig));
        }
    }
}
