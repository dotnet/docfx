// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Docfx.Build.Engine;
using Docfx.Build.OperationLevelRestApi;
using Docfx.Build.TagLevelRestApi;
using Docfx.Common;
using Docfx.DataContracts.Common;
using Docfx.DataContracts.RestApi;
using Docfx.Plugins;
using Docfx.Tests.Common;
using Newtonsoft.Json.Linq;

namespace Docfx.Build.RestApi.WithPlugins.Tests;

[DoNotParallelize]
[TestClass]
public class SplitRestApiToOperationLevelTest : TestBase
{
    private readonly string _outputFolder;
    private readonly FileCollection _defaultFiles;
    private readonly ApplyTemplateSettings _applyTemplateSettings;
    private readonly TemplateManager _templateManager;

    private const string RawModelFileExtension = ".raw.json";

    public SplitRestApiToOperationLevelTest()
    {
        string inputFolder = GetRandomFolder();
        _outputFolder = GetRandomFolder();
        _defaultFiles = new FileCollection(Directory.GetCurrentDirectory());
        _defaultFiles.Add(DocumentType.Article, new[] { "TestData/swagger/petstore.json" }, "TestData/");
        _applyTemplateSettings = new ApplyTemplateSettings(inputFolder, _outputFolder)
        {
            RawModelExportSettings = { Export = true },
            TransformDocument = true,
        };
        _templateManager = new TemplateManager(["template"], null, "TestData/");
    }

    [TestMethod]
    public void SplitRestApiToOperationLevelShouldSucceed()
    {
        var files = new FileCollection(_defaultFiles);
        BuildDocument(files);

        {
            // Verify original petstore page
            var outputRawModelPath = GetRawModelFilePath("petstore.json");
            Assert.IsTrue(File.Exists(outputRawModelPath));
            var model = JsonUtility.Deserialize<RestApiRootItemViewModel>(outputRawModelPath);
            Assert.IsNotNull(model);
            Assert.AreEqual("petstore.swagger.io/v2/Swagger Petstore/1.0.0", model.Uid);
            Assert.IsEmpty(model.Children);
            Assert.IsTrue((bool)model.Metadata["_isSplittedByOperation"]);
            Assert.IsEmpty(model.Tags);
            Assert.AreEqual("<p sourcefile=\"TestData/swagger/petstore.json\" sourcestartlinenumber=\"1\">Find out more about Swagger</p>\n", ((JObject)model.Metadata["externalDocs"])["description"]);
        }
        {
            // Verify splitted operation page
            var outputRawModelPath = GetRawModelFilePath("petstore/addPet.json");
            Assert.IsTrue(File.Exists(outputRawModelPath));
            var model = JsonUtility.Deserialize<RestApiRootItemViewModel>(outputRawModelPath);
            Assert.IsNotNull(model);
            Assert.AreEqual("petstore.swagger.io/v2/Swagger Petstore/1.0.0/addPet", model.Uid);
            Assert.IsNull(model.HtmlId);
            Assert.AreEqual("addPet", model.Name);
            Assert.AreEqual("<p sourcefile=\"TestData/swagger/petstore.json\" sourcestartlinenumber=\"1\">Add a new pet to the store</p>\n", model.Summary);
            Assert.IsEmpty(model.Tags);
            Assert.AreEqual("swagger/petstore/addPet.html", model.Metadata["_path"]);
            Assert.AreEqual("TestData/swagger/petstore/addPet.json", model.Metadata["_key"]);
            Assert.IsTrue(model.Metadata.ContainsKey("externalDocs"));
            Assert.IsTrue((bool)model.Metadata["_isSplittedToOperation"]);
            Assert.ContainsSingle(model.Children);
            Assert.IsEmpty(model.Tags);

            // Test overwritten metadata
            Assert.AreEqual("<p sourcefile=\"TestData/swagger/petstore.json\" sourcestartlinenumber=\"1\">Find out more about addPet</p>\n", ((JObject)model.Metadata["externalDocs"])["description"]);

            var child = model.Children[0];
            Assert.AreEqual("petstore.swagger.io/v2/Swagger Petstore/1.0.0/addPet/operation", child.Uid);
            Assert.IsNull(child.HtmlId);
            Assert.IsNull(child.Summary); // Summary is popped to operation page
            Assert.IsEmpty(child.Tags);
        }
    }

