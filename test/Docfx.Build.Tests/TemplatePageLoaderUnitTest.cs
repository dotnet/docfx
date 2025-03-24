// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Tests.Common;

namespace Docfx.Build.Engine.Tests;

[DoNotParallelize]
[TestClass]
public class TemplatePageLoaderUnitTest : TestBase
{
    private readonly string _inputFolder;

    public TemplatePageLoaderUnitTest()
    {
        _inputFolder = GetRandomFolder();
    }

    [TestMethod]
    public void TestLoaderWhenNoFileExists()
    {
        using var listener = new TestListenerScope();
        var templates = LoadAllTemplates();
        Assert.IsEmpty(listener.Items);
        Assert.IsEmpty(templates);

        CreateFile("a.js", string.Empty, _inputFolder);
        templates = LoadAllTemplates();
        Assert.IsEmpty(listener.Items);
        Assert.IsEmpty(templates);

        // only allows file under root folder
        CreateFile("sub/a.tmpl", string.Empty, _inputFolder);
        templates = LoadAllTemplates();
        Assert.IsEmpty(listener.Items);
        Assert.IsEmpty(templates);

        CreateFile("a.tmpl.js", string.Empty, _inputFolder);
        templates = LoadAllTemplates();
        Assert.IsEmpty(listener.Items);
        Assert.IsEmpty(templates);
    }

    [TestMethod]
    public void TestLoaderWhenRendererExists()
    {
        CreateFile("a.tmpl", string.Empty, _inputFolder);

        using var listener = new TestListenerScope();
        var templates = LoadAllTemplates();

        Assert.IsEmpty(listener.Items);

        Assert.ContainsSingle(templates);
        var template = templates[0];
        Assert.IsNotNull(template.Renderer);
        Assert.AreEqual(TemplateType.Default, template.TemplateType);
        Assert.AreEqual("a", template.Name);
        Assert.AreEqual("a", template.Type);
        Assert.AreEqual(string.Empty, template.Extension);
        Assert.IsNull(template.Preprocessor);
        Assert.IsFalse(template.ContainsGetOptions);
        Assert.IsFalse(template.ContainsModelTransformation);
    }

    [TestMethod]
    public void TestLoaderWhenPreprocessorExists()
    {
        CreateFile("a.primary.tmpl", string.Empty, _inputFolder);
        CreateFile("a.primary.js", "exports.transform = function(){}", _inputFolder);

        using var listener = new TestListenerScope();
        var templates = LoadAllTemplates();

        Assert.IsEmpty(listener.Items);

        Assert.ContainsSingle(templates);
        var template = templates[0];
        Assert.IsNotNull(template.Renderer);
        Assert.AreEqual(TemplateType.Primary, template.TemplateType);
        Assert.AreEqual("a.primary", template.Name);
        Assert.AreEqual("a", template.Type);
        Assert.AreEqual(string.Empty, template.Extension);
        Assert.IsNotNull(template.Preprocessor);
        Assert.IsFalse(template.ContainsGetOptions);
        Assert.IsTrue(template.ContainsModelTransformation);

        var output = template.TransformModel(new { a = 1 });
        Assert.IsNull(output);
    }

    [TestMethod]
    public void TestLoaderWhenStandalonePreprocessorExists()
    {
        CreateFile("a.ext.TMPL.js", "exports.transform = function(){}", _inputFolder);

        using var listener = new TestListenerScope();
        var templates = LoadAllTemplates();

        Assert.IsEmpty(listener.Items);

        Assert.ContainsSingle(templates);
        var template = templates[0];
        Assert.IsNull(template.Renderer);
        Assert.AreEqual(TemplateType.Default, template.TemplateType);
        Assert.AreEqual("a.ext", template.Name);
        Assert.AreEqual("a", template.Type);
        Assert.AreEqual(".ext", template.Extension);
        Assert.IsNotNull(template.Preprocessor);
        Assert.IsFalse(template.ContainsGetOptions);
        Assert.IsTrue(template.ContainsModelTransformation);

        var output = template.TransformModel(new { a = 1 });
        Assert.IsNull(output);
    }

    private List<Template> LoadAllTemplates()
    {
        var loader = new TemplatePageLoader(new LocalFileResourceReader(_inputFolder), null, 64);
        return loader.LoadAll().ToList();
    }
}
