// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

using Docfx.Build.Engine;
using Docfx.Common;
using Docfx.Plugins;
using Docfx.Tests.Common;

namespace Docfx.Build.UniversalReference.Tests;

[TestClass]
public class UniversalReferenceDocumentProcessorTest : TestBase
{
    private readonly string _outputFolder;
    private readonly ApplyTemplateSettings _applyTemplateSettings;
    private readonly TemplateManager _templateManager;

    private const string RawModelFileExtension = ".raw.json";
    private const string TestDataDirectory = "TestData";
    private const string YmlDirectoryName = "yml";
    private const string OverwriteDirectoryName = "overwrite";
    private static readonly string YmlDataDirectory = Path.Combine(TestDataDirectory, YmlDirectoryName);
    private static readonly string OverwriteDataDirectory = Path.Combine(TestDataDirectory, OverwriteDirectoryName);

    public UniversalReferenceDocumentProcessorTest()
    {
        _outputFolder = GetRandomFolder();
        string inputFolder = GetRandomFolder();
        _applyTemplateSettings = new ApplyTemplateSettings(inputFolder, _outputFolder)
        {
            RawModelExportSettings = { Export = true },
            TransformDocument = true,
        };
        _templateManager = new TemplateManager(["template"], null, "TestData/");
    }

    #region Python

    [TestMethod]
    public void ProcessPythonReferencesShouldSucceed()
    {
        var fileNames = new string[] { "cntk.core.yml", "cntk.core.Value.yml", "cntk.debugging.yml" };
        var files = new FileCollection(Directory.GetCurrentDirectory());
        files.Add(DocumentType.Article, fileNames.Select(f => $"{YmlDataDirectory}/{f}"), TestDataDirectory);

        BuildDocument(files);

        foreach (var fileName in fileNames)
        {
            var outputRawModelPath = GetRawModelFilePath(fileName);
            Assert.IsTrue(File.Exists(outputRawModelPath));
        }
    }

    [TestMethod]
    public void ProcessPythonModelShouldSucceed()
    {
        var moduleFileName = "cntk.core.yml";
        var classFileName = "cntk.core.Value.yml";
        var files = new FileCollection(Directory.GetCurrentDirectory());
        files.Add(
            DocumentType.Article,
            new[] { $"{YmlDataDirectory}/{moduleFileName}", $"{YmlDataDirectory}/{classFileName}" },
            TestDataDirectory);

        BuildDocument(files);

        var outputModuleRawModelPath = GetRawModelFilePath(moduleFileName);
        var outputClassRawModelPath = GetRawModelFilePath(classFileName);
        Assert.IsTrue(File.Exists(outputClassRawModelPath));

        var moduleModel = JsonUtility.Deserialize<ApiBuildOutput>(outputModuleRawModelPath);
        Assert.IsNotNull(moduleModel);
        Assert.AreEqual("Test UniversalReferenceDocumentProcessor", moduleModel.Metadata["meta"]);
        Assert.AreEqual(
            "<p sourcefile=\"TestData/yml/cntk.core.Value.yml\" sourcestartlinenumber=\"1\">Bases: <xref href=\"cntk.cntk_py.Value\" data-throw-if-not-resolved=\"False\" data-raw-source=\"@cntk.cntk_py.Value\" sourcefile=\"TestData/yml/cntk.core.Value.yml\" sourcestartlinenumber=\"1\"></xref>\nInternal representation of minibatch data.</p>\n",
            moduleModel.Children[0].Value[1].Summary);
        Assert.AreEqual("Class", moduleModel.Children[0].Value[1].Type);

        var classModel = JsonUtility.Deserialize<ApiBuildOutput>(outputClassRawModelPath);
        Assert.IsNotNull(classModel);
        Assert.AreEqual("Test UniversalReferenceDocumentProcessor", classModel.Metadata["meta"]);

        Assert.ContainsSingle(classModel.SupportedLanguages);
        Assert.AreEqual("python", classModel.SupportedLanguages[0]);

        Assert.AreEqual("Class", classModel.Type);

        Assert.AreEqual("Value", classModel.Name[0].Value);
        Assert.AreEqual("cntk.core.Value", classModel.FullName[0].Value);

        Assert.AreEqual("https://github.com/Microsoft/CNTK", classModel.Source[0].Value.Remote.Repo);
        Assert.AreEqual("cntk/core.py", classModel.Source[0].Value.Remote.Path);
        Assert.AreEqual(182, classModel.Source[0].Value.StartLine);

        Assert.AreEqual(6, classModel.Syntax.Parameters.Count);
        Assert.AreEqual("shape", classModel.Syntax.Parameters[0].Name);
        Assert.AreEqual("tuple", classModel.Syntax.Parameters[0].Type[0].Uid);
        Assert.AreEqual("<p sourcefile=\"TestData/yml/cntk.core.Value.yml\" sourcestartlinenumber=\"1\">shape of the value</p>\n",
            classModel.Syntax.Parameters[0].Description);

        Assert.AreEqual("cntk.cntk_py.Value", classModel.Inheritance[0].Value[0].Type.Uid);
        Assert.AreEqual("builtins.object", classModel.Inheritance[0].Value[0].Inheritance[0].Type.Uid);

        Assert.ContainsSingle(classModel.Children);
        Assert.AreEqual("python", classModel.Children[0].Language);
        Assert.AreEqual(5, classModel.Children[0].Value.Count);

        var firstChildrenValue = classModel.Children[0].Value[0];
        Assert.AreEqual("Method", firstChildrenValue.Type);
        Assert.AreEqual("cntk.core.Value.create", firstChildrenValue.Uid);
        Assert.AreEqual("create", firstChildrenValue.Name[0].Value);
        Assert.AreEqual("cntk.core.Value.create", firstChildrenValue.FullName[0].Value);
        Assert.AreEqual("<p sourcefile=\"TestData/yml/cntk.core.Value.yml\" sourcestartlinenumber=\"1\">Creates a <xref href=\"cntk.core.Value\" data-throw-if-not-resolved=\"False\" data-raw-source=\"@cntk.core.Value\" sourcefile=\"TestData/yml/cntk.core.Value.yml\" sourcestartlinenumber=\"1\"></xref> object.</p>\n",
            firstChildrenValue.Summary);
        Assert.AreEqual("<p sourcefile=\"TestData/yml/cntk.core.Value.yml\" sourcestartlinenumber=\"1\"><xref href=\"cntk.core.Value\" data-throw-if-not-resolved=\"False\" data-raw-source=\"@cntk.core.Value\" sourcefile=\"TestData/yml/cntk.core.Value.yml\" sourcestartlinenumber=\"1\"></xref> object.</p>\n",
            firstChildrenValue.Syntax.Return[0].Value.Description);
        Assert.AreEqual("type1", firstChildrenValue.Syntax.Return[0].Value.Type[0].Uid);
        Assert.AreEqual("type2", firstChildrenValue.Syntax.Return[0].Value.Type[1].Uid);
        Assert.AreEqual("type3", firstChildrenValue.Syntax.Return[0].Value.Type[2].Uid);
    }

