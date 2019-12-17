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
        [Theory]
        [InlineData(
            "https://ops/buildconfig/?name=azure-documents&repository_url=https://github.com/MicrosoftDocs/azure-docs-pr",
            "{'product':'Azure','siteName':'Docs','hostName':'docs.microsoft.com','basePath':'/azure','xrefHostName':'review.docs.microsoft.com','localization':{'defaultLocale':'en-us'}}")]
        [InlineData(
            "https://ops/buildconfig/?name=azure-documents&repository_url=https://github.com/MicrosoftDocs/azure-docs-pr&branch=live",
            "{'product':'Azure','siteName':'Docs','hostName':'docs.microsoft.com','basePath':'/azure','xrefHostName':'docs.microsoft.com','localization':{'defaultLocale':'en-us'}}")]
        [InlineData(
            "https://ops/buildconfig/?name=azure-documents&repository_url=https://github.com/MicrosoftDocs/azure-docs-pr.zh-cn&branch=live-sxs",
            "{'product':'Azure','siteName':'Docs','hostName':'docs.microsoft.com','basePath':'/azure','xrefHostName':'docs.microsoft.com','localization':{'defaultLocale':'en-us'}}")]
        [InlineData(
            "https://ops/buildconfig/?name=mooncake-docs&repository_url=https://github.com/MicrosoftDocs/mc-docs-pr",
            "{'product':'Azure','siteName':'DocsAzureCN','hostName':'docs.azure.cn','basePath':'/','xrefHostName':'review.docs.azure.cn','localization':{'defaultLocale':'zh-cn'}}")]
        public static async Task AdaptOpsServiceConfig(string url, string expectedJson)
        {
            var token = Environment.GetEnvironmentVariable("DOCS_OPS_TOKEN");
            if (string.IsNullOrEmpty(token))
            {
                return;
            }

            var response = await new OpsConfigAdapter(null).InterceptHttpRequest(new HttpRequestMessage { RequestUri = new Uri(url) });
            var actualConfig = await response.EnsureSuccessStatusCode().Content.ReadAsStringAsync();

            new JsonDiffBuilder().UseAdditionalProperties().Build().Verify(
                JToken.Parse(expectedJson.Replace('\'', '"')),
                JToken.Parse(actualConfig));
        }
    }
}
