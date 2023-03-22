// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System.Net;

namespace Microsoft.DocAsCode.Build.Engine.Tests;

public class XRefMapDownloadTest
{
    [Fact(Skip = "Flakly SSL connection problems on GH windows CI")]
    public async Task BaseUrlIsSet()
    {
        // GitHub doesn't support TLS 1.1 since Feb 23, 2018. See: https://github.com/blog/2507-weak-cryptographic-standards-removed
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

        var downloader = new XRefMapDownloader();
        var xrefs = await downloader.DownloadAsync(new Uri("https://dotnet.github.io/docfx/xrefmap.yml")) as XRefMap;
        Assert.NotNull(xrefs);
        Assert.Equal("https://dotnet.github.io/docfx/", xrefs.BaseUrl);
    }

    [Fact]
    public async Task ReadLocalXRefMapWithFallback()
    {
        // GitHub doesn't support TLS 1.1 since Feb 23, 2018. See: https://github.com/blog/2507-weak-cryptographic-standards-removed
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

        var basePath = Path.GetRandomFileName();
        var fallbackFolders = new List<string>() { Path.Combine(Directory.GetCurrentDirectory(), "TestData") };
        var xrefmaps = new List<string>() { "xrefmap.yml" };
        
        // Get fallback TestData/xrefmap.yml which contains uid: 'str'
        var reader = await new XRefCollection(from u in xrefmaps
                                        select new Uri(u, UriKind.RelativeOrAbsolute)).GetReaderAsync(basePath, fallbackFolders);

        var xrefSpec = reader.Find("str");
        Assert.NotNull(xrefSpec);
        Assert.Equal("https://docs.python.org/3.5/library/stdtypes.html#str", xrefSpec.Href);
    }
}