    [TestMethod]
    public void SplitRestApiToOperationLevelWithTocShouldSucceed()
    {
        var files = new FileCollection(_defaultFiles);
        files.Add(DocumentType.Article, new[] { "TestData/swagger/toc.yml" }, "TestData/");
        BuildDocument(files);

        {
            // Verify original petstore page
            var outputRawModelPath = GetRawModelFilePath("petstore.json");
            Assert.IsTrue(File.Exists(outputRawModelPath));
            var model = JsonUtility.Deserialize<RestApiRootItemViewModel>(outputRawModelPath);
            Assert.IsNotNull(model);
            Assert.AreEqual("petstore.swagger.io/v2/Swagger Petstore/1.0.0", model.Uid);
            Assert.IsEmpty(model.Children);
            Assert.IsEmpty(model.Tags);
        }
        {
            // Verify splitted operation page
            var outputRawModelPath = GetRawModelFilePath("petstore/addPet.json");
            Assert.IsTrue(File.Exists(outputRawModelPath));
            var model = JsonUtility.Deserialize<RestApiRootItemViewModel>(outputRawModelPath);
            Assert.IsNotNull(model);
            Assert.AreEqual("petstore.swagger.io/v2/Swagger Petstore/1.0.0/addPet", model.Uid);
            Assert.IsNull(model.HtmlId);
            Assert.AreEqual("addPet", model.Name);
            Assert.AreEqual("<p sourcefile=\"TestData/swagger/petstore.json\" sourcestartlinenumber=\"1\">Add a new pet to the store</p>\n", model.Summary);
            Assert.IsEmpty(model.Tags);
            Assert.AreEqual("swagger/petstore/addPet.html", model.Metadata["_path"]);
            Assert.AreEqual("TestData/swagger/petstore/addPet.json", model.Metadata["_key"]);
            Assert.AreEqual("../toc.yml", model.Metadata["_tocRel"]);
            Assert.IsTrue(model.Metadata.ContainsKey("externalDocs"));
            Assert.ContainsSingle(model.Children);
            Assert.IsEmpty(model.Tags);

            var child = model.Children[0];
            Assert.AreEqual("petstore.swagger.io/v2/Swagger Petstore/1.0.0/addPet/operation", child.Uid);
            Assert.IsNull(child.HtmlId);
            Assert.IsNull(child.Summary); // Summary has been popped to operation page
            Assert.IsEmpty(child.Tags);
        }
        {
            // Verify toc page
            var outputRawModelPath = GetRawModelFilePath("toc.yml");
            Assert.IsTrue(File.Exists(outputRawModelPath));
            var model = JsonUtility.Deserialize<TocItemViewModel>(outputRawModelPath);
            Assert.IsNotNull(model);
            Assert.ContainsSingle(model.Items);
            var rootModel = model.Items[0];
            Assert.AreEqual("petstore.html", rootModel.TopicHref);
            Assert.AreEqual(20, rootModel.Items.Count);
            Assert.AreEqual("petstore.swagger.io/v2/Swagger Petstore/1.0.0/addPet", rootModel.Items[0].TopicUid);
            Assert.AreEqual("petstore/addPet.html", rootModel.Items[0].TopicHref);
            Assert.AreEqual("petstore/addPet.html", rootModel.Items[0].Href);
            Assert.AreEqual("addPet", rootModel.Items[0].Name);
        }
    }

