// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;

using Acornima.Ast;

using Docfx.Tests.Common;

using Jint;
using Jint.Runtime;

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

    /// <summary>
    /// A function exported by a <c>require</c>d module belongs to that module's engine, so calling it from
    /// the template's engine is not a public entry into the module's engine and does not reset the module
    /// engine's execution constraints. The timeout deadline is armed only on reset, so unless the
    /// preprocessor resets every engine it owns per call, a pooled preprocessor that has been alive longer
    /// than the timeout starts every module call against a deadline that has already passed - and fails
    /// every remaining document of the build.
    /// </summary>
    [Fact]
    public void TestModuleCallIsNotBoundedByAStaleDeadline()
    {
        using var listener = new TestListenerScope();

        // Generous enough that a cold CI runner cannot make a single call exceed it - the point of the test
        // is the deadline going stale between calls, not how long one call takes - while still keeping the
        // aging sleep below short.
        var timeout = TimeSpan.FromSeconds(1);

        CreateFile(
            "mod.js",
            "exports.value = function() { var s = 0; for (var i = 0; i < 2000; i++) { s = s + i; } return 'module:' + s; };",
            _inputFolder);

        var preprocessor = LoadWithTimeout(
            "a.ext.TMPL.js",
            "var mod = require('./mod.js'); exports.transform = function(model) { return { value: mod.value() }; }",
            timeout);

        // The first document is inside the window whether or not constraints are reset per call.
        Assert.Equal("module:1999000", ((dynamic)preprocessor.TransformModel(new { a = 1 })).value);

        // Age the preprocessor past its own timeout, which is what a pooled slot does during a real build.
        Thread.Sleep(timeout + TimeSpan.FromMilliseconds(500));

        Assert.Equal("module:1999000", ((dynamic)preprocessor.TransformModel(new { a = 1 })).value);
    }

    /// <summary>
    /// The other half of the same contract: rearming a module engine's constraints per call must not turn
    /// into removing them. A runaway loop inside a <c>require</c>d module has to be bounded too, and only
    /// the module engine's own constraints can bound it - the template engine's are checked only while
    /// template-engine statements are executing.
    /// </summary>
    [Fact]
    public void TestRunawayScriptInsideModuleIsStopped()
    {
        using var listener = new TestListenerScope();
        CreateFile("mod.js", "exports.value = function() { var i = 0; while (true) { i = i + 1; } };", _inputFolder);

        var preprocessor = LoadWithTimeout(
            "a.ext.TMPL.js",
            "var mod = require('./mod.js'); exports.transform = function(model) { return { value: mod.value() }; }",
            TimeSpan.FromSeconds(1));

        // Which limit fires first is a race decided by machine speed - a fast machine burns the statement
        // budget inside the timeout window, a slow one runs out of time first - so assert that the runaway
        // was stopped, not which of the two limits stopped it.
        var exception = Assert.ThrowsAny<Exception>(() => preprocessor.TransformModel(new { a = 1 }));
        Assert.True(
            exception is TimeoutException or StatementsCountOverflowException,
            $"Expected an execution constraint to stop the module's runaway loop, but got: {exception}");
    }

    private ITemplatePreprocessor Load(string path, string content)
    {
        var loader = new PreprocessorLoader(new LocalFileResourceReader(_inputFolder), null, 64);
        return loader.Load(new ResourceInfo(path, content));
    }

    private ITemplatePreprocessor LoadWithTimeout(string path, string content, TimeSpan executionTimeout)
    {
        return new TemplateJintPreprocessor(
            new LocalFileResourceReader(_inputFolder),
            new ResourceInfo(path, content),
            context: null,
            name: null,
            preparedScripts: new ConcurrentDictionary<string, Prepared<Script>>(),
            executionTimeout: executionTimeout);
    }
}
