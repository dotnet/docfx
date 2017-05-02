// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.UniversalReference.Tests
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode.Build.Engine;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Tests.Common;

    using Xunit;

    [Trait("EntityType", "UniversalReferenceDocumentProcessor")]
    public class UniversalReferenceDocumentProcessorTest : TestBase
    {
        private string _outputFolder;
        private string _inputFolder;
        private string _templateFolder;
        private ApplyTemplateSettings _applyTemplateSettings;
        private TemplateManager _templateManager;

        private const string RawModelFileExtension = ".raw.json";
        private const string TestDataDirectory = "TestData";
        private const string YmlDirectoryName = "yml";
        private const string RawModelDirectoryName = "raw";
        private static readonly string YmlDataDirectory = Path.Combine(TestDataDirectory, YmlDirectoryName);

        public UniversalReferenceDocumentProcessorTest()
        {
            _outputFolder = GetRandomFolder();
            _inputFolder = GetRandomFolder();
            _templateFolder = GetRandomFolder();
            _applyTemplateSettings = new ApplyTemplateSettings(_inputFolder, _outputFolder)
            {
                RawModelExportSettings = { Export = true },
                TransformDocument = true,
            };
            _templateManager = new TemplateManager(null, null, new List<string> { "template" }, null, "TestData/");
        }

        [Fact]
        public void ProcessPythonReferencesShouldSucceed()
        {
            // TODO: inheritance tree is removed from data, wait for the design decision
            var fileNames = Directory.EnumerateFiles(YmlDataDirectory).Select(Path.GetFileName).ToList();
            var files = new FileCollection(Directory.GetCurrentDirectory());
            files.Add(DocumentType.Article, fileNames.Select(f => $"{YmlDataDirectory}/{f}"), TestDataDirectory);

            BuildDocument(files);

            foreach (var fileName in fileNames)
            {
                var outputRawModelPath = GetRawModelFilePath(fileName);
                Assert.True(File.Exists(outputRawModelPath));
                var rawModel = File.ReadAllText(outputRawModelPath);
            }
        }

        [Fact]
        public void ProcessModelShouldSucceed()
        {
            var fileName = "cntk.core.Value.yml";
            var files = new FileCollection(Directory.GetCurrentDirectory());
            files.Add(DocumentType.Article, new[] { $"{YmlDataDirectory}/{fileName}" }, TestDataDirectory);

            BuildDocument(files);

            var outputRawModelPath = GetRawModelFilePath(fileName);
            Assert.True(File.Exists(outputRawModelPath));
            var model = JsonUtility.Deserialize<ApiBuildOutput>(outputRawModelPath);
            Assert.NotNull(model);

            Assert.Equal(1, model.SupportedLanguages.Count());
            Assert.Equal("python", model.SupportedLanguages[0]);

            Assert.Equal("Class", model.Type);

            Assert.Equal("Value", model.Name[0].Value);
            Assert.Equal("cntk.core.Value", model.FullName[0].Value);

            Assert.Equal("https://github.com/Microsoft/CNTK", model.Source[0].Value.Remote.RemoteRepositoryUrl);
            Assert.Equal("cntk/core.py", model.Source[0].Value.Remote.RelativePath);
            Assert.Equal(182, model.Source[0].Value.StartLine);

            Assert.Equal(6, model.Syntax.Parameters.Count);
            Assert.Equal("shape", model.Syntax.Parameters[0].Name);
            Assert.Equal("tuple", model.Syntax.Parameters[0].Type[0].Uid);
            Assert.Equal("shape of the value", model.Syntax.Parameters[0].Description);

            Assert.Equal(1, model.Children.Count);
            Assert.Equal("python", model.Children[0].Language);
            Assert.Equal(5, model.Children[0].Value.Count);

            Assert.Equal("Method", model.Children[0].Value[0].Type);
            Assert.Equal("cntk.core.Value.create", model.Children[0].Value[0].Uid);
            Assert.Equal("create", model.Children[0].Value[0].Name[0].Value);
            Assert.Equal("cntk.core.Value.create", model.Children[0].Value[0].FullName[0].Value);
            Assert.Equal("Creates a `Value` object.", model.Children[0].Value[0].Summary);
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
                    ["meta"] = "Test UniversalReferenceDocumentProcessor",
                }.ToImmutableDictionary(),
                TemplateManager = _templateManager
            };

            using (var builder = new DocumentBuilder(LoadAssemblies(), ImmutableArray<string>.Empty, null))
            {
                builder.Build(parameters);
            }
        }

        private static IEnumerable<System.Reflection.Assembly> LoadAssemblies()
        {
            yield return typeof(UniversalReferenceDocumentProcessor).Assembly;
        }

        private string GetRawModelFilePath(string fileName)
        {
            return Path.Combine(_outputFolder, YmlDirectoryName, Path.ChangeExtension(fileName, RawModelFileExtension));
        }
    }
}
