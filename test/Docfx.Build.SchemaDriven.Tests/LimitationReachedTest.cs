// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Text.RegularExpressions;

using Docfx.Build.Engine;
using Docfx.Build.TableOfContents;
using Docfx.Plugins;
using Docfx.Tests.Common;
using Xunit;

namespace Docfx.Build.SchemaDriven.Tests;

[Collection("docfx STA")]
public class LimitationReachedTest : TestBase
{
    private static Regex InputMatcher = new(@"```(yml|yaml)\s*(### YamlMime:[\s\S]*?)\s*```", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static Regex SchemaMatcher = new(@"```json\s*(\{\s*""\$schema""[\s\S]*?)\s*```", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private string _outputFolder;
    private string _inputFolder;
    private string _templateFolder;
    private FileCollection _defaultFiles;
    private ApplyTemplateSettings _applyTemplateSettings;
    private TemplateManager _templateManager;

    private const string RawModelFileExtension = ".raw.json";

    public LimitationReachedTest()
    {
        _outputFolder = GetRandomFolder();
        _inputFolder = GetRandomFolder();
        _templateFolder = GetRandomFolder();
        _defaultFiles = new FileCollection(Directory.GetCurrentDirectory());
        _applyTemplateSettings = new ApplyTemplateSettings(_inputFolder, _outputFolder)
        {
            RawModelExportSettings = { Export = true },
            TransformDocument = true,
        };

        _templateManager = new TemplateManager(new List<string> { "template" }, null, _templateFolder);
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
            TemplateManager = _templateManager,
        };

        using var builder = new DocumentBuilder(LoadAssemblies(), ImmutableArray<string>.Empty);
        builder.Build(parameters);
    }

    private static IEnumerable<System.Reflection.Assembly> LoadAssemblies()
    {
        yield return typeof(SchemaDrivenDocumentProcessor).Assembly;
        yield return typeof(TocDocumentProcessor).Assembly;
        yield return typeof(SchemaDrivenProcessorTest).Assembly;
    }

    private static bool LimitationReached(TestListenerScope listener)
    {
        return listener.Items.SingleOrDefault(s => s.Message.StartsWith("Limitation reached when validating")) != null;
    }
}
