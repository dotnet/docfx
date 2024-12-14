// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Build.Engine;
using Docfx.Common;
using Docfx.Plugins;
using Docfx.Tests.Common;

using Xunit;

namespace Docfx.Build.SchemaDriven.Tests;

[Collection("docfx STA")]
public class MarkdownFragmentsValidationTest : TestBase
{
    private string _outputFolder;
    private string _inputFolder;
    private string _templateFolder;
    private FileCollection _defaultFiles;
    private ApplyTemplateSettings _applyTemplateSettings;
    private TemplateManager _templateManager;
    private FileCollection _files;

    private TestLoggerListener _listener = new();
    private string _rawModelFilePath;

    private const string RawModelFileExtension = ".raw.json";

    [Fact]
    public void OverwriteUnEditableTest()
    {
        _outputFolder = GetRandomFolder();
        _inputFolder = GetRandomFolder();
        _templateFolder = GetRandomFolder();
        _defaultFiles = new FileCollection(Directory.GetCurrentDirectory());
        _applyTemplateSettings = new ApplyTemplateSettings(_inputFolder, _outputFolder) { RawModelExportSettings = { Export = true }, TransformDocument = true, };

        _templateManager = new TemplateManager(["template"], null, _templateFolder);

        _rawModelFilePath = GetRawModelFilePath("FragmentsValidation.yml");

        var schemaFile = CreateFile("template/schemas/fragments.validation.schema.json", File.ReadAllText("TestData/schemas/fragments.validation.schema.json"), _templateFolder);
        var yamlFile = CreateFile("FragmentsValidation.yml", File.ReadAllText("TestData/inputs/FragmentsValidation.yml"), _inputFolder);
        var mdFile = CreateFile("FragmentsValidation.yml.md", File.ReadAllText("TestData/inputs/FragmentsValidation.yml.md"), _inputFolder);
        _files = new FileCollection(_defaultFiles);
        _files.Add(DocumentType.Article, [yamlFile], _inputFolder);

        // Act
        Logger.RegisterListener(_listener);
        try
        {
            BuildDocument(_files);
        }
        finally
        {
            Logger.UnregisterListener(_listener);
        }

        var logs = _listener.Items;
        var warningLogs = logs.Where(l => l.Code == WarningCodes.Overwrite.InvalidMarkdownFragments).ToList();
        Assert.True(File.Exists(_rawModelFilePath));
        Assert.Equal(5, warningLogs.Count);
        Assert.Equal(
            @"Markdown property `depot_name` is not allowed inside a YAML code block
You cannot overwrite a readonly property: `site_name`, please add an `editable` tag on this property or mark its contentType as `markdown` in schema if you want to overwrite this property
There is an invalid H2: `name`: the contentType of this property in schema must be `markdown`
There is an invalid H2: `operations[id=""management.azure.com.advisor.fragmentsValidation.create""]/summary`: the contentType of this property in schema must be `markdown`
""/operations/1"" in overwrite object fails to overwrite ""/operations"" for ""management.azure.com.advisor.fragmentsValidation"" because it does not match any existing item.",
            string.Join(Environment.NewLine, warningLogs.Select(x => x.Message)),
            ignoreLineEndingDifferences: true);
        Assert.Equal("14", warningLogs[2].Line);
        Assert.Equal("17", warningLogs[3].Line);
    }

    private void BuildDocument(FileCollection files)
    {
        var parameters = new DocumentBuildParameters
        {
            Files = files,
            OutputBaseDir = _outputFolder,
            ApplyTemplateSettings = _applyTemplateSettings,
            TemplateManager = _templateManager,
        };

        using var builder = new DocumentBuilder(LoadAssemblies(), []);
        builder.Build(parameters);
    }

    private static IEnumerable<System.Reflection.Assembly> LoadAssemblies()
    {
        yield return typeof(SchemaDrivenDocumentProcessor).Assembly;
        yield return typeof(SchemaDrivenProcessorTest).Assembly;
    }

    private string GetRawModelFilePath(string fileName)
    {
        return Path.GetFullPath(Path.Combine(_outputFolder, Path.ChangeExtension(fileName, RawModelFileExtension)));
    }
}
