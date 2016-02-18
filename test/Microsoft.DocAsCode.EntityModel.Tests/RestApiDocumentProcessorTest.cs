// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;

    using Xunit;

    using Microsoft.DocAsCode.EntityModel.Builders;
    using Microsoft.DocAsCode.EntityModel.Plugins;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Utility;
    using Microsoft.DocAsCode.EntityModel.ViewModels;

    [Trait("Owner", "lianwei")]
    [Trait("EntityType", "RestApiDocumentProcessor")]
    [Collection("docfx STA")]
    public class RestApiDocumentProcessorTest : IClassFixture<RestApiDocumentProcessorFixture>
    {
        private string _outputFolder;
        private string _inputFolder;
        private string _templateFolder;

        public RestApiDocumentProcessorTest(RestApiDocumentProcessorFixture fixture)
        {
            _outputFolder = Path.GetFullPath(fixture.OutputFolder);
            _inputFolder = Path.GetFullPath(fixture.InputFolder);
            _templateFolder = Path.GetFullPath(fixture.TemplateFolder);
        }

        [Fact]
        public void TestRestApiDocumentProcessorWithSwaggerJsonFile()
        {
            const string RawModelFileExtension = ".raw.json";

            // TODO: Add override markdown file
            FileCollection files = new FileCollection(Environment.CurrentDirectory);
            files.Add(DocumentType.Article, new[] { "TestData/contacts_swagger2.json" }, p => (((RelativePath)p) - (RelativePath)"TestData/").ToString());

            using (var builder = new DocumentBuilder())
            {
                var applyTemplateSettings = new ApplyTemplateSettings(_inputFolder, _outputFolder);
                applyTemplateSettings.RawModelExportSettings.Export = true;
                var parameters = new DocumentBuildParameters
                {
                    Files = files,
                    OutputBaseDir = _outputFolder,
                    ApplyTemplateSettings = applyTemplateSettings,
                    Metadata = new Dictionary<string, object>
                    {
                        ["meta"] = "Hello world!",
                    }.ToImmutableDictionary()
                };
                builder.Build(parameters);
            }

            // Check REST API
            var outputRawModelPath = Path.Combine(_outputFolder, Path.ChangeExtension("contacts_swagger2.json", RawModelFileExtension));
            Assert.True(File.Exists(outputRawModelPath));
            var model = RestApiDocumentProcessor.GetModelWithoutRef<RestApiItemViewModel>(File.ReadAllText(outputRawModelPath));
            Assert.Equal("graph.windows.net/myorganization/Contacts", model.Uid);
            Assert.Equal("graph_windows_net_myorganization_Contacts", model.HtmlId);
            Assert.Equal(9, model.Children.Count);
            Assert.Equal("Hello world!", model.Metadata["meta"]);
            var item1 = model.Children[0];
            Assert.Equal("graph.windows.net/myorganization/Contacts/get contacts", item1.Uid);
            Assert.Equal("<p>You can get a collection of contacts from your tenant.</p>\n", item1.Summary);
            Assert.Equal(1, item1.Parameters.Count);
            Assert.Equal("1.6", item1.Parameters[0].Metadata["default"]);
            Assert.Equal(1, item1.Responses.Count);
            Assert.Equal("200", item1.Responses[0].HttpStatusCode);
        }
    }
}