    [TestMethod]
    public void SplitRestApiToTagAndOperationLevelWithTocShouldSucceed()
    {
        var files = new FileCollection(_defaultFiles);
        files.Add(DocumentType.Article, new[] { "TestData/swagger/toc.yml" }, "TestData/");
        BuildDocument(files, true);

        {
            // Verify original petstore page
            var outputRawModelPath = GetRawModelFilePath("petstore.json");
            Assert.IsTrue(File.Exists(outputRawModelPath));
            var model = JsonUtility.Deserialize<RestApiRootItemViewModel>(outputRawModelPath);
            Assert.IsNotNull(model);
            Assert.AreEqual("petstore.swagger.io/v2/Swagger Petstore/1.0.0", model.Uid);
            Assert.IsEmpty(model.Children);
            Assert.IsEmpty(model.Tags);
            Assert.IsTrue((bool)model.Metadata["_isSplittedByTag"]);
        }
        {
            // Verify splitted tag page
            var outputRawModelPath = GetRawModelFilePath("petstore/pet.json");
            Assert.IsTrue(File.Exists(outputRawModelPath));
            var model = JsonUtility.Deserialize<RestApiRootItemViewModel>(outputRawModelPath);
            Assert.IsNotNull(model);
            Assert.AreEqual("petstore.swagger.io/v2/Swagger Petstore/1.0.0/tag/pet", model.Uid);
            Assert.AreEqual("pet", model.Name);
            Assert.AreEqual("<p sourcefile=\"TestData/swagger/petstore.json\" sourcestartlinenumber=\"1\">Everything about your Pets</p>\n", model.Description);
            Assert.IsEmpty(model.Children);
            Assert.IsEmpty(model.Tags);
            Assert.AreEqual("swagger/petstore/pet.html", model.Metadata["_path"]);
            Assert.AreEqual("TestData/swagger/petstore/pet.json", model.Metadata["_key"]);
            Assert.IsTrue(model.Metadata.ContainsKey("externalDocs"));
            Assert.IsTrue((bool)model.Metadata["_isSplittedToTag"]);
            Assert.IsTrue((bool)model.Metadata["_isSplittedByOperation"]);
        }
        {
            // Verify splitted operation page
            var outputRawModelPath = GetRawModelFilePath("petstore/pet/addPet.json");
            Assert.IsTrue(File.Exists(outputRawModelPath));
            var model = JsonUtility.Deserialize<RestApiRootItemViewModel>(outputRawModelPath);
            Assert.IsNotNull(model);
            Assert.AreEqual("petstore.swagger.io/v2/Swagger Petstore/1.0.0/addPet", model.Uid);
            Assert.IsNull(model.HtmlId);
            Assert.AreEqual("addPet", model.Name);
            Assert.AreEqual("<p sourcefile=\"TestData/swagger/petstore.json\" sourcestartlinenumber=\"1\">Add a new pet to the store</p>\n", model.Summary);
            Assert.IsEmpty(model.Tags);
            Assert.AreEqual("swagger/petstore/pet/addPet.html", model.Metadata["_path"]);
            Assert.AreEqual("TestData/swagger/petstore/pet/addPet.json", model.Metadata["_key"]);
            Assert.AreEqual("../../toc.yml", model.Metadata["_tocRel"]);
            Assert.IsTrue(model.Metadata.ContainsKey("externalDocs"));
            Assert.ContainsSingle(model.Children);
            Assert.IsTrue((bool)model.Metadata["_isSplittedToOperation"]);

            var child = model.Children[0];
            Assert.AreEqual("petstore.swagger.io/v2/Swagger Petstore/1.0.0/addPet/operation", child.Uid);
            Assert.IsNull(child.HtmlId);
            Assert.IsNull(child.Summary); // Summary has been popped to operation page
            Assert.IsEmpty(child.Tags);
        }
        {
            // Verify toc page
            var outputRawModelPath = GetRawModelFilePath("toc.yml");
            Assert.IsTrue(File.Exists(outputRawModelPath));
            var model = JsonUtility.Deserialize<TocItemViewModel>(outputRawModelPath);
            Assert.IsNotNull(model);
            Assert.ContainsSingle(model.Items);
            var rootModel = model.Items[0];
            Assert.AreEqual("petstore.html", rootModel.TopicHref);
            Assert.AreEqual(3, rootModel.Items.Count);
            var firstTagToc = rootModel.Items[0];
            Assert.AreEqual("petstore.swagger.io/v2/Swagger Petstore/1.0.0/tag/pet", firstTagToc.TopicUid);
            Assert.AreEqual("petstore/pet.html", firstTagToc.TopicHref);
            Assert.AreEqual("petstore/pet.html", firstTagToc.Href);
            Assert.AreEqual("pet", firstTagToc.Name);
            Assert.AreEqual(8, firstTagToc.Items.Count);
            var firstOperationToc = firstTagToc.Items[0];
            Assert.AreEqual("petstore.swagger.io/v2/Swagger Petstore/1.0.0/addPet", firstOperationToc.TopicUid);
            Assert.AreEqual("petstore/pet/addPet.html", firstOperationToc.TopicHref);
            Assert.AreEqual("petstore/pet/addPet.html", firstOperationToc.Href);
            Assert.AreEqual("addPet", firstOperationToc.Name);
            Assert.IsNull(firstOperationToc.Items);
        }
    }

    private void BuildDocument(FileCollection files, bool enableTagLevel = false)
    {
        var parameters = new DocumentBuildParameters
        {
            Files = files,
            OutputBaseDir = _outputFolder,
            ApplyTemplateSettings = _applyTemplateSettings,
            Metadata = new Dictionary<string, object>
            {
                ["meta"] = "Hello world!",
            }.ToImmutableDictionary(),
            TemplateManager = _templateManager
        };

        using var builder = new DocumentBuilder(LoadAssemblies(enableTagLevel), []);
        builder.Build(parameters);
    }

    private static IEnumerable<System.Reflection.Assembly> LoadAssemblies(bool enableTagLevel)
    {
        yield return typeof(RestApiDocumentProcessor).Assembly;
        yield return typeof(SplitRestApiToOperationLevel).Assembly;
        if (enableTagLevel)
        {
            yield return typeof(SplitRestApiToTagLevel).Assembly;
        }
    }

    private string GetRawModelFilePath(string fileName)
    {
        return Path.GetFullPath(Path.Combine(_outputFolder, "swagger", Path.ChangeExtension(fileName, RawModelFileExtension)));
    }
}
