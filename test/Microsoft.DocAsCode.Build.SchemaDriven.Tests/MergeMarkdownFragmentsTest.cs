namespace Microsoft.DocAsCode.Build.SchemaDriven.Tests
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Composition;
    using System.IO;

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

        private const string RawModelFileExtension = ".raw.json";

        public MergeMarkdownFragmentsTest()
        {
            _outputFolder = GetRandomFolder();
            _inputFolder = GetRandomFolder();
            _templateFolder = GetRandomFolder();
            _defaultFiles = new FileCollection(Directory.GetCurrentDirectory());
            _applyTemplateSettings = new ApplyTemplateSettings(_inputFolder, _outputFolder) { RawModelExportSettings = { Export = true }, TransformDocument = true, };

            _templateManager = new TemplateManager(null, null, new List<string> { "template" }, null, _templateFolder);
        }

        [Fact]
        void TestMergeMarkdownFragments()
        {
            // Arrange
            var schemaFile = CreateFile("template/schemas/rest.mixed.schema.json", File.ReadAllText("TestData/schemas/rest.mixed.schema.json"), _templateFolder);
            var yamlFile = CreateFile("Suppressions.yml", File.ReadAllText("TestData/inputs/Suppressions.yml"), _inputFolder);
            var mdFile = CreateFile("Suppressions.yml.md", File.ReadAllText("TestData/inputs/Suppressions.yml.md"), _inputFolder);
            FileCollection files = new FileCollection(_defaultFiles);
            files.Add(DocumentType.Article, new[] { yamlFile }, _inputFolder);

            // Act
            BuildDocument(files);

            // Assert
            var rawModelFilePath = GetRawModelFilePath("Suppressions.yml");
            Assert.True(File.Exists(rawModelFilePath));
            var rawModel = JsonUtility.Deserialize<JObject>(rawModelFilePath);
            Assert.Equal("bob", rawModel["author"]);
            Assert.Contains("Enables the snoozed or dismissed attribute", rawModel["operations"][0]["summary"].ToString());
            Assert.Contains("Some empty lines between H2 and this paragraph is tolerant", rawModel["definitions"][0]["properties"][0]["description"].ToString());
            Assert.Contains("This is a summary at YAML's", rawModel["summary"].ToString());
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

        // TODO: remove this class when ApplyOverwriteFragments supports incremental and exports to all schemas
        [Export("SchemaDrivenDocumentProcessor.RESTMixedTest", typeof(IDocumentBuildStep))]
        public class ApplyOverwriteFragmentsForTest : ApplyOverwriteFragments
        {
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