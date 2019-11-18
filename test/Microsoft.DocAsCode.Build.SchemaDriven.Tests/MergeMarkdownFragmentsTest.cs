namespace Microsoft.DocAsCode.Build.SchemaDriven.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Composition;
    using System.IO;
    using System.Linq;
    using Microsoft.DocAsCode.Build.Engine;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Tests.Common;

    using Newtonsoft.Json.Linq;
    using Xunit;

    [Trait("Owner", "renzeyu")]
    [Trait("EntityType", "SchemaDrivenProcessorTest")]
    [Collection("docfx STA")]
    public class MergeMarkdownFragmentsTest : TestBase
    {
        private string _outputFolder;
        private string _inputFolder;
        private string _templateFolder;
        private FileCollection _defaultFiles;
        private ApplyTemplateSettings _applyTemplateSettings;
        private TemplateManager _templateManager;
        private FileCollection _files;

        private TestLoggerListener _listener;
        private string _rawModelFilePath;

        private const string RawModelFileExtension = ".raw.json";

        public MergeMarkdownFragmentsTest()
        {
            _outputFolder = GetRandomFolder();
            _inputFolder = GetRandomFolder();
            _templateFolder = GetRandomFolder();
            _defaultFiles = new FileCollection(Directory.GetCurrentDirectory());
            _applyTemplateSettings = new ApplyTemplateSettings(_inputFolder, _outputFolder) { RawModelExportSettings = { Export = true }, TransformDocument = true, };

            _templateManager = new TemplateManager(null, null, new List<string> { "template" }, null, _templateFolder);

            _listener = TestLoggerListener.CreateLoggerListenerWithPhaseEqualFilter(null);
            _rawModelFilePath = GetRawModelFilePath("Suppressions.yml");

            var schemaFile = CreateFile("template/schemas/rest.mixed.schema.json", File.ReadAllText("TestData/schemas/rest.mixed.schema.json"), _templateFolder);
            var yamlFile = CreateFile("Suppressions.yml", File.ReadAllText("TestData/inputs/Suppressions.yml"), _inputFolder);
            _files = new FileCollection(_defaultFiles);
            _files.Add(DocumentType.Article, new[] { yamlFile }, _inputFolder);
        }

        [Fact]
        void TestMergeMarkdownFragments()
        {
            // Arrange
            var mdFile = CreateFile("Suppressions.yml.md", File.ReadAllText("TestData/inputs/Suppressions.yml.md"), _inputFolder);

            // Act
            BuildDocument(_files);

            // Assert
            Assert.True(File.Exists(_rawModelFilePath));
            var rawModel = JsonUtility.Deserialize<JObject>(_rawModelFilePath);
            Assert.Equal("bob", rawModel["author"]);
            Assert.Contains("Enables the snoozed or dismissed attribute", rawModel["operations"][0]["summary"].ToString());
            Assert.Contains("Some empty lines between H2 and this paragraph is tolerant", rawModel["definitions"][0]["properties"][0]["description"].ToString());
            Assert.Contains("This is a summary at YAML's", rawModel["summary"].ToString());
        }

        [Fact]
        public void TestMissingStartingH1CodeHeading()
        {
            // Arrange
            var mdFile = CreateFile(
                "Suppressions.yml.md",
                @"## `summary`
markdown content
",
                _inputFolder);

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
            var warningLogs = logs.Where(l => l.Code == WarningCodes.Overwrite.InvalidMarkdownFragments);
            Assert.True(File.Exists(_rawModelFilePath));
            var rawModel = JsonUtility.Deserialize<JObject>(_rawModelFilePath);
            Assert.Single(warningLogs);
            Assert.Equal("Unable to parse markdown fragments: Expect L1InlineCodeHeading", warningLogs.First().Message);
            Assert.Equal("1", warningLogs.First().Line);
            Assert.Null(rawModel["summary"]);
        }

        [Fact]
        public void TestInvalidSpaceMissing()
        {
            // Arrange
            var mdFile = CreateFile(
                "Suppressions.yml.md",
                @"#head_1_without_space

## `summary`
markdown content
",
                _inputFolder);

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
            var warningLogs = logs.Where(l => l.Code == WarningCodes.Overwrite.InvalidMarkdownFragments);
            Assert.True(File.Exists(_rawModelFilePath));
            var rawModel = JsonUtility.Deserialize<JObject>(_rawModelFilePath);
            Assert.Single(warningLogs);
            Assert.Equal("Unable to parse markdown fragments: Expect L1InlineCodeHeading", warningLogs.First().Message);
            Assert.Equal("1", warningLogs.First().Line);
            Assert.Null(rawModel["summary"]);
        }

        [Fact]
        public void TestValidSpaceMissing()
        {
            // Arrange
            var mdFile = CreateFile(
                "Suppressions.yml.md",
                @"# `management.azure.com.advisor.suppressions`

## `summary`
markdown content

##head_3_without_space
markdown content
",
                _inputFolder);

            // Act
            BuildDocument(_files);

            Assert.True(File.Exists(_rawModelFilePath));
            var rawModel = JsonUtility.Deserialize<JObject>(_rawModelFilePath);
            Assert.Contains("##head_3_without_space", rawModel["summary"].ToString());
        }

        [Fact]
        public void TestInvalidYaml()
        {
            // Arrange
            var mdFile = CreateFile(
                "Suppressions.yml.md",
                @"# `management.azure.com.advisor.suppressions`
```
author:author_without_space
```

## `summary`
markdown content
",
                _inputFolder);

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
            var warningLogs = logs.Where(l => l.Code == WarningCodes.Overwrite.InvalidMarkdownFragments);
            Assert.True(File.Exists(_rawModelFilePath));
            var rawModel = JsonUtility.Deserialize<JObject>(_rawModelFilePath);
            Assert.Single(warningLogs);
            Assert.Equal("Unable to parse markdown fragments: Encountered an invalid YAML code block: (Line: 1, Col: 1, Idx: 0) - (Line: 1, Col: 28, Idx: 27): Exception during deserialization", warningLogs.First().Message);
            Assert.Equal("2", warningLogs.First().Line);
            Assert.Null(rawModel["summary"]);
        }

        [Fact]
        public void TestInvalidOPath()
        {
            // Arrange
            var mdFile = CreateFile(
                "Suppressions.yml.md",
                @"# `management.azure.com.advisor.suppressions`

## `operations[id=]/summary`

markdown_content

## `summary`
markdown content
",
                _inputFolder);

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
            var warningLogs = logs.Where(l => l.Code == WarningCodes.Overwrite.InvalidMarkdownFragments);
            Assert.True(File.Exists(_rawModelFilePath));
            var rawModel = JsonUtility.Deserialize<JObject>(_rawModelFilePath);
            Assert.Single(warningLogs);
            Assert.Equal("Unable to parse markdown fragments: operations[id=]/summary is not a valid OPath", warningLogs.First().Message);
            Assert.Equal("3", warningLogs.First().Line);
            Assert.Null(rawModel["summary"]);
        }

        [Fact]
        public void TestNotExistedUid()
        {
            // Arrange
            var mdFile = CreateFile(
                "Suppressions.yml.md",
                @"# `uid_not_exist`

## `summary`
markdown content

# `management.azure.com.advisor.suppressions`
```
author: bob
```

## `operations[id=""management.azure.com.advisor.suppressions.create""]/summary`
Enables the snoozed or dismissed attribute of a **recommendation**.

## `definitions[name=""Application 1""]/description`

## `definitions[name=""Application 1""]/properties[name=""displayName""]/description`

## `definitions[name=""Application 1""]/properties[name=""id""]/description`

Some empty lines between H2 and this paragraph is tolerant

## ``summary``

## `definitions[name=""CloudError""]/description`

## `definitions[name=""CloudError""]/properties[name=""error""]/description`

",
                _inputFolder);

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
            var warningLogs = logs.Where(l => l.Code == WarningCodes.Overwrite.InvalidMarkdownFragments);
            Assert.True(File.Exists(_rawModelFilePath));
            var rawModel = JsonUtility.Deserialize<JObject>(_rawModelFilePath);
            Assert.Single(warningLogs);
            Assert.Equal("Unable to find UidDefinition for Uid: uid_not_exist", warningLogs.First().Message);
            Assert.Empty(rawModel["summary"]);
        }

        [Fact]
        public void TestDuplicateOPathsInYamlCodeBlockAndContentsBlock()
        {
            // Arrange
            var mdFile = CreateFile(
                "Suppressions.yml.md",
                @"# `management.azure.com.advisor.suppressions`
```
name: name overwrite
definitions:
- name: Application 1
  properties:
  - name: id
    description: overwrite in yaml block
  - name: displayName
    description: overwrite in yaml block
```

## `definitions[name=""Application 1""]/properties[name=""displayName""]/description`
overwrite in contents block
",
                _inputFolder);

            // Act
            BuildDocument(_files);

            Assert.True(File.Exists(_rawModelFilePath));
            var rawModel = JsonUtility.Deserialize<JObject>(_rawModelFilePath);
            Assert.Equal("name overwrite", rawModel["name"]);
            Assert.Equal($"<p sourcefile=\"{_inputFolder}/Suppressions.yml.md\" sourcestartlinenumber=\"1\" sourceendlinenumber=\"1\" jsonPath=\"/definitions/0/properties/0/description\">overwrite in yaml block</p>\n", rawModel["definitions"][0]["properties"][0]["description"].ToString());
            Assert.Equal($"<p sourceFile=\"{_inputFolder}/Suppressions.yml.md\" sourceStartLineNumber=\"14\">overwrite in contents block</p>\n", rawModel["definitions"][0]["properties"][1]["description"].ToString());
        }

        [Fact]
        public void TestFragmentsWithIncremental()
        {
            using (var listener = new TestListenerScope(nameof(TestFragmentsWithIncremental)))
            {
                // first build
                using (new LoggerPhaseScope("FirstBuild"))
                {
                    BuildDocument(_files);
                }
                Assert.True(File.Exists(_rawModelFilePath));
                var rawModel = JsonUtility.Deserialize<JObject>(_rawModelFilePath);
                Assert.Null(rawModel["summary"]);
                Assert.NotNull(listener.Items.FirstOrDefault(s => s.Message.StartsWith("There is no template processing document type(s): RESTMixedTest")));
                ClearLog(listener.Items);

                // add fragments
                var mdFile = CreateFile(
                    "Suppressions.yml.md",
                    @"# `management.azure.com.advisor.suppressions`
## ``summary``
I add a summary.
With [!include[invalid](invalid.md)]",
                    _inputFolder);
                using (new LoggerPhaseScope("AddFragments"))
                {
                    BuildDocument(_files);
                }
                Assert.True(File.Exists(_rawModelFilePath));
                rawModel = JsonUtility.Deserialize<JObject>(_rawModelFilePath);
                Assert.NotNull(rawModel["summary"]);
                Assert.Contains("I add a summary", rawModel["summary"].ToString());
                Assert.NotNull(listener.Items.FirstOrDefault(s => s.Message.StartsWith("There is no template processing document type(s): RESTMixedTest")));
                Assert.NotNull(listener.Items.FirstOrDefault(s => s.Message.Contains("Cannot resolve") && s.Message.Contains("invalid.md")));
                List<string> lastMessages;
                var messages = ClearLog(listener.Items);

                // modify fragments
                UpdateFile(
                    "Suppressions.yml.md",
                    new string[] { @"# `management.azure.com.advisor.suppressions`",
                    "## ``summary``",
                    "I update a summary.",
                    "With [!include[invalid](invalid.md)]",
                    },
                    _inputFolder);
                using (new LoggerPhaseScope("ModifyFragments"))
                {
                    BuildDocument(_files);
                }
                Assert.True(File.Exists(_rawModelFilePath));
                rawModel = JsonUtility.Deserialize<JObject>(_rawModelFilePath);
                Assert.NotNull(rawModel["summary"]);
                Assert.Contains("I update a summary", rawModel["summary"].ToString());
                Assert.NotNull(listener.Items.FirstOrDefault(s => s.Message.StartsWith("There is no template processing document type(s): RESTMixedTest")));
                Assert.NotNull(listener.Items.FirstOrDefault(s => s.Message.Contains("Cannot resolve") && s.Message.Contains("invalid.md")));
                lastMessages = messages;
                messages = ClearLog(listener.Items);
                Assert.True(messages.SequenceEqual(lastMessages));

                // rebuild
                using (new LoggerPhaseScope("Rebuild"))
                {
                    BuildDocument(_files);
                }
                Assert.True(File.Exists(_rawModelFilePath));
                rawModel = JsonUtility.Deserialize<JObject>(_rawModelFilePath);
                Assert.NotNull(rawModel["summary"]);
                Assert.Contains("I update a summary", rawModel["summary"].ToString());
                Assert.NotNull(listener.Items.FirstOrDefault(s => s.Message.StartsWith("There is no template processing document type(s): RESTMixedTest")));
                Assert.NotNull(listener.Items.FirstOrDefault(s => s.Message.Contains("Cannot resolve") && s.Message.Contains("invalid.md")));
                lastMessages = messages;
                messages = ClearLog(listener.Items);
                Assert.True(messages.SequenceEqual(lastMessages));
            }
        }

        private List<string> ClearLog(List<ILogItem> items)
        {
            var result = items.Select(i => i.Message).ToList();
            items.Clear();
            return result;
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

            using (var builder = new DocumentBuilder(LoadAssemblies(), ImmutableArray<string>.Empty, null, "obj"))
            {
                builder.Build(parameters);
            }
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
}
