// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.ManagedReference.Tests
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Web;

    using Microsoft.DocAsCode.Build.Engine;
    using Microsoft.DocAsCode.Build.ConceptualDocuments;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Tests.Common;

    using Xunit;

    [Trait("Owner", "lianwei")]
    [Trait("EntityType", "ConceptualDocumentProcessorTest")]
    public class ConceptualDocumentProcessorTest : TestBase
    {
        private readonly string _outputFolder;
        private readonly string _inputFolder;
        private readonly string _templateFolder;
        private readonly FileCollection _defaultFiles;
        private readonly FileCreator _fileCreator;
        private ApplyTemplateSettings _applyTemplateSettings;
        private TemplateManager _templateManager;
        private const string RawModelFileExtension = ".raw.json";

        public ConceptualDocumentProcessorTest()
        {
            _outputFolder = GetRandomFolder();
            _inputFolder = GetRandomFolder();
            _templateFolder = GetRandomFolder();
            _fileCreator = new FileCreator(_inputFolder);
            _defaultFiles = new FileCollection(_inputFolder);

            _applyTemplateSettings = new ApplyTemplateSettings(_inputFolder, _outputFolder)
            {
                RawModelExportSettings = {Export = true},
                TransformDocument = true
            };
            EnvironmentContext.SetBaseDirectory(_inputFolder);
            EnvironmentContext.SetOutputDirectory(_outputFolder);

            // Prepare conceptual template
            var templateCreator = new FileCreator(_templateFolder);
            var file = templateCreator.CreateFile(@"{{{conceptual}}}", "conceptual.html.tmpl", "default");
            _templateManager = new TemplateManager(null, null, new List<string> { "default" }, null, _templateFolder);
        }

        public override void Dispose()
        {
            EnvironmentContext.Clean();
            base.Dispose();
        }

        [Fact]
        public void ProcessMarkdownFileWithComplexCharsShouldSucceed()
        {
            var fileName1 = "A#ctor.md";
            var fileName2 = "normal.md";
            var file1 = _fileCreator.CreateFile($@"
[Main]({HttpUtility.UrlEncode(fileName2)})
", fileName1);
            var file2 = _fileCreator.CreateFile($@"
[Constructor]({HttpUtility.UrlEncode(fileName1)})
", fileName2);
            var files = new FileCollection(_defaultFiles);
            files.Add(DocumentType.Article, new[] { file1, file2 });
            BuildDocument(files);
            {
                var outputRawModelPath = GetRawModelFilePath(file2);
                Assert.True(File.Exists(outputRawModelPath));
                var outputHtml = GetOutputFilePath(file2);
                Assert.True(File.Exists(outputHtml));
                var content = File.ReadAllText(outputHtml);
                Assert.Equal("<p><a href=\"A%23ctor.html\">Constructor</a></p>\n",
content);
            }
        }

        #region Private Helpers
        private string GetRawModelFilePath(string fileName)
        {
            return Path.GetFullPath(Path.Combine(_outputFolder, Path.ChangeExtension(fileName, RawModelFileExtension)));
        }

        private string GetOutputFilePath(string fileName)
        {
            return Path.GetFullPath(Path.Combine(_outputFolder, Path.ChangeExtension(fileName, "html")));
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
                TemplateManager = _templateManager
            };

            using (var builder = new DocumentBuilder(LoadAssemblies(), ImmutableArray<string>.Empty, null))
            {
                builder.Build(parameters);
            }
        }

        private static IEnumerable<System.Reflection.Assembly> LoadAssemblies()
        {
            yield return typeof(ConceptualDocumentProcessor).Assembly;
        }

        private sealed class FileCreator
        {
            private readonly string _rootDir;
            public FileCreator(string rootDir)
            {
                _rootDir = rootDir ?? Directory.GetCurrentDirectory();
            }

            public string CreateFile(string content, string fileName = null, string folder = null)
            {
                fileName = fileName ?? Path.GetRandomFileName() + ".md";

                fileName = Path.Combine(folder ?? string.Empty, fileName);

                var filePath = Path.Combine(_rootDir, fileName);
                var dir = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                File.WriteAllText(filePath, content);
                return fileName.Replace('\\', '/');
            }
        }

        #endregion
    }
}