    [TestMethod]
    public void ApplyOverwriteDocumentForPythonShouldSucceed()
    {
        var fileName = "cntk.core.Value.yml";
        var overwriteFileName = "cntk.core.Value.md";
        var files = new FileCollection(Directory.GetCurrentDirectory());
        files.Add(DocumentType.Article, new[] { $"{YmlDataDirectory}/{fileName}" }, TestDataDirectory);
        files.Add(DocumentType.Overwrite, new[] { $"{OverwriteDataDirectory}/{overwriteFileName}" }, TestDataDirectory);

        BuildDocument(files);

        var outputRawModelPath = GetRawModelFilePath(fileName);
        Assert.IsTrue(File.Exists(outputRawModelPath));
        var model = JsonUtility.Deserialize<ApiBuildOutput>(outputRawModelPath);
        Assert.IsNotNull(model);

        Assert.AreEqual("\n<p sourcefile=\"TestData/overwrite/cntk.core.Value.md\" sourcestartlinenumber=\"5\"><strong sourcefile=\"TestData/overwrite/cntk.core.Value.md\" sourcestartlinenumber=\"5\">conceptual</strong> of <code sourcefile=\"TestData/overwrite/cntk.core.Value.md\" sourcestartlinenumber=\"5\">cntk.core.Value</code></p>\n", model.Conceptual);
        Assert.AreEqual("<p sourcefile=\"TestData/overwrite/cntk.core.Value.md\" sourcestartlinenumber=\"1\">summary of cntk.core.Value</p>\n", model.Summary);
    }

    #endregion

    #region JavaScript

    [TestMethod]
    public void ProcessJavaScriptReferencesShouldSucceed()
    {
        var fileNames = new string[] { "azure.ApplicationTokenCredentials.yml" };
        var files = new FileCollection(Directory.GetCurrentDirectory());
        files.Add(DocumentType.Article, fileNames.Select(f => $"{YmlDataDirectory}/{f}"), TestDataDirectory);

        BuildDocument(files);

        foreach (var fileName in fileNames)
        {
            var outputRawModelPath = GetRawModelFilePath(fileName);
            Assert.IsTrue(File.Exists(outputRawModelPath));
        }
    }

    #endregion

    [TestMethod]
    public void ProcessItemWithEmptyUidShouldFail()
    {
        var fileNames = new string[] { "invalid.yml" };
        var files = new FileCollection(Directory.GetCurrentDirectory());
        files.Add(DocumentType.Article, fileNames.Select(f => $"{YmlDataDirectory}/{f}"), TestDataDirectory);

        using var listener = new TestListenerScope();
        BuildDocument(files);
        Assert.IsNotNull(listener.Items);
        Assert.AreEqual(2, listener.Items.Count);
        Assert.Contains("Uid must not be null or empty", listener.Items[1].Message);
    }

    private void BuildDocument(FileCollection files)
    {
        var parameters = new DocumentBuildParameters
        {
            Files = files,
            OutputBaseDir = _outputFolder,
            ApplyTemplateSettings = _applyTemplateSettings,
            Metadata = new Dictionary<string, object>
            {
                ["meta"] = "Test UniversalReferenceDocumentProcessor",
            }.ToImmutableDictionary(),
            TemplateManager = _templateManager
        };

        using var builder = new DocumentBuilder(LoadAssemblies(), []);
        builder.Build(parameters);
    }

    private static IEnumerable<System.Reflection.Assembly> LoadAssemblies()
    {
        yield return typeof(UniversalReferenceDocumentProcessor).Assembly;
    }

    private string GetRawModelFilePath(string fileName)
    {
        return Path.Combine(_outputFolder, YmlDirectoryName, Path.ChangeExtension(fileName, RawModelFileExtension));
    }
}
