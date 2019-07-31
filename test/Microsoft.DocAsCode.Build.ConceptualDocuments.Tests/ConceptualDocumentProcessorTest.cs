// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.ManagedReference.Tests
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;
    using System.Web;

    using Microsoft.DocAsCode.Build.Engine;
    using Microsoft.DocAsCode.Build.ConceptualDocuments;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Tests.Common;

    using Newtonsoft.Json.Linq;
    using Xunit;

    [Collection("docfx STA")]
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
                RawModelExportSettings = { Export = true },
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
        public void ProcessMarkdownResultWithEncodedUrlShouldSucceed()
        {
            var markdownResult = new MarkupResult
            {
                Html = @"<p><a href=""%7E/docs/csharp/language-reference/keywords/select-clause.md""></p>"
            };

            markdownResult = MarkupUtility.Parse(markdownResult, "docs/framework/data/wcf/how-to-project-query-results-wcf-data-services.md", ImmutableDictionary.Create<string, FileAndType>());
            Assert.Equal("~/docs/csharp/language-reference/keywords/select-clause.md", markdownResult.LinkToFiles.First());
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

        [Fact]
        public void ProcessMarkdownFileWithBreakLinkShouldSucceed()
        {
            var fileName = "normal.md";
            var file = _fileCreator.CreateFile("[Main](a#b)", fileName);
            var files = new FileCollection(_defaultFiles);
            files.Add(DocumentType.Article, new[] { file });
            BuildDocument(files);
            {
                var outputRawModelPath = GetRawModelFilePath(file);
                Assert.True(File.Exists(outputRawModelPath));
                var outputHtml = GetOutputFilePath(file);
                Assert.True(File.Exists(outputHtml));
                var content = File.ReadAllText(outputHtml);
                Assert.Equal("<p><a href=\"a#b\">Main</a></p>\n", content);
            }
        }

        [Fact]
        public void ProcessMarkdownFileWithBreakLinkInTokenShouldSucceed()
        {
            var fileName = "normal.md";
            var tokenFileName = "token.md";
            var file = _fileCreator.CreateFile($"[!include[]({tokenFileName})]", fileName);
            _fileCreator.CreateFile("[Main](a#b)", tokenFileName);
            var files = new FileCollection(_defaultFiles);
            files.Add(DocumentType.Article, new[] { file });
            BuildDocument(files);
            {
                var outputRawModelPath = GetRawModelFilePath(file);
                Assert.True(File.Exists(outputRawModelPath));
                var outputHtml = GetOutputFilePath(file);
                Assert.True(File.Exists(outputHtml));
                var content = File.ReadAllText(outputHtml);
                Assert.Equal($@"<p><a href=""a#b"">Main</a></p>
".Replace("\r\n", "\n"),
content);
            }
        }

        [Fact]
        public void SystemKeysListShouldBeComplete()
        {
            var fileName = "test.md";
            var file = _fileCreator.CreateFile("# test", fileName);
            var files = new FileCollection(_defaultFiles);
            files.Add(DocumentType.Article, new[] { file });
            BuildDocument(files);
            {
                var outputRawModelPath = GetRawModelFilePath(file);
                Assert.True(File.Exists(outputRawModelPath));
                var model = JsonUtility.Deserialize<Dictionary<string, object>>(outputRawModelPath);
                var systemKeys = (JArray)model[Constants.PropertyName.SystemKeys];
                Assert.NotEmpty(systemKeys);
                foreach (var key in model.Keys.Where(key => key[0] != '_' && key != "meta"))
                {
                    Assert.Contains(key, systemKeys);
                }
            }
        }

        [Fact]
        public void ProcessMarkdownFileWithRenameOutputFileName()
        {
            var fileName1 = "a.md";
            var fileName2 = "b.md";
            var renameFile1 = "x.html";
            var renameFile2 = "y.html";
            var file1 = _fileCreator.CreateFile($@"---
outputFileName: {renameFile1}
---

[Main]({HttpUtility.UrlEncode(fileName2)})
", fileName1);
            var file2 = _fileCreator.CreateFile($@"---
outputFileName: {renameFile2}
---

[Constructor]({HttpUtility.UrlEncode(fileName1)})
", fileName2);
            var files = new FileCollection(_defaultFiles);
            files.Add(DocumentType.Article, new[] { file1, file2 });
            BuildDocument(files);
            {
                var outputRawModelPath = GetRawModelFilePath(renameFile2);
                Assert.True(File.Exists(outputRawModelPath));
                var outputHtml = GetOutputFilePath(renameFile2);
                Assert.True(File.Exists(outputHtml));
                var content = File.ReadAllText(outputHtml);
                Assert.Equal($"<p><a href=\"{renameFile1}\">Constructor</a></p>\n",
content);
            }
        }

        [Fact]
        public void ExtractTitle()
        {
            // arrange
            var fileName = "title.md";
            var content = @"# This is title

Some content";
            var file = _fileCreator.CreateFile(content, fileName);
            var files = new FileCollection(_defaultFiles);
            files.Add(DocumentType.Article, new[] { file });

            // act
            BuildDocument(files);

            // assert
            var outputRawModelPath = GetRawModelFilePath(file);
            Assert.True(File.Exists(outputRawModelPath));
            var model = JsonUtility.Deserialize<Dictionary<string, object>>(outputRawModelPath);
            Assert.True(model.TryGetValue("title", out var title));
            Assert.Equal("This is title", title);
            Assert.True(model.TryGetValue("rawTitle", out var rawTitle));
            Assert.Equal(
                "<h1 id=\"this-is-title\" sourcefile=\"title.md\" sourcestartlinenumber=\"1\" sourceendlinenumber=\"1\">This is title</h1>",
                rawTitle);
        }

        [Fact]
        public void ExtractTitleFromYamlHeader()
        {
            // arrange
            var fileName = "title.md";
            var content = @"---
title: Overwrite title
---

# This is title

Some content";
            var file = _fileCreator.CreateFile(content, fileName);
            var files = new FileCollection(_defaultFiles);
            files.Add(DocumentType.Article, new[] { file });

            // act
            BuildDocument(files);

            // assert
            var outputRawModelPath = GetRawModelFilePath(file);
            Assert.True(File.Exists(outputRawModelPath));
            var model = JsonUtility.Deserialize<Dictionary<string, object>>(outputRawModelPath);
            Assert.True(model.TryGetValue("title", out var title));
            Assert.Equal("Overwrite title", title);
        }

        [Fact]
        public void ExtractTitleFromH1IfItIsNullInYamlHeader()
        {
            // arrange
            var fileName = "title.md";
            var content = @"---
title:
---

# This is title

Some content";
            var file = _fileCreator.CreateFile(content, fileName);
            var files = new FileCollection(_defaultFiles);
            files.Add(DocumentType.Article, new[] { file });

            // act
            BuildDocument(files);

            // assert
            var outputRawModelPath = GetRawModelFilePath(file);
            Assert.True(File.Exists(outputRawModelPath));
            var model = JsonUtility.Deserialize<Dictionary<string, object>>(outputRawModelPath);
            Assert.True(model.TryGetValue("title", out var title));
            Assert.Equal("This is title", title);
        }

        [Fact]
        public void ExtractTitleFromH1IfItIsEmptyInYamlHeader()
        {
            // arrange
            var fileName = "title.md";
            var content = @"---
title: ''
---

# This is title

Some content";
            var file = _fileCreator.CreateFile(content, fileName);
            var files = new FileCollection(_defaultFiles);
            files.Add(DocumentType.Article, new[] { file });

            // act
            BuildDocument(files);

            // assert
            var outputRawModelPath = GetRawModelFilePath(file);
            Assert.True(File.Exists(outputRawModelPath));
            var model = JsonUtility.Deserialize<Dictionary<string, object>>(outputRawModelPath);
            Assert.True(model.TryGetValue("title", out var title));
            Assert.Equal("This is title", title);
        }

        [Fact]
        public void TitleOverwriteH1InMetadataCanOverwriteTitleFromH1()
        {
            // arrange
            var metadata = new Dictionary<string, object> { ["titleOverwriteH1"] = "this title overwrites title from H1" };
            var fileName = "title.md";
            var content = @"
# This is title from H1

Some content";
            var file = _fileCreator.CreateFile(content, fileName);
            var files = new FileCollection(_defaultFiles);
            files.Add(DocumentType.Article, new[] { file });

            // act
            BuildDocument(files, metadata);

            // assert
            var outputRawModelPath = GetRawModelFilePath(file);
            Assert.True(File.Exists(outputRawModelPath));
            var model = JsonUtility.Deserialize<Dictionary<string, object>>(outputRawModelPath);
            Assert.True(model.TryGetValue("title", out var title));
            Assert.Equal("this title overwrites title from H1", title);
        }

        [Fact]
        public void TitleOverwriteH1InMetadataCannotOverwriteTitleFromYamlHeader()
        {
            // arrange
            var metadata = new Dictionary<string, object> { ["titleOverwriteH1"] = "this title overwrites title from H1" };
            var fileName = "title.md";
            var content = @"---
title: This is title from YAML header
---

# This is title from H1

Some content";
            var file = _fileCreator.CreateFile(content, fileName);
            var files = new FileCollection(_defaultFiles);
            files.Add(DocumentType.Article, new[] { file });

            // act
            BuildDocument(files, metadata);

            // assert
            var outputRawModelPath = GetRawModelFilePath(file);
            Assert.True(File.Exists(outputRawModelPath));
            var model = JsonUtility.Deserialize<Dictionary<string, object>>(outputRawModelPath);
            Assert.True(model.TryGetValue("title", out var title));
            Assert.Equal("This is title from YAML header", title);
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

        private void BuildDocument(FileCollection files, Dictionary<string, object> metadata = null)
        {
            var parameters = new DocumentBuildParameters
            {
                Files = files,
                OutputBaseDir = _outputFolder,
                ApplyTemplateSettings = _applyTemplateSettings,
                Metadata = (metadata ?? new Dictionary<string, object> { ["meta"] = "Hello world!" }).ToImmutableDictionary(),
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
