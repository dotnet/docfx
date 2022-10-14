// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Xunit;

namespace Microsoft.Docs.Build;

public static class NewTest
{
    public static TheoryData<string> TemplateNames { get; } = new TheoryData<string>();

    static NewTest()
    {
        TestQuirks.Initializable = true;
        var basePath = Path.Combine(AppContext.BaseDirectory, "data", "new");
        foreach (var path in Directory.GetDirectories(basePath))
        {
            TemplateNames.Add(Path.GetRelativePath(basePath, path));
        }
    }

    [Theory]
    [MemberData(nameof(TemplateNames))]
    public static void Create_Build_Docset(string templateName)
    {
        var path = Path.Combine("new-test", Guid.NewGuid().ToString("N"));

        Assert.Equal(0, Docfx.Run(new[] { "new", templateName, "--git-init", "-o", path }));
        Assert.Equal(0, Docfx.Run(new[] { "build", path }));
    }

    [Theory]
    [MemberData(nameof(TemplateNames))]
    public static void Create_Build_Serve_Docset(string templateName)
    {
        var path = Path.Combine("new-test", Guid.NewGuid().ToString("N"));

        Assert.Equal(0, Docfx.Run(new[] { "new", templateName, "--git-init", "-o", path }));
        Assert.Equal(0, Docfx.Run(new[] { "build", path }));

        var exception = new Exception();
        Assert.Equal(exception, Assert.Throws<Exception>(() =>
            Serve.Run(new() { Directory = Path.GetFullPath(path), Address = "127.0.0.1" }, null, OnUrl)));

        void OnUrl(string url)
        {
            using var http = new HttpClient();
            var response = http.GetAsync(url).GetAwaiter().GetResult();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            throw exception;
        }
    }

    [Fact]
    public static void Create_Build_Invalid_Docset()
    {
        var path = Path.Combine("new-test", Guid.NewGuid().ToString("N"));

        Assert.Equal(1, Docfx.Run(new[] { "new", "C:/Users", "-o", path }));
    }
}
