// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Tests.Common;

namespace Docfx.Build.Engine.Tests;

[DoNotParallelize]
[TestClass]
public class TemplatePreprocessorLoaderUnitTest : TestBase
{
    private readonly string _inputFolder;

    public TemplatePreprocessorLoaderUnitTest()
    {
        _inputFolder = GetRandomFolder();
    }

    [TestMethod]
    public void TestLoaderWithValidInput()
    {
        using var listener = new TestListenerScope();
        var preprocessor = Load("a.ext.TMPL.js", "exports.transform = function(model) { return model; }");

        Assert.IsEmpty(listener.Items);

        Assert.IsNotNull(preprocessor);
        Assert.IsFalse(preprocessor.ContainsGetOptions);
        Assert.IsTrue(preprocessor.ContainsModelTransformation);

        var input = new { a = 1 };
        var output = preprocessor.TransformModel(input);
        Assert.AreEqual(input.a, ((dynamic)output).a);
    }

    private ITemplatePreprocessor Load(string path, string content)
    {
        var loader = new PreprocessorLoader(new LocalFileResourceReader(_inputFolder), null, 64);
        return loader.Load(new ResourceInfo(path, content));
    }
}
