// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.Tests
{
    using Builders;
    using EntityModel;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Xunit;

    public class TemplateManagerFixture : IDisposable
    {
        public string OutputFolder { get; }

        public TemplateManagerFixture()
        {
            OutputFolder = "TemplateManagerUnitTestOutput";
        }

        public void Dispose()
        {
            if (Directory.Exists(OutputFolder))
            {
               Directory.Delete(OutputFolder, true);
            }
        }
    }

    [Trait("Owner", "lianwei")]
    [Trait("EntityType", "TemplateManager")]
    public class TemplateManagerUnitTest : IClassFixture<TemplateManagerFixture>
    {
        private string _outputFolder;
        public TemplateManagerUnitTest(TemplateManagerFixture fixture)
        {
            _outputFolder = fixture.OutputFolder;
        }

        [Trait("Related", "ResourceFinder")]
        [Fact]
        public void TestResourceFinder_FromAssembly()
        {
            var testFinder = new ResourceFinder(this.GetType().Assembly, "tmpl");

            // 1. Support tmpl1.zip
            using (var result = testFinder.Find("tmpl1"))
            {
                Assert.NotNull(result);
                Assert.Equal(2, result.Names.Count());
                var item = result.GetResource("tmpl1.dot.$");
                Assert.Equal("This is file with complex filename characters", item);

                // backslash is also supported
                item = result.GetResource(@"sub\file1");
                Assert.Equal("This is file inside a subfolder", item);
            }
        }

        [Trait("Related", "ResourceFinder")]
        [Fact]
        public void TestResourceFinder_FromOverrideFolder()
        {
            // If the same resource name exists in the override folder, use the overriden one
            var testFinder = new ResourceFinder(this.GetType().Assembly, "tmpl", "tmpl");

            // 1. Support tmpl1.zip
            using (var result = testFinder.Find("tmpl1"))
            {
                Assert.NotNull(result);
                Assert.Equal(2, result.Names.Count());
                var item = result.GetResource("tmpl1.dot.$");
                Assert.Equal("Override: This is file with complex filename characters", item);

                // backslash is also supported
                item = result.GetResource(@"sub/file1");
                Assert.Equal("Override: This is file inside a subfolder", item);
            }
        }

        [Trait("Related", "TemplateProcessor")]
        [Fact]
        public void TestTemplateProcessor_SingleTemplateWithNoScript()
        {
            // 1. Prepare template
            var templateName = "NoScript";
            string template = @"
{{#model}}
{{name}}
{{/model}}
";
            var model = new
            {
                model = new[]
               {
                   new {name = "test1"},
                   new {name = "test2"},
               }
            };
            var modelFileName = "TestTemplateProcessor_NoScript.yml";
            var item = new ManifestItem { ModelFile = modelFileName, ResourceFile = modelFileName };
            ProcessTemplate(templateName, null, new[] { item }, model, _outputFolder, Tuple.Create($"{templateName}.tmpl", template));

            var outputFile = Path.Combine(_outputFolder, Path.GetFileNameWithoutExtension(modelFileName));
            Assert.True(File.Exists(outputFile));
            Assert.Equal(@"
test1
test2
", File.ReadAllText(outputFile));
        }

        [Trait("Related", "TemplateProcessor")]
        [Fact]
        public void TestTemplateProcessor_NoTemplate()
        {
            var model = new
            {
                model = new List<dynamic>
               {
                   new {name = "test1"},
                   new {name = "test2"},
               }
            };
            var modelFileName = "TestTemplateProcessor_NoTemplate.yml";
            var templateName = "NoTemplate";
            var item = new ManifestItem { ModelFile = modelFileName };
            ProcessTemplate(templateName, null, new[] { item }, model, _outputFolder);
            Assert.True(!File.Exists(Path.Combine(_outputFolder, modelFileName)));
            item = new ManifestItem { ModelFile = modelFileName, ResourceFile = modelFileName };
            ProcessTemplate(templateName, null, new[] { item }, model, _outputFolder);
            Assert.True(File.Exists(Path.Combine(_outputFolder, modelFileName)));
        }

        [Trait("Related", "TemplateProcessor")]
        [Fact]
        public void TestTemplateProcessor_InvalidTemplate()
        {
            var templateName = "InvalidTemplate.html";
            string inputFolder = null;
            var modelFileName = "TestTemplateProcessor_InvalidTemplate.yml";
            var item = new ManifestItem { ModelFile = modelFileName };
            ProcessTemplate(templateName, inputFolder, new[] { item }, new object(), _outputFolder,
                Tuple.Create($"{templateName}.invalidtmpl", string.Empty),
                Tuple.Create($"{templateName}.js", string.Empty),
                Tuple.Create("reference1.html", string.Empty),
                Tuple.Create("reference2.html", string.Empty)
                );
        }

        [Trait("Related", "TemplateProcessor")]
        [Fact]
        public void TestTemplateProcessor_SingleTemplateWithIncludes()
        {
            var templateName = "WithIncludes.html";

            string template = @"
{{ !include('reference1.html') }}
{{ !include('reference2.html') }}
{{#model}}
{{name}}
{{/model}}
";
            string script = @"
function transform(text){
    var model = JSON.parse(text);
    model.model.push({name:'test2'});
    return model;
}";

            var model = new
            {
                model = new List<object>
               {
                   new {name = "test1"},
               }
            };

            var modelFileName = "TestTemplateProcessor_WithIncludes.yml";
            string inputFolder = null;
            var item = new ManifestItem { ModelFile = modelFileName };
            ProcessTemplate(templateName, inputFolder, new[] { item }, model, _outputFolder, 
                Tuple.Create($"{templateName}.tmpl", template), 
                Tuple.Create($"{templateName}.js", script),
                Tuple.Create("reference1.html", string.Empty),
                Tuple.Create("reference2.html", string.Empty)
                );
            var outputFilePath = Path.Combine(_outputFolder, Path.ChangeExtension(modelFileName, "html"));
            Assert.True(File.Exists(outputFilePath));
            Assert.True(File.Exists(Path.Combine(_outputFolder, "reference1.html")));
            Assert.True(File.Exists(Path.Combine(_outputFolder, "reference2.html")));
            Assert.Equal(@"
test1
test2
", File.ReadAllText(outputFilePath));
        }

        [Trait("Related", "TemplateProcessor")]
        [Fact]
        public void TestTemplateProcessor_TemplateFolderWithDifferentType()
        {
            var templateName = "TemplateFolder.html";
            string defaultTemplate = @"
default:
{{#model}}
{{name}}
{{/model}}
";
            string conceptualTemplate = @"
conceptual:
{{#model}}
{{name}}
{{/model}}
";
            string script = @"
function transform(text){
    var model = JSON.parse(text);
    model.model.push({name:'test2'});
    return model;
}";

            var model = new
            {
                model = new List<object>
               {
                   new {name = "test1"},
               }
            };

            string inputFolder = null;
            var item1 = new ManifestItem { ModelFile = "TestTemplateProcessor_TemplateFolderWithDifferentType1.yml", DocumentType = "conceptual" };
            var item2 = new ManifestItem { ModelFile = "TestTemplateProcessor_TemplateFolderWithDifferentType2.yml" };
            ProcessTemplate(templateName, inputFolder, new[] { item1, item2 }, model, _outputFolder,
                Tuple.Create($"{templateName}.tmpl", defaultTemplate),
                Tuple.Create($"{templateName}/conceptual.md.tmpl", conceptualTemplate),
                Tuple.Create($"{templateName}.js", script)
                );
            var outputFilePath1 = Path.Combine(_outputFolder, "TestTemplateProcessor_TemplateFolderWithDifferentType1.md");
            Assert.True(File.Exists(outputFilePath1));
            Assert.Equal(@"
conceptual:
test1
test2
", File.ReadAllText(outputFilePath1));
            var outputFilePath2 = Path.Combine(_outputFolder, "TestTemplateProcessor_TemplateFolderWithDifferentType2.html");
            Assert.True(File.Exists(outputFilePath2));
            Assert.Equal(@"
default:
test1
test2
", File.ReadAllText(outputFilePath2));
        }

        [Trait("Related", "GenerateDefaultToc")]
        [Fact]
        public void TestGenerateDefaultToc()
        {
            TemplateManager.GenerateDefaultToc(null, null, null, true);
            Assert.True(File.Exists(TemplateManager.DefaultTocEntry));
            var content = File.ReadAllText(TemplateManager.DefaultTocEntry);
            Assert.True(string.IsNullOrEmpty(content));
        }

        private static void ProcessTemplate(string templateName, string inputFolder, IEnumerable<ManifestItem> items, object model, string outputFolder, params Tuple<string, string>[] templateFiles)
        {
            var rootTemplateFolder = "tmpl";
            var templateFolder = Path.Combine(rootTemplateFolder, templateName);
            if (Directory.Exists(templateFolder))
                Directory.Delete(templateFolder, true);
            WriteTemplate(templateFolder, templateFiles);
            foreach(var item in items)
            {
                var modelPath = Path.Combine(inputFolder ?? string.Empty, item.ModelFile);
                WriteModel(modelPath, model);
            }

            using (var resource = new ResourceFinder(rootTemplateFolder).Find(templateName))
            {
                var processor = new TemplateProcessor(templateName, resource);
                var context = new DocumentBuildContext(inputFolder);
                context.Manifest.AddRange(items);
                processor.Process(context, outputFolder);
            }
        }

        private static void WriteTemplate(string cwd, params Tuple<string, string>[] files)
        {
            foreach (var file in files)
            {
                var filePath = Path.Combine(cwd ?? string.Empty, file.Item1);
                var directory = Path.GetDirectoryName(filePath);

                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);
                File.WriteAllText(filePath, file.Item2);
            }
        }

        private static void WriteModel(string path, object model)
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            YamlUtility.Serialize(path, model);
        }
    }
}
