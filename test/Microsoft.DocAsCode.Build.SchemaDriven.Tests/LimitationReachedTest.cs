﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Text.RegularExpressions;

using Microsoft.DocAsCode.Build.Engine;
using Microsoft.DocAsCode.Build.TableOfContents;
using Microsoft.DocAsCode.Plugins;
using Microsoft.DocAsCode.Tests.Common;
using Xunit;

namespace Microsoft.DocAsCode.Build.SchemaDriven.Tests;

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

    [Fact(Skip = "Manually run this testcase, as it will influence the result of other test cases")]
    public void TestSchemaReachedLimits()
    {
        // Json.NET schema has limitation of 1000 calls per hour
        using var listener = new TestListenerScope("TestInvalidMetadataReference");
        var schemaFile = CreateFile("template/schemas/limit.test.schema.json", @"
{
  ""$schema"": ""http://dotnet.github.io/docfx/schemas/v1.0/schema.json#"",
  ""version"": ""1.0.0"",
  ""title"": ""LimitTest"",
  ""description"": ""A simple test schema for sdp"",
  ""type"": ""object"",
  ""properties"": {
      ""metadata"": {
            ""type"": ""string""
      }
  }
}
", _templateFolder);

        var inputFiles = Enumerable.Range(0, 2000)
            .Select(s => CreateFile($"normal{s}.yml", @"### YamlMime:LimitTest
metadata: Web Apps Documentation
", _inputFolder)).ToArray();

        FileCollection files = new(_defaultFiles);
        files.Add(DocumentType.Article, inputFiles, _inputFolder);
        BuildDocument(files);
        Assert.Equal(2, listener.Items.Count);
        Assert.Single(listener.Items.Where(s => s.Message == "There is no template processing document type(s): LimitTest"));
        Assert.True(LimitationReached(listener));
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

    private bool LimitationReached(TestListenerScope listener)
    {
        return listener.Items.SingleOrDefault(s => s.Message.StartsWith("Limitation reached when validating")) != null;
    }
}
