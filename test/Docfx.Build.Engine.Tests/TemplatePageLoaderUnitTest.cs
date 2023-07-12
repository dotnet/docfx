// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Tests.Common;

using Xunit;

namespace Docfx.Build.Engine.Tests;

[Collection("docfx STA")]
public class TemplatePageLoaderUnitTest : TestBase
{
    private readonly string _inputFolder;

    public TemplatePageLoaderUnitTest()
    {
        _inputFolder = GetRandomFolder();
    }

    [Fact]
    public void TestLoaderWhenNoFileExists()
    {
        using var listener = new TestListenerScope("NoTemplate");
        var templates = LoadAllTemplates();
        Assert.Empty(listener.Items);
        Assert.Empty(templates);

        var file1 = CreateFile("a.js", string.Empty, _inputFolder);
        templates = LoadAllTemplates();
        Assert.Empty(listener.Items);
        Assert.Empty(templates);

        // only allows file under root folder
        var file2 = CreateFile("sub/a.tmpl", string.Empty, _inputFolder);
        templates = LoadAllTemplates();
        Assert.Empty(listener.Items);
        Assert.Empty(templates);

        var file3 = CreateFile("a.tmpl.js", string.Empty, _inputFolder);
        templates = LoadAllTemplates();
        Assert.Empty(listener.Items);
        Assert.Empty(templates);
    }

    [Fact]
    public void TestLoaderWhenRendererExists()
    {
        var file1 = CreateFile("a.tmpl", string.Empty, _inputFolder);

        using var listener = new TestListenerScope("TestLoaderWhenRendererExists");
        var templates = LoadAllTemplates();

        Assert.Empty(listener.Items);

        Assert.Single(templates);
        var template = templates[0];
        Assert.NotNull(template.Renderer);
        Assert.Equal(TemplateType.Default, template.TemplateType);
        Assert.Equal("a", template.Name);
        Assert.Equal("a", template.Type);
        Assert.Equal(string.Empty, template.Extension);
        Assert.Null(template.Preprocessor);
        Assert.False(template.ContainsGetOptions);
        Assert.False(template.ContainsModelTransformation);
    }

    [Fact]
    public void TestLoaderWhenPreprocessorExists()
    {
        var file1 = CreateFile("a.primary.tmpl", string.Empty, _inputFolder);
        var file2 = CreateFile("a.primary.js", "exports.transform = function(){}", _inputFolder);

        using var listener = new TestListenerScope("TestLoaderWhenPreprocessorExists");
        var templates = LoadAllTemplates();

        Assert.Empty(listener.Items);

        Assert.Single(templates);
        var template = templates[0];
        Assert.NotNull(template.Renderer);
        Assert.Equal(TemplateType.Primary, template.TemplateType);
        Assert.Equal("a.primary", template.Name);
        Assert.Equal("a", template.Type);
        Assert.Equal(string.Empty, template.Extension);
        Assert.NotNull(template.Preprocessor);
        Assert.False(template.ContainsGetOptions);
        Assert.True(template.ContainsModelTransformation);

        var output = template.TransformModel(new { a = 1 });
        Assert.Null(output);
    }

    [Fact]
    public void TestLoaderWhenStandalonePreprocessorExists()
    {
        var file1 = CreateFile("a.ext.TMPL.js", "exports.transform = function(){}", _inputFolder);

        using var listener = new TestListenerScope("TestLoaderWhenStandalonePreprocessorExists");
        var templates = LoadAllTemplates();

        Assert.Empty(listener.Items);

        Assert.Single(templates);
        var template = templates[0];
        Assert.Null(template.Renderer);
        Assert.Equal(TemplateType.Default, template.TemplateType);
        Assert.Equal("a.ext", template.Name);
        Assert.Equal("a", template.Type);
        Assert.Equal(".ext", template.Extension);
        Assert.NotNull(template.Preprocessor);
        Assert.False(template.ContainsGetOptions);
        Assert.True(template.ContainsModelTransformation);

        var output = template.TransformModel(new { a = 1 });
        Assert.Null(output);
    }

    private List<Template> LoadAllTemplates()
    {
        var loader = new TemplatePageLoader(new LocalFileResourceReader(_inputFolder), null, 64);
        return loader.LoadAll().ToList();
    }
}
