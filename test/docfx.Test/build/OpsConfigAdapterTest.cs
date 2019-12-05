// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Xunit;

namespace Microsoft.Docs.Build
{
    public static class OpsConfigAdapterTest
    {
        [Theory]
        [InlineData("", "", "", null)]
        [InlineData(
            "azure-documents",
            "https://github.com/MicrosoftDocs/azure-docs-pr",
            "master",
            "{'product':'Azure','siteName':'Docs','baseUrl':'https://docs.microsoft.com/azure','xrefBaseUrl':'https://review.docs.microsoft.com','localization':{'defaultLocale':'en-us'}}")]
        [InlineData(
            "azure-documents",
            "https://github.com/MicrosoftDocs/azure-docs-pr",
            "live",
            "{'product':'Azure','siteName':'Docs','baseUrl':'https://docs.microsoft.com/azure','xrefBaseUrl':'https://docs.microsoft.com','localization':{'defaultLocale':'en-us'}}")]
        [InlineData(
            "azure-documents",
            "https://github.com/MicrosoftDocs/azure-docs-pr.zh-cn",
            "live-sxs",
            "{'product':'Azure','siteName':'Docs','baseUrl':'https://docs.microsoft.com/azure','xrefBaseUrl':'https://docs.microsoft.com','localization':{'defaultLocale':'en-us'}}")]
        [InlineData(
            "mooncake-docs",
            "https://github.com/MicrosoftDocs/mc-docs-pr",
            "master",
            "{'product':'Azure','siteName':'DocsAzureCN','baseUrl':'https://docs.azure.cn/','xrefBaseUrl':'https://review.docs.azure.cn','localization':{'defaultLocale':'zh-cn'}}")]
        public static void AdaptOpsServiceConfig(string name, string repository, string branch, string expectedJson)
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DOCS_OPS_TOKEN")))
            {
                return;
            }

            var actualJson = OpsConfigAdapter
                .Load(new SourceInfo<string>(name), repository, branch)
                ?.ToString(Newtonsoft.Json.Formatting.None)
                ?.Replace('"', '\'');

            Assert.Equal(expectedJson, actualJson);
        }
    }
}
