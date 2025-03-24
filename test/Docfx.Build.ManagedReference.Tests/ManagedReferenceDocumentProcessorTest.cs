// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

using Docfx.Build.Engine;
using Docfx.Build.ManagedReference.BuildOutputs;
using Docfx.Common;
using Docfx.DataContracts.Common;
using Docfx.Plugins;
using Docfx.Tests.Common;

using Newtonsoft.Json.Linq;

namespace Docfx.Build.ManagedReference.Tests;

[TestClass]
public class ManagedReferenceDocumentProcessorTest : TestBase
{
    private readonly string _outputFolder;
    private readonly FileCollection _defaultFiles;
    private readonly ApplyTemplateSettings _applyTemplateSettings;
    private readonly TemplateManager _templateManager;

    private const string RawModelFileExtension = ".raw.json";
    private const string MrefDirectory = "mref";

    public ManagedReferenceDocumentProcessorTest()
    {
        _outputFolder = GetRandomFolder();
        string inputFolder = GetRandomFolder();
        _defaultFiles = new FileCollection(Directory.GetCurrentDirectory());
        _defaultFiles.Add(DocumentType.Article, ["TestData/mref/CatLibrary.Cat-2.yml"], "TestData/");
        _applyTemplateSettings = new ApplyTemplateSettings(inputFolder, _outputFolder)
        {
            RawModelExportSettings = { Export = true },
            TransformDocument = true,
        };

        _templateManager = new TemplateManager(["template"], null, "TestData/");
    }

    [TestMethod]
    public void ProcessMrefShouldSucceed()
    {
        FileCollection files = new(_defaultFiles);
        BuildDocument(files);

        var outputRawModelPath = GetRawModelFilePath("CatLibrary.Cat-2.yml");
        Assert.IsTrue(File.Exists(outputRawModelPath));
        var model = JsonUtility.Deserialize<ApiBuildOutput>(outputRawModelPath);
        Assert.IsNotNull(model);

        Assert.AreEqual("Hello world!", model.Metadata["meta"]);
        Assert.AreEqual("item level metadata should overwrite page level metadata.", model.Metadata["anotherMeta"]);
        Assert.ContainsSingle(model.Attributes);
        Assert.AreEqual("System.SerializableAttribute.#ctor", model.Attributes[0].Constructor);
        Assert.IsEmpty(model.Attributes[0].Arguments);
        Assert.AreEqual("System.SerializableAttribute", model.Attributes[0].Type);

        Assert.AreEqual(2, model.Implements.Count);

        Assert.ContainsSingle(model.Inheritance);

        Assert.AreEqual(6, model.InheritedMembers.Count);

        Assert.AreEqual(2, model.Syntax.Content.Count);
        Assert.AreEqual("csharp", model.Syntax.Content[0].Language);
        Assert.AreEqual("<p>[A](http://A/).</p>\n", model.AdditionalNotes.Implementer);
        Assert.AreEqual("[Serializable]\npublic class Cat<T, K> : ICat, IAnimal where T : class, new ()where K : struct", model.Syntax.Content[0].Value);
        Assert.AreEqual("vb", model.Syntax.Content[1].Language);
        Assert.AreEqual("<Serializable>\nPublic Class Cat(Of T As {Class, New}, K As Structure)\n    Implements ICat, IAnimal", model.Syntax.Content[1].Value);

        Assert.AreEqual(2, model.Syntax.TypeParameters.Count);
        Assert.AreEqual("T", model.Syntax.TypeParameters[0].Name);
        Assert.AreEqual("<p sourcefile=\"TestData/mref/CatLibrary.Cat-2.yml\" sourcestartlinenumber=\"1\">This type should be class and can new instance.</p>\n", model.Syntax.TypeParameters[0].Description);
        Assert.AreEqual("K", model.Syntax.TypeParameters[1].Name);
        Assert.AreEqual("<p sourcefile=\"TestData/mref/CatLibrary.Cat-2.yml\" sourcestartlinenumber=\"1\">This type is a struct type, class type can't be used for this parameter.</p>\n", model.Syntax.TypeParameters[1].Description);

        Assert.ContainsSingle(model.Examples);
        Assert.AreEqual("<p>Here's example of how to create an instance of **Cat** class. As T is limited with <code>class</code> and K is limited with <code>struct</code>.</p>\n<pre><code class=\"c#\">    var a = new Cat(object, int)();\n    int catNumber = new int();\n    unsafe\n    {\n        a.GetFeetLength(catNumber);\n    }</code></pre>\n<p>As you see, here we bring in <strong>pointer</strong> so we need to add <span class=\"languagekeyword\">unsafe</span> keyword.</p>\n", model.Examples[0]);

        Assert.AreEqual(20, model.Children.Count);
        var cm = model.Children[1];
        Assert.AreEqual("<p>[A](http://A/).</p>\n", cm.AdditionalNotes.Implementer);
        Assert.AreEqual("<p>[B](http://B/).</p>\n", cm.AdditionalNotes.Inheritor);
        Assert.AreEqual("<p>[C](http://C/).</p>\n", cm.AdditionalNotes.Caller);

    }

