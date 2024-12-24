// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Common;
using Docfx.Plugins;
using Docfx.Tests.Common;

using Xunit;

namespace Docfx.Build.Engine.Tests;

[Collection("docfx STA")]
public class TemplateProcessorUnitTest : TestBase
{
    private readonly string _inputFolder;
    private readonly string _outputFolder;
    private readonly string _templateFolder;

    public TemplateProcessorUnitTest()
    {
        _inputFolder = GetRandomFolder();
        _outputFolder = GetRandomFolder();
        _templateFolder = GetRandomFolder();
    }

    [Fact]
    public void TestXrefWithTemplate()
    {
        CreateFile("partials/xref.html.tmpl", "<h2>{{uid}}</h2><p>{{summary}}</p>{{#isGood}}Good!{{/isGood}}", _templateFolder);
        CreateFile("index.html.tmpl", @"
<xref uid=""{{reference}}"" template=""partials/xref.html.tmpl"" />
", _templateFolder);

        var xref = new XRefSpec
        {
            Uid = "reference",
            Href = "ref.html",
            ["summary"] = "hello world",
            ["isGood"] = true,
        };
        var output = Process("index", "input", new { reference = "reference" }, xref);

        Assert.Equal($"{_outputFolder}/input.html".ToNormalizedFullPath(), output.Output[".html"].RelativePath.ToNormalizedPath());
        Assert.Equal(@"
<h2>reference</h2><p>hello world</p>Good!
", File.ReadAllText(Path.Combine(_outputFolder, "input.html")));

    }

    private ManifestItem Process(string documentType, string fileName, object content, XRefSpec spec)
    {
        var reader = new LocalFileResourceReader(_templateFolder);
        var context = new DocumentBuildContext(_outputFolder);
        context.RegisterInternalXrefSpec(spec);
        var processor = new TemplateProcessor(reader, context, 64);
        var inputItem = new InternalManifestItem
        {
            DocumentType = documentType,
            Extension = "html",
            FileWithoutExtension = Path.GetFullPath(Path.Combine(_outputFolder, Path.GetFileNameWithoutExtension(fileName))),
            LocalPathFromRoot = fileName,
            Content = content,
        };
        return processor.Process([inputItem], new ApplyTemplateSettings(_inputFolder, _outputFolder))[0];
    }
}
