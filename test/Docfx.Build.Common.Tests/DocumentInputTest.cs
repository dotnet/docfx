// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Common;
using Docfx.Plugins;
using Docfx.Tests.Common;
using Xunit;

namespace Docfx.Build.Common.Tests;

[Collection("docfx STA")]
public class DocumentInputTest : TestBase
{
    [Theory]
    [InlineData("YamlMime:ManagedReference")]
    [InlineData("YamlMime:CustomProtocol")]
    public void YamlMimeTakesPrecedenceOverFieldsInTheBody(string mime)
    {
        var input = DocumentInput.FromText($"### {mime}\nopenapi: 3.2.0\ninvalid: [", "yaml");
        Assert.Equal(mime, input.Header.Kind);
        Assert.Null(input.Header.Version);
    }

    [Theory]
    [InlineData("json", "{\"openapi\":\"3.2.0\",\"invalid\":]")]
    [InlineData("yaml", "# API document\nopenapi: 3.2.0\ninvalid: [")]
    public void DetectsOpenApiBeforeParsingTheBody(string format, string source)
    {
        var input = DocumentInput.FromText(source, format);
        Assert.Equal("openapi", input.Header.Kind);
        Assert.Equal("3.2.0", input.Header.Version);
        using var reader = input.OpenRead();
        Assert.Equal(source, reader.ReadToEnd());
    }

    [Fact]
    public void InputsAreSharedWithinABuildAndRefreshedForTheNextBuild()
    {
        var folder = GetRandomFolder();
        var path = CreateFile("api.yaml", "openapi: 3.0.3", folder);
        var file = new FileAndType(Path.GetFullPath(folder), "api.yaml", DocumentType.Article);
        using (DocumentInput.BeginRead([file]))
        {
            var input = DocumentInput.Get(file);
            Assert.Equal("3.0.3", input.Header.Version);
            Assert.Equal("openapi: 3.0.3", input.ReadAllText());
            File.Delete(path);
            var shared = DocumentInput.Get(file);
            Assert.Equal("3.0.3", shared.Header.Version);
            using var reader = shared.OpenRead();
            Assert.Equal("openapi: 3.0.3", reader.ReadToEnd());
        }
        File.WriteAllText(path, "### YamlMime:CustomProtocol\nvalue: 42");
        using (DocumentInput.BeginRead([file]))
        {
            var input = DocumentInput.Get(file);
            Assert.Equal("YamlMime:CustomProtocol", input.Header.Kind);
            using var reader = input.OpenRead();
            Assert.Equal(42, YamlUtility.Deserialize<Dictionary<string, int>>(reader)["value"]);
        }
    }
}
