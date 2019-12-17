// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Xunit;
using Yunit;

namespace Microsoft.Docs.Build
{
    public static class OpsConfigAdapterTest
    {
        [Theory]
        [InlineData("", "", "", "null")]
        [InlineData(
            "azure-documents",
            "https://github.com/MicrosoftDocs/azure-docs-pr",
            "master",
            "{'product':'Azure','siteName':'Docs','hostName':'docs.microsoft.com','basePath':'/azure','xrefHostName':'review.docs.microsoft.com','localization':{'defaultLocale':'en-us'}}")]
        [InlineData(
            "azure-documents",
            "https://github.com/MicrosoftDocs/azure-docs-pr",
            "live",
            "{'product':'Azure','siteName':'Docs','hostName':'docs.microsoft.com','basePath':'/azure','xrefHostName':'docs.microsoft.com','localization':{'defaultLocale':'en-us'}}")]
        [InlineData(
            "azure-documents",
            "https://github.com/MicrosoftDocs/azure-docs-pr.zh-cn",
            "live-sxs",
            "{'product':'Azure','siteName':'Docs','hostName':'docs.microsoft.com','basePath':'/azure','xrefHostName':'docs.microsoft.com','localization':{'defaultLocale':'en-us'}}")]
        [InlineData(
            "mooncake-docs",
            "https://github.com/MicrosoftDocs/mc-docs-pr",
            "master",
            "{'product':'Azure','siteName':'DocsAzureCN','hostName':'docs.azure.cn','basePath':'/','xrefHostName':'review.docs.azure.cn','localization':{'defaultLocale':'zh-cn'}}")]
        public static async Task AdaptOpsServiceConfig(string name, string repository, string branch, string expectedJson)
        {
            var token = Environment.GetEnvironmentVariable("DOCS_OPS_TOKEN");
            if (string.IsNullOrEmpty(token))
            {
                return;
            }

            var actualConfig = await new OpsConfigAdapter(null).GetBuildConfig(Array.Empty<string>(), new SourceInfo<string>(name), repository, branch);

            new JsonDiffBuilder().UseAdditionalProperties().Build().Verify(JToken.Parse(expectedJson.Replace('\'', '"')), actualConfig);
        }
    }
}
