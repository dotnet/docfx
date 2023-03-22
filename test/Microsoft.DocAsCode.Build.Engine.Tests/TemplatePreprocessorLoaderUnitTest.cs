// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DocAsCode.Tests.Common;

using Xunit;

namespace Microsoft.DocAsCode.Build.Engine.Tests;

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
        using var listener = new TestListenerScope("TestLoaderWhenStandalonePreprocessorExists");
        var preprocessor = Load("a.ext.TMPL.js", "exports.transform = function(model) { return model; }");

        Assert.Empty(listener.Items);

        Assert.NotNull(preprocessor);
        Assert.False(preprocessor.ContainsGetOptions);
        Assert.True(preprocessor.ContainsModelTransformation);

        var input = new { a = 1 };
        var output = preprocessor.TransformModel(input);
        Assert.Equal(input.a, ((dynamic)output).a);
    }

    private ITemplatePreprocessor Load(string path, string content)
    {
        var loader = new PreprocessorLoader(new LocalFileResourceReader(_inputFolder), null, 64);
        return loader.Load(new ResourceInfo(path, content));
    }
}
