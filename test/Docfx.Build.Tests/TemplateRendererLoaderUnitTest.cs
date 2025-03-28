// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Tests.Common;

namespace Docfx.Build.Engine.Tests;

[DoNotParallelize]
[TestClass]
public class TemplateRendererLoaderUnitTest : TestBase
{
    private readonly string _inputFolder;

    public TemplateRendererLoaderUnitTest()
    {
        _inputFolder = GetRandomFolder();
    }

    [TestMethod]
    public void TestLoaderWhenNoFileExists()
    {
        using var listener = new TestListenerScope();
        var renderers = LoadAllRenderers();
        Assert.IsEmpty(listener.Items);
        Assert.IsEmpty(renderers);

        var file1 = CreateFile("a.js", string.Empty, _inputFolder);
        renderers = LoadAllRenderers();
        Assert.IsEmpty(listener.Items);
        Assert.IsEmpty(renderers);

        // only allows file under root folder
        var file2 = CreateFile("sub/a.tmpl", string.Empty, _inputFolder);
        renderers = LoadAllRenderers();
        Assert.IsEmpty(listener.Items);
        Assert.IsEmpty(renderers);

        var file3 = CreateFile("a.tmpl.js", string.Empty, _inputFolder);
        renderers = LoadAllRenderers();
        Assert.IsEmpty(listener.Items);
        Assert.IsEmpty(renderers);
    }

    [TestMethod]
    public void TestLoaderWithValidInput()
    {
        var file1 = CreateFile("a.tmpl", "{{name}}", _inputFolder);

        using var listener = new TestListenerScope();
        var renderers = LoadAllRenderers();

        Assert.IsEmpty(listener.Items);

        Assert.ContainsSingle(renderers);
        var renderer = renderers[0];
        Assert.IsNotNull(renderer);

        var model = new { name = "model" };

        var output = renderer.Render(model);
        Assert.AreEqual("model", output);
    }

    [TestMethod]
    public void TestSingleFileLoaderWithValidInput()
    {
        var path = "a.tmpl";
        var file1 = CreateFile(path, "{{name}}", _inputFolder);

        using var listener = new TestListenerScope();
        var renderer = Load(path);

        Assert.IsEmpty(listener.Items);

        Assert.IsNotNull(renderer);

        var model = new { name = "model" };

        var output = renderer.Render(model);
        Assert.AreEqual("model", output);
    }

    private List<ITemplateRenderer> LoadAllRenderers()
    {
        var loader = new RendererLoader(new LocalFileResourceReader(_inputFolder), 64);
        return loader.LoadAll().ToList();
    }

    private ITemplateRenderer Load(string path)
    {
        var loader = new RendererLoader(new LocalFileResourceReader(_inputFolder), 64);
        return loader.Load(path);
    }
}