    [TestMethod]
    public void ProcessMrefWithComplexFileNameShouldSucceed()
    {
        FileCollection files = new(_defaultFiles);
        files.RemoveAll(s => true);
        files.Add(DocumentType.Article, ["TestData/mref/Namespace1.Class1`2.yml", "TestData/mref/Namespace1.Class1`2.#ctor.yml"], "TestData/");
        BuildDocument(files);

        var outputRawModelPath = GetRawModelFilePath("Namespace1.Class1`2.yml");
        Assert.IsTrue(File.Exists(outputRawModelPath));
        var model = JsonUtility.Deserialize<ApiBuildOutput>(outputRawModelPath);
        Assert.IsNotNull(model);
        outputRawModelPath = GetRawModelFilePath("Namespace1.Class1`2.#ctor.yml");
        Assert.IsTrue(File.Exists(outputRawModelPath));
        model = JsonUtility.Deserialize<ApiBuildOutput>(outputRawModelPath);
        Assert.IsNotNull(model);
        var outputHtml = GetOutputFilePath("mref/Namespace1.Class1`2.html");
        Assert.IsTrue(File.Exists(outputHtml));
        var content = File.ReadAllText(outputHtml);
        Assert.AreEqual("<p><a class=\"xref\" href=\"Namespace1.Class1%602.%23ctor.html#constructor\">Constructor</a></p>\n", content);
    }

    [TestMethod]
    public void ProcessMrefWithXRefMapShouldSucceed()
    {
        var files = new FileCollection(_defaultFiles);
        BuildDocument(files);

        var xrefMapPath = Path.Combine(Directory.GetCurrentDirectory(), _outputFolder, XRefArchive.MajorFileName);
        Assert.IsTrue(File.Exists(xrefMapPath));

        var xrefMap = YamlUtility.Deserialize<XRefMap>(xrefMapPath);

        Assert.IsNotNull(xrefMap.References);
        Assert.AreEqual(34, xrefMap.References.Count);
    }

    [TestMethod]
    public void ProcessMrefWithDefaultOverwriteShouldSucceed()
    {
        FileCollection files = new(_defaultFiles);
        files.Add(DocumentType.Overwrite, ["TestData/overwrite/mref.overwrite.default.md"]);
        BuildDocument(files);
        {
            var outputRawModelPath = GetRawModelFilePath("CatLibrary.Cat-2.yml");
            Assert.IsTrue(File.Exists(outputRawModelPath));
            var model = JsonUtility.Deserialize<ApiBuildOutput>(outputRawModelPath);
            Assert.AreEqual("<p sourcefile=\"TestData/overwrite/mref.overwrite.default.md\" sourcestartlinenumber=\"1\">Overwrite summary</p>", model.Children[0].Metadata["summary"].ToString().Trim());
            Assert.AreEqual("<p sourcefile=\"TestData/overwrite/mref.overwrite.default.md\" sourcestartlinenumber=\"6\">Overwrite content</p>", model.Children[0].Conceptual.Trim());
        }
    }

    [TestMethod]
    public void ProcessMrefWithSimpleOverwriteShouldSucceed()
    {
        FileCollection files = new(_defaultFiles);
        files.Add(DocumentType.Overwrite, ["TestData/overwrite/mref.overwrite.simple.md"]);
        BuildDocument(files);
        var outputRawModelPath = GetRawModelFilePath("CatLibrary.Cat-2.yml");
        Assert.IsTrue(File.Exists(outputRawModelPath));
        var model = JsonUtility.Deserialize<ApiBuildOutput>(outputRawModelPath);
        Assert.AreEqual("\n<p sourcefile=\"TestData/overwrite/mref.overwrite.simple.md\" sourcestartlinenumber=\"6\">Overwrite content</p>\n", model.Summary);
        Assert.IsNull(model.Conceptual);
    }

