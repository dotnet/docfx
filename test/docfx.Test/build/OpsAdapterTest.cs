// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using Xunit;

namespace Microsoft.Docs.Build
{
    public static class OpsAdapterTest
    {
        [Theory]
        [InlineData(
            "azure-documents",
            "https://github.com/MicrosoftDocs/azure-docs-pr",
            "master",
            "{}")]
        public static void AdaptOpsServiceConfig(string name, string repository, string branch, string expectedJson)
        {
            var actualJson = new OpsConfigAdapter(new FileDownloader("."))
                .TryAdapt(name, repository, branch)
                ?.ToString(Newtonsoft.Json.Formatting.None)
                ?.Replace('"', '\'');

            Assert.Equal(expectedJson, actualJson);
        }
    }
}
