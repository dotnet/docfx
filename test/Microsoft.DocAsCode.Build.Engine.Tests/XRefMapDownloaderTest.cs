// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine.Tests
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    using Xunit;

    using Microsoft.DocAsCode.Build.Engine;

    [Trait("Owner", "makaretu")]
    public class XRefMapDownloadTest
    {
        [Fact]
        public async Task BaseUrlIsSet()
        {
            var downloader = new XRefMapDownloader();
            var xrefs = await downloader.DownloadAsync(new Uri("https://dotnet.github.io/docfx/xrefmap.yml")) as XRefMap;
            Assert.NotNull(xrefs);
            Assert.Equal("https://dotnet.github.io/docfx/", xrefs.BaseUrl);
            var reader = xrefs.GetReader();
            Assert.Equal("https://dotnet.github.io/docfx/api/Microsoft.DocAsCode.AssemblyLicenseAttribute.html", reader.Find("Microsoft.DocAsCode.AssemblyLicenseAttribute").Href);
        }
    }
}
