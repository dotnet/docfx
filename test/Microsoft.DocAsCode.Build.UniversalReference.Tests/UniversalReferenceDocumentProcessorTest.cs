// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.UniversalReference.Tests
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode.Build.Engine;
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
            var fileNames = Directory.EnumerateFiles(YmlDataDirectory).Select(Path.GetFileName).ToList();
            var files = new FileCollection(Directory.GetCurrentDirectory());
            files.Add(DocumentType.Article, fileNames.Select(f => $"{YmlDataDirectory}/{f}"), "TestData/");

            BuildDocument(files);

            foreach (var fileName in fileNames)
            {
                var outputRawModelPath = GetRawModelFilePath(fileName);
                Assert.True(File.Exists(outputRawModelPath));
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
