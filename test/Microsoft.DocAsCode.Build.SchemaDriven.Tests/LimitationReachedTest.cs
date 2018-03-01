// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.SchemaDriven.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Composition;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Microsoft.DocAsCode.Build.Engine;
    using Microsoft.DocAsCode.Build.SchemaDriven.Processors;
    using Microsoft.DocAsCode.Build.TableOfContents;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Exceptions;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Tests.Common;

    using Newtonsoft.Json.Linq;
    using Xunit;

    [Trait("Owner", "lianwei")]
    [Trait("EntityType", "SchemaDrivenProcessorTest")]
    [Collection("docfx STA")]
    public class LimitationReachedTest : TestBase
    {
        private const string SpecPath = @"TestData\specs\docfx_document_schema.md";
        private static Regex InputMatcher = new Regex(@"```(yml|yaml)\s*(### YamlMime:[\s\S]*?)\s*```", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static Regex SchemaMatcher = new Regex(@"```json\s*(\{\s*""\$schema""[\s\S]*?)\s*```", RegexOptions.Compiled | RegexOptions.IgnoreCase);

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

            _templateManager = new TemplateManager(null, null, new List<string> { "template" }, null, _templateFolder);
        }

        [Fact(Skip = "Mannually run this testcase, as it will influence the result of other test cases")]
        public void TestSchemaReachedLimits()
        {
            // Json.NET schema has limitation of 1000 calls per hour
            using (var listener = new TestListenerScope("TestInvalidMetadataReference"))
            {
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

                FileCollection files = new FileCollection(_defaultFiles);
                files.Add(DocumentType.Article, inputFiles, _inputFolder);
                BuildDocument(files);
                Assert.Equal(2, listener.Items.Count);
                Assert.Single(listener.Items.Where(s => s.Message == "There is no template processing document type(s): LimitTest"));
                Assert.True(LimitationReached(listener));
            }
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

            using (var builder = new DocumentBuilder(LoadAssemblies(), ImmutableArray<string>.Empty, null))
            {
                builder.Build(parameters);
            }
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
}
