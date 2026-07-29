// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Tests.Common;

using Xunit;

namespace Docfx.Build.Engine.Tests;

[Collection("docfx STA")]
public class TemplatePreprocessorLoaderUnitTest : TestBase
{
    private readonly string _inputFolder;

    public TemplatePreprocessorLoaderUnitTest()
    {
        _inputFolder = GetRandomFolder();
    }

    [Fact]
    public void TestLoaderWithValidInput()
    {
        using var listener = new TestListenerScope();
        var preprocessor = Load("a.ext.TMPL.js", "exports.transform = function(model) { return model; }");

        Assert.Empty(listener.Items);

        Assert.NotNull(preprocessor);
        Assert.False(preprocessor.ContainsGetOptions);
        Assert.True(preprocessor.ContainsModelTransformation);

        var input = new { a = 1 };
        var output = preprocessor.TransformModel(input);
        Assert.Equal(input.a, ((dynamic)output).a);
    }

    [Fact]
    public void TestGetOptionsIsReadFromScriptResult()
    {
        using var listener = new TestListenerScope();
        var preprocessor = Load(
            "a.ext.TMPL.js",
            "exports.getOptions = function(model) { return { isShared: true, bookmarks: { uid1: 'bookmark1' } }; }");

        Assert.NotNull(preprocessor);
        Assert.True(preprocessor.ContainsGetOptions);

        var options = new Template(null, preprocessor).GetOptions(new { a = 1 });

        Assert.NotNull(options);
        Assert.True(options.IsShared);
        Assert.Equal("bookmark1", options.Bookmarks["uid1"]);
    }

    /// <summary>
    /// The shipped templates chain <c>require</c> - a primary script requires a common script which requires
    /// another - and every module gets its own engine, so this covers the whole chain resolving and the
    /// exported functions still being callable.
    /// </summary>
    [Fact]
    public void TestRequireResolvesChainedModules()
    {
        using var listener = new TestListenerScope();
        CreateFile("inner.js", "exports.value = function() { return 'inner'; };", _inputFolder);
        CreateFile("outer.js", "var inner = require('./inner.js'); exports.value = function() { return inner.value() + '+outer'; };", _inputFolder);

        var preprocessor = Load(
            "a.ext.TMPL.js",
            "var outer = require('./outer.js'); exports.transform = function(model) { return { value: outer.value() }; }");

        Assert.NotNull(preprocessor);
        Assert.True(preprocessor.ContainsModelTransformation);

        var output = preprocessor.TransformModel(new { a = 1 });
        Assert.Equal("inner+outer", ((dynamic)output).value);
    }

    /// <summary>
    /// <c>console</c> is registered lazily, so it is only built for engines whose script mentions it. Reading
    /// the name has to be indistinguishable from an eagerly registered global.
    /// </summary>
    [Fact]
    public void TestConsoleIsAvailableToScripts()
    {
        using var listener = new TestListenerScope();
        var preprocessor = Load("a.ext.TMPL.js", "exports.transform = function(model) { console.warn('from template'); return model; }");

        preprocessor.TransformModel(new { a = 1 });

        Assert.Contains(listener.Items, i => i.Message == "from template");
    }

    [Fact]
    public void TestRunawayScriptIsStopped()
    {
        using var listener = new TestListenerScope();
        var preprocessor = Load("a.ext.TMPL.js", "exports.transform = function(model) { var i = 0; while (true) { i = i + 1; } }");

        Assert.NotNull(preprocessor);
        Assert.Throws<InvalidPreprocessorException>(() => preprocessor.TransformModel(new { a = 1 }));
    }

    private ITemplatePreprocessor Load(string path, string content)
    {
        var loader = new PreprocessorLoader(new LocalFileResourceReader(_inputFolder), null, 64);
        return loader.Load(new ResourceInfo(path, content));
    }
}