    [TestMethod]
    public void ProcessMrefWithParametersOverwriteShouldSucceed()
    {
        var files = new FileCollection(_defaultFiles);
        files.Add(DocumentType.Overwrite, ["TestData/overwrite/mref.overwrite.parameters.md"]);
        BuildDocument(files);
        var outputRawModelPath = GetRawModelFilePath("CatLibrary.Cat-2.yml");
        Assert.IsTrue(File.Exists(outputRawModelPath));
        var model = JsonUtility.Deserialize<ApiBuildOutput>(outputRawModelPath);

        var method = model.Children.First(s => s.Uid == "CatLibrary.Cat`2.CatLibrary#IAnimal#Eat``1(``0)");

        // Verify overwrite parameters
        Assert.AreEqual("<p sourcefile=\"TestData/overwrite/mref.overwrite.parameters.md\" sourcestartlinenumber=\"1\">The overwritten description for a</p>\n", method.Syntax.Parameters[0].Description);
        Assert.IsNotNull(method.Syntax.Parameters[0].Type);
        Assert.AreEqual("\n<p sourcefile=\"TestData/overwrite/mref.overwrite.parameters.md\" sourcestartlinenumber=\"12\">This is overwritten type parameters</p>\n", method.Syntax.TypeParameters[0].Description);
        Assert.IsNull(model.Conceptual);
    }

    [TestMethod]
    public void ProcessMrefWithNotPredefinedOverwriteShouldSucceed()
    {
        FileCollection files = new(_defaultFiles);
        files.Add(DocumentType.Overwrite, ["TestData/overwrite/mref.overwrite.not.predefined.md"]);
        BuildDocument(files);
        {
            var outputRawModelPath = GetRawModelFilePath("CatLibrary.Cat-2.yml");
            Assert.IsTrue(File.Exists(outputRawModelPath));
            var model = JsonUtility.Deserialize<ApiBuildOutput>(outputRawModelPath);

            Assert.AreEqual("\n<p sourcefile=\"TestData/overwrite/mref.overwrite.not.predefined.md\" sourcestartlinenumber=\"6\">Overwrite content</p>\n"
                , model.Metadata["not_defined_property"]);
        }
    }

    [TestMethod]
    public void ProcessMrefWithDynamicDevLangsShouldSucceed()
    {
        FileCollection files = new(_defaultFiles);
        files.RemoveAll(s => true);
        files.Add(DocumentType.Article, ["TestData/mref/System.String.yml"], "TestData/");

        BuildDocument(files);

        var outputRawModelPath = GetRawModelFilePath("System.String.yml");
        Assert.IsTrue(File.Exists(outputRawModelPath));
        var model = JsonUtility.Deserialize<ApiBuildOutput>(outputRawModelPath);
        Assert.IsNotNull(model);
        Assert.IsNotNull(model.Syntax);
        Assert.IsNotNull(model.Syntax.Content);
        Assert.AreEqual(4, model.Syntax.Content.Count);
        Assert.AreEqual("public ref class String sealed", model.Syntax.Content.First(c => c.Language == "cpp").Value);
        Assert.AreEqual("public sealed class String", model.Syntax.Content.First(c => c.Language == "csharp").Value);
        Assert.AreEqual("type String", model.Syntax.Content.First(c => c.Language == "fsharp").Value);
        Assert.AreEqual("Public NotInheritable Class String", model.Syntax.Content.First(c => c.Language == "vb").Value);
    }

    [TestMethod]
    public void ProcessMrefWithInvalidCrossReferenceShouldWarn()
    {
        var files = new FileCollection(Directory.GetCurrentDirectory());
        files.Add(DocumentType.Article, ["TestData/mref/System.String.yml"], "TestData/");
        files.Add(DocumentType.Overwrite, ["TestData/overwrite/mref.overwrite.invalid.ref.md"]);

        using var listener = new TestListenerScope(LogLevel.Info);

        BuildDocument(files);

        var warnings = listener.GetItemsByLogLevel(LogLevel.Warning);
        Assert.ContainsSingle(warnings);
        var warning = warnings.Single();
        Assert.AreEqual("2 invalid cross reference(s) \"<xref:invalidXref1>\", \"<xref:invalidXref2>\".", warning.Message);
        Assert.AreEqual("TestData/mref/System.String.yml", warning.File);

        var infos = listener.GetItemsByLogLevel(LogLevel.Info).Where(i => i.Message.Contains("Details for invalid cross reference(s)")).ToList();
        Assert.ContainsSingle(infos);
        Assert.AreEqual("Details for invalid cross reference(s): \"<xref:invalidXref1>\" in line 6, \"<xref:invalidXref2>\" in line 8", infos[0].Message);
        Assert.AreEqual("TestData/overwrite/mref.overwrite.invalid.ref.md", infos[0].File);
        Assert.IsNull(infos[0].Line);
    }

    [TestMethod]
    public void ProcessMrefWithInvalidOverwriteShouldFail()
    {
        FileCollection files = new(_defaultFiles);
        files.Add(DocumentType.Overwrite, ["TestData/overwrite/mref.overwrite.invalid.md"]);
        Assert.Throws<DocumentException>(() => BuildDocument(files));
    }

