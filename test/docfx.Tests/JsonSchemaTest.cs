// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Docfx.Common;
using Docfx.DataContracts.Common;
using Docfx.Tests.Common;
using FluentAssertions;
using YamlDotNet.Serialization;

namespace Docfx.Tests;

[DoNotParallelize]
[TestClass]
public class JsonSchemaTest : TestBase
{
    [TestMethod]
    [DataRow("docs/docfx.json")]
    [DataRow("samples/csharp/docfx.json")]
    [DataRow("samples/extensions/docfx.json")]
    [DataRow("samples/seed/docfx.json")]
    [DataRow("test/docfx.Tests/Assets/docfx.json_build/docfx.json")]
    [DataRow("test/docfx.Tests/Assets/docfx.json_empty/docfx.json")]
    [DataRow("test/docfx.Tests/Assets/docfx.json_metadata/docfx.json")]
    [DataRow("test/docfx.Tests/Assets/docfx.json_metadata/docfxWithFilter.json")]
    [DataRow("test/docfx.Tests/Assets/docfx.json_metadata_build/docfx.json")]
    public void JsonSchemaTest_Docfx_Json(string path)
    {
        // Arrange
        var jsonElement = LoadAsJsonElement(path);

        // Act
        var result = JsonSchemaUtility.ValidateJsonSchema(jsonElement, Constants.JsonSchemas.Docfx);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [TestMethod]
    [DataRow("test/docfx.Tests/Assets/docfx.json_invalid_format/docfx.json")]
    [DataRow("test/docfx.Tests/Assets/docfx.json_invalid_key/docfx.json")]
    public void JsonSchemaTest_Docfx_Json_Invalid(string path)
    {
        // Arrange
        var jsonElement = LoadAsJsonElement(path);

        // Act
        var result = JsonSchemaUtility.ValidateJsonSchema(jsonElement, Constants.JsonSchemas.Docfx);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [TestMethod]
    [DataRow("src/Docfx.Dotnet/Resources/defaultfilterconfig.yml")]
    [DataRow("test/Docfx.Dotnet.Tests/TestData/filterconfig.yml")]
    [DataRow("test/Docfx.Dotnet.Tests/TestData/filterconfig_attribute.yml")]
    [DataRow("test/Docfx.Dotnet.Tests/TestData/filterconfig_docs_sample.yml")]
    public void JsonSchemaTest_FilterConfig(string path)
    {
        // Arrange
        var jsonElement = LoadAsJsonElement(path);

        // Act
        var result = JsonSchemaUtility.ValidateJsonSchema(jsonElement, Constants.JsonSchemas.FilterConfig);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [TestMethod]
    [DataRow("test/docfx.Snapshot.Tests/SamplesTest.CSharp/api/toc.json.view.verified.json")]
    [DataRow("test/docfx.Snapshot.Tests/SamplesTest.Extensions/api/toc.json.view.verified.json")]
    [DataRow("test/docfx.Snapshot.Tests/SamplesTest.Extensions/toc.json.view.verified.json")]
    [DataRow("test/docfx.Snapshot.Tests/SamplesTest.Seed/api/toc.json.view.verified.json")]
    [DataRow("test/docfx.Snapshot.Tests/SamplesTest.Seed/apipage/toc.json.view.verified.json")]
    [DataRow("test/docfx.Snapshot.Tests/SamplesTest.Seed/articles/toc.json.view.verified.json")]
    [DataRow("test/docfx.Snapshot.Tests/SamplesTest.Seed/md/toc.json.view.verified.json")]
    [DataRow("test/docfx.Snapshot.Tests/SamplesTest.Seed/pdf/toc.json.view.verified.json")]
    [DataRow("test/docfx.Snapshot.Tests/SamplesTest.Seed/restapi/toc.json.view.verified.json")]
    [DataRow("test/docfx.Snapshot.Tests/SamplesTest.Seed/toc.json.view.verified.json")]
    public void JsonSchemaTest_Toc_Json(string path)
    {
        // Arrange
        var jsonElement = LoadAsJsonElement(path);

        // Act
        var result = JsonSchemaUtility.ValidateJsonSchema(jsonElement, Constants.JsonSchemas.Toc);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [TestMethod]
    [DataRow("test/Docfx.Build.RestApi.WithPlugins.Tests/TestData/swagger/toc.yml")]
    [DataRow("test/docfx.Snapshot.Tests/SamplesTest.SeedMarkdown/toc.verified.yml")]
    public void JsonSchemaTest_Toc_Yaml(string path)
    {
        // Arrange
        var jsonElement = LoadAsJsonElement(path);

        // Act
        var result = JsonSchemaUtility.ValidateJsonSchema(jsonElement, Constants.JsonSchemas.Toc);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [TestMethod]
    [DataRow("test/Docfx.Build.Tests/TestData/xrefmap.json")]
    public void JsonSchemaTest_XrefMap_Json(string path)
    {
        // Arrange
        var jsonElement = LoadAsJsonElement(path);

        // Act
        var result = JsonSchemaUtility.ValidateJsonSchema(jsonElement, Constants.JsonSchemas.XrefMap);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [TestMethod]
    [DataRow("test/Docfx.Build.Tests/TestData/xrefmap.yml")]
    [DataRow("test/docfx.Snapshot.Tests/SamplesTest.CSharp/xrefmap.verified.yml")]
    [DataRow("test/docfx.Snapshot.Tests/SamplesTest.Extensions/xrefmap.verified.yml")]
    [DataRow("test/docfx.Snapshot.Tests/SamplesTest.Seed/xrefmap.verified.yml")]
    public void JsonSchemaTest_XrefMap_Yaml(string path)
    {
        // Arrange
        var jsonElement = LoadAsJsonElement(path);

        // Act
        var result = JsonSchemaUtility.ValidateJsonSchema(jsonElement, Constants.JsonSchemas.XrefMap);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    /// <summary>
    /// Load file content as JsonElement.
    /// </summary>
    private static JsonElement LoadAsJsonElement(string path)
    {
        var solutionDir = PathHelper.GetSolutionFolder();

        var filePath = Path.Combine(solutionDir, path);

        if (!File.Exists(filePath))
            throw new FileNotFoundException(filePath);

        switch (Path.GetExtension(filePath))
        {
            case ".json":
                var doc = JsonDocument.Parse(File.OpenRead(filePath), JsonSchemaUtility.DefaultJsonDocumentOptions);
                return doc.RootElement;
            case ".yml":
                var yamlObject = YamlUtility.Deserialize<object>(filePath);

                var serializer = new SerializerBuilder()
                                   .JsonCompatible()
                                   .Build();
                var json = serializer.Serialize(yamlObject);
                return JsonSerializer.Deserialize<JsonElement>(json);

            default:
                throw new NotSupportedException(path);
        }
    }
}