    [TestMethod]
    public void ProcessMrefWithRemarksOverwriteShouldSucceed()
    {
        var files = new FileCollection(_defaultFiles);
        files.Add(DocumentType.Overwrite, ["TestData/overwrite/mref.overwrite.remarks.md"]);
        BuildDocument(files);
        {
            var outputRawModelPath = GetRawModelFilePath("CatLibrary.Cat-2.yml");
            Assert.IsTrue(File.Exists(outputRawModelPath));
            var model = JsonUtility.Deserialize<ApiBuildOutput>(outputRawModelPath);
            var method = model.Children.First(s => s.Uid == "CatLibrary.Cat`2.#ctor(`0)");
            Assert.AreEqual("\n<p sourcefile=\"TestData/overwrite/mref.overwrite.remarks.md\" sourcestartlinenumber=\"6\">Remarks content</p>\n", method.Remarks);
        }
    }

    [TestMethod]
    public void ProcessMrefWithMultiUidOverwriteShouldSucceed()
    {
        var files = new FileCollection(_defaultFiles);
        files.Add(DocumentType.Overwrite, ["TestData/overwrite/mref.overwrite.multi.uid.md"]);
        BuildDocument(files);
        {
            var outputRawModelPath = GetRawModelFilePath("CatLibrary.Cat-2.yml");
            Assert.IsTrue(File.Exists(outputRawModelPath));
            var model = JsonUtility.Deserialize<ApiBuildOutput>(outputRawModelPath);
            Assert.AreEqual("\n<p sourcefile=\"TestData/overwrite/mref.overwrite.multi.uid.md\" sourcestartlinenumber=\"6\">Overwrite content1</p>\n", model.Conceptual);
            Assert.AreEqual("\n<p sourcefile=\"TestData/overwrite/mref.overwrite.multi.uid.md\" sourcestartlinenumber=\"13\">Overwrite &quot;content2&quot;</p>\n", model.Summary);
            Assert.AreEqual("\n<p sourcefile=\"TestData/overwrite/mref.overwrite.multi.uid.md\" sourcestartlinenumber=\"20\">Overwrite 'content3'</p>\n", model.Metadata["not_defined_property"]);
        }
    }

    [TestMethod]
    public void SystemKeysListShouldBeComplete()
    {
        var files = new FileCollection(_defaultFiles);
        BuildDocument(files);
        var outputRawModelPath = GetRawModelFilePath("CatLibrary.Cat-2.yml");
        Assert.IsTrue(File.Exists(outputRawModelPath));
        var model = JsonUtility.Deserialize<Dictionary<string, object>>(outputRawModelPath);
        var systemKeys = ToList(model[Constants.PropertyName.SystemKeys]);
        Assert.IsNotEmpty(systemKeys);
        foreach (var key in model.Keys.Where(key => key[0] != '_' && key != "meta" && key != "anotherMeta"))
        {
            Assert.Contains(key, systemKeys);
        }
    }

    [TestMethod]
    public void LoadArticleWithEmptyFileShouldWarnAndReturnNull()
    {
        var fileWithNoContent = "TestData/mref/FileWithNoContent.yml";
        var file = new FileAndType(Directory.GetCurrentDirectory(), fileWithNoContent, DocumentType.Article);
        var processor = new ManagedReferenceDocumentProcessor();

        using var listener = new TestListenerScope();

        var actualFileModel = processor.Load(file, null);

        var warnings = listener.GetItemsByLogLevel(LogLevel.Warning);
        Assert.ContainsSingle(warnings);
        var warning = warnings.Single();
        Assert.AreEqual("Please add `YamlMime` as the first line of file, e.g.: `### YamlMime:ManagedReference`, otherwise the file will be not treated as ManagedReference source file in near future.", warning.Message);
        Assert.AreEqual(fileWithNoContent, warning.File);

        Assert.IsNull(actualFileModel);
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
                ["meta"] = "Hello world!",
            }.ToImmutableDictionary(),
            TemplateManager = _templateManager
        };

        using var builder = new DocumentBuilder(LoadAssemblies(), []);
        builder.Build(parameters);
    }

    private static IEnumerable<System.Reflection.Assembly> LoadAssemblies()
    {
        yield return typeof(ManagedReferenceDocumentProcessor).Assembly;
    }

    private string GetRawModelFilePath(string fileName)
    {
        return Path.Combine(_outputFolder, MrefDirectory, Path.ChangeExtension(fileName, RawModelFileExtension));
    }

    private string GetOutputFilePath(string fileName)
    {
        return Path.GetFullPath(Path.Combine(_outputFolder, Path.ChangeExtension(fileName, "html")));
    }

    private static List<object> ToList(object value)
    {
        return value is List<object> list
            ? list
            : ((JArray)value).Cast<object>().ToList();
    }
}
