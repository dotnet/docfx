// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Tests.Common;

    using Xunit;

    [Trait("Owner", "lianwei")]
    [Trait("EntityType", "TemplateManager")]
    [Collection("docfx STA")]
    public class TemplateManagerUnitTest : TestBase
    {
        private readonly string _inputFolder;
        private readonly string _outputFolder;

        public TemplateManagerUnitTest()
        {
            _inputFolder = GetRandomFolder();
            _outputFolder = GetRandomFolder();
        }

        [Trait("Related", "ResourceFinder")]
        [Fact]
        public void TestResourceFinderFromAssembly()
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
        public void TestTemplateManagerWithMutipleThemesShouldWork()
        {
            // If the same resource name exists in the override folder, use the overriden one
            var themes = new List<string> { "tmpl1", "tmpl/tmpl1" };
            var manager = new TemplateManager(GetType().Assembly, "tmpl", null, themes, null);
            var outputFolder = Path.Combine(_outputFolder, "TestTemplateManager_MutipleThemes");
            manager.ProcessTheme(outputFolder, true);
            // 1. Support tmpl1.zip
            var file1 = Path.Combine(outputFolder, "tmpl1.dot.$");
            Assert.True(File.Exists(file1));
            Assert.Equal("Override: This is file with complex filename characters", File.ReadAllText(file1));

            // backslash is also supported
            var file2 = Path.Combine(outputFolder, "sub/file1");
            Assert.True(File.Exists(file2));
            Assert.Equal("Override: This is file inside a subfolder", File.ReadAllText(file2));
        }

        #region Mustache template processor test

        [Trait("Related", "TemplateProcessor")]
        [Trait("Related", "Mustache")]
        [Fact]
        public void TestMustacheTemplateProcessSingleTemplateWithNoScriptShouldWork()
        {
            // 1. Prepare template
            var templateName = "NoScript";
            string template = @"
{{#model}}
name1={{name1}},
name2={{name2}};
{{/model}}
";
            var model = new
            {
                model = new object[]
               {
                   new {name1 = "test1"},
                   new {name2 = "test2"},
               }
            };
            var modelFileName = Path.Combine(_inputFolder, "TestTemplateProcessor_NoScript.yml");
            var item = new InternalManifestItem
            {
                DocumentType = string.Empty,
                Key = modelFileName,
                FileWithoutExtension = Path.ChangeExtension(modelFileName, null),
                ResourceFile = modelFileName,
                LocalPathFromRoot = modelFileName,
            };
            ProcessTemplate(templateName, null, new[] { item }, model, _outputFolder, Tuple.Create("default.tmpl", template));

            var outputFile = Path.Combine(_outputFolder, Path.ChangeExtension(modelFileName, string.Empty));
            Assert.True(File.Exists(outputFile));
            AssertEqualIgnoreCrlf(@"
name1=test1,
name2=;
name1=,
name2=test2;
", File.ReadAllText(outputFile));
        }

        [Trait("Related", "TemplateProcessor")]
        [Trait("Related", "Mustache")]
        [Fact]
        public void TestMustacheTemplateProcessSingleTemplateWithNoScriptWithPartialShouldWork()
        {
            // 1. Prepare template
            var templateName = "Subfolder/NoScriptWithPartial";
            string template = @"
{{#model}}
{{name}}
{{/model}}
{{>partial1}}
";
            string partial1 = @"partial 1:
{{>partial2}}";
            string partial2 = @"partial 2:
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
            var modelFileName = Path.Combine(_inputFolder, "TestTemplateProcessor_NoScriptWithPartial.yml");
            var item = new InternalManifestItem
            {
                DocumentType = string.Empty,
                Key = modelFileName,
                FileWithoutExtension = Path.ChangeExtension(modelFileName, null),
                ResourceFile = modelFileName,
                LocalPathFromRoot = modelFileName,
            };
            ProcessTemplate(
                templateName,
                null,
                new[] { item },
                model,
                _outputFolder,
                Tuple.Create("default.tmpl", template),
                Tuple.Create("partial1.tmpl.partial", partial1),
                Tuple.Create("partial2.tmpl.partial", partial2));

            var outputFile = Path.Combine(_outputFolder, Path.ChangeExtension(modelFileName, string.Empty));
            Assert.True(File.Exists(outputFile));
            AssertEqualIgnoreCrlf(@"
test1
test2
partial 1:
partial 2:
test1
test2
", File.ReadAllText(outputFile));
        }

        [Trait("Related", "TemplateProcessor")]
        [Trait("Related", "Mustache")]
        [Fact]
        public void TestMustacheTemplateWithMasterPageShouldWork()
        {
            // 1. Prepare template
            var templateName = "Subfolder/WithMasterPage";
            string template = @"
{{!master('_layout/master.html')}}
{{!master(' _layout/invalid1.html ')}}
{{#model}}
{{name}}
{{/model}}
{{>partial1}}
{{!master( _layout/invalid2.html )}}
";
            string partial1 = @"partial 1:
{{>partial2}}";
            string partial2 = @"partial 2:
{{#model}}
{{name}}
{{/model}}
";

            string master = @"
{{ !include('reference1.html') }}
Hello Master
{{!body}}
Hello Body
{{!body}}
";
            var model = new
            {
                model = new[]
               {
                   new {name = "test1"},
                   new {name = "test2"},
               }
            };
            var modelFileName = Path.Combine(_inputFolder, "TestTemplateProcessor_WithMasterPage.yml");
            var item = new InternalManifestItem
            {
                DocumentType = string.Empty,
                Key = modelFileName,
                FileWithoutExtension = Path.ChangeExtension(modelFileName, null),
                ResourceFile = modelFileName,
                LocalPathFromRoot = modelFileName,
            };
            ProcessTemplate(
                templateName,
                null,
                new[] { item },
                model,
                _outputFolder,
                Tuple.Create("default.tmpl", template),
                Tuple.Create("_layout/master.html", master),
                Tuple.Create("reference1.html", string.Empty),
                Tuple.Create("partial1.tmpl.partial", partial1),
                Tuple.Create("partial2.tmpl.partial", partial2));

            var outputFile = Path.Combine(_outputFolder, Path.ChangeExtension(modelFileName, string.Empty));
            Assert.True(File.Exists(Path.Combine(_outputFolder, "reference1.html")));
            Assert.True(File.Exists(outputFile));
            AssertEqualIgnoreCrlf(@"
Hello Master

test1
test2
partial 1:
partial 2:
test1
test2
Hello Body

test1
test2
partial 1:
partial 2:
test1
test2
", File.ReadAllText(outputFile));
        }

        [Trait("Related", "TemplateProcessor")]
        [Trait("Related", "Mustache")]
        [Fact]
        public void TestMustacheTemplateProcessInvalidTemplateShouldFail()
        {
            var templateName = "InvalidTemplate.html";
            string inputFolder = null;
            var modelFileName = Path.Combine(_inputFolder, "TestTemplateProcessor_InvalidTemplate.yml");
            var item = new InternalManifestItem
            {
                FileWithoutExtension = Path.ChangeExtension(modelFileName, null),
                DocumentType = string.Empty,
                Key = modelFileName,
                LocalPathFromRoot = modelFileName,
            };
            ProcessTemplate(templateName, inputFolder, new[] { item }, new object(), _outputFolder,
                Tuple.Create("default.invalidtmpl", string.Empty),
                Tuple.Create("default.js", string.Empty),
                Tuple.Create("reference1.html", string.Empty),
                Tuple.Create("reference2.html", string.Empty)
                );
        }

        [Trait("Related", "TemplateProcessor")]
        [Trait("Related", "Mustache")]
        [Fact]
        public void TestMustacheTemplateProcessSingleTemplateWithIncludesShouldWork()
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
exports.transform = function (model){
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

            var modelFileName = Path.Combine(_inputFolder, "TestTemplateProcessor_WithIncludes.yml");
            string inputFolder = null;
            var item = new InternalManifestItem
            {
                FileWithoutExtension = Path.ChangeExtension(modelFileName, null),
                DocumentType = string.Empty,
                Key = modelFileName,
                LocalPathFromRoot = modelFileName,
            };
            ProcessTemplate(templateName, inputFolder, new[] { item }, model, _outputFolder,
                Tuple.Create("default.html.tmpl", template),
                Tuple.Create("default.html.js", script),
                Tuple.Create("reference1.html", string.Empty),
                Tuple.Create("reference2.html", string.Empty)
                );
            var outputFilePath = Path.Combine(_outputFolder, Path.ChangeExtension(modelFileName, "html"));
            Assert.True(File.Exists(outputFilePath));
            Assert.True(File.Exists(Path.Combine(_outputFolder, "reference1.html")));
            Assert.True(File.Exists(Path.Combine(_outputFolder, "reference2.html")));
            AssertEqualIgnoreCrlf(@"
test1
test2
", File.ReadAllText(outputFilePath));
        }

        [Trait("Related", "TemplateProcessor")]
        [Fact]
        public void TestMustacheTemplateProcessSingleTemplateWithRequireScriptShouldWork()
        {
            var templateName = "WithRequireScript.html";

            string template = @"
{{#model}}
{{#result1}}result1 = true{{/result1}}
{{#result2}}result2 = true{{/result2}}
{{#result3}}result3 = true{{/result3}}
{{/model}}
";
            string mainScript = @"
var util = require('./util.js');

exports.transform = function (model){
    var url = 'https://www.microsoft.com';
    model.model.result1 = util.isAbsolutePath(url);
    model.model.result2 = util.isAbsolutePath(url);
    model.model.result3 = util.isAbsolutePath(url);
    return model;
}";
            string utilScript = @"
exports.isAbsolutePath = isAbsolutePath;

function isAbsolutePath(path) {
    return /^(\w+:)?\/\//g.test(path);
}
";

            var model = new
            {
                model = new Dictionary<string, object>()
            };

            var modelFileName = Path.Combine(_inputFolder, "TestTemplateProcessor_WithRequireScript.yml");
            string inputFolder = null;
            var item = new InternalManifestItem
            {
                FileWithoutExtension = Path.ChangeExtension(modelFileName, null),
                DocumentType = string.Empty,
                Key = modelFileName,
                LocalPathFromRoot = modelFileName,
            };
            ProcessTemplate(templateName, inputFolder, new[] { item }, model, _outputFolder,
                Tuple.Create("default.html.tmpl", template),
                Tuple.Create("default.html.js", mainScript),
                Tuple.Create("util.js", utilScript)
                );
            var outputFilePath = Path.Combine(_outputFolder, Path.ChangeExtension(modelFileName, "html"));
            Assert.True(File.Exists(outputFilePath));
            AssertEqualIgnoreCrlf(@"
result1 = true
result2 = true
result3 = true
", File.ReadAllText(outputFilePath));
        }

        [Trait("Related", "TemplateProcessor")]
        [Trait("Related", "Mustache")]
        [Fact]
        public void TestMustacheTemplateProcessTemplateFolderWithDifferentTypeShouldWork()
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
exports.transform = function (model){
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
            var item1 = new InternalManifestItem
            {
                FileWithoutExtension = "TestTemplateProcessor_TemplateFolderWithDifferentType1",
                Key = "x.yml",
                DocumentType = "Conceptual",
                LocalPathFromRoot = "TestTemplateProcessor_TemplateFolderWithDifferentType1.md",
            };
            var item2 = new InternalManifestItem
            {
                DocumentType = string.Empty,
                FileWithoutExtension = "TestTemplateProcessor_TemplateFolderWithDifferentType2",
                Key = "y.yml",
                LocalPathFromRoot = "TestTemplateProcessor_TemplateFolderWithDifferentType2.md",
            };
            ProcessTemplate(templateName, inputFolder, new[] { item1, item2 }, model, _outputFolder,
                Tuple.Create("default.html.tmpl", defaultTemplate),
                Tuple.Create("conceptual.md.tmpl", conceptualTemplate),
                Tuple.Create("default.html.js", script),
                Tuple.Create("conceptual.md.js", script)
                );
            var outputFilePath1 = Path.Combine(_outputFolder, "TestTemplateProcessor_TemplateFolderWithDifferentType1.md");
            Assert.True(File.Exists(outputFilePath1));
            AssertEqualIgnoreCrlf(@"
conceptual:
test1
test2
", File.ReadAllText(outputFilePath1));
            var outputFilePath2 = Path.Combine(_outputFolder, "TestTemplateProcessor_TemplateFolderWithDifferentType2.html");
            Assert.True(File.Exists(outputFilePath2));
            AssertEqualIgnoreCrlf(@"
default:
test1
test2
", File.ReadAllText(outputFilePath2));
        }

        [Trait("Related", "TemplateProcessor")]
        [Trait("Related", "Mustache")]
        [Fact(Skip = "Disable as downgrading jint to 2.5.0 to fix toc external link issue")]
        public void TestMustacheTemplateWithScriptWithLongStringInModelShouldWork()
        {
            var templateName = "TemplateFolder.html";
            string defaultTemplate = @"{{name}}";
            var name = "this is a looooooooooooooooooooooooooooooooooooog name";
            var longName = string.Concat(Enumerable.Repeat(name, 20000));
            string script = @"
exports.transform = function (model){
    return {
        name: JSON.stringify(model)
    };
}";

            var model = new
            {
                model = new 
               {
                   name = longName,
               }
            };

            string inputFolder = null;
            var item1 = new InternalManifestItem
            {
                FileWithoutExtension = "TestMustacheTemplateWithScriptWithLongStringInModelShouldWork",
                Key = "x.yml",
                DocumentType = "Conceptual",
                LocalPathFromRoot = "TestMustacheTemplateWithScriptWithLongStringInModelShouldWork.md",
            };
            ProcessTemplate(templateName, inputFolder, new[] { item1 }, model, _outputFolder,
                Tuple.Create("default.html.tmpl", defaultTemplate),
                Tuple.Create("default.html.js", script)
                );
            var outputFilePath1 = Path.Combine(_outputFolder, "TestMustacheTemplateWithScriptWithLongStringInModelShouldWork.html");
            Assert.True(File.Exists(outputFilePath1));
            Assert.Equal($"{{&quot;model&quot;:{{&quot;name&quot;:&quot;{longName}&quot;}},&quot;__global&quot;:{{}}}}", File.ReadAllText(outputFilePath1));
        }

        #endregion

        #region Liquid template processor test

        [Trait("Related", "TemplateProcessor")]
        [Trait("Related", "Liquid")]
        [Fact]
        public void TestLiquidTemplateProcessSingleTemplateWithNoScriptShouldWork()
        {
            // 1. Prepare template
            var templateName = "NoScript.liquid";
            string template = @"
{% for item in model -%}
{{ item.name }}
{% endfor -%}
";
            var model = new
            {
                model = new[]
               {
                   new {name = "test1"},
                   new {name = "test2"},
               }
            };
            var modelFileName = Path.Combine(_inputFolder, "TestLiquidTemplateProcessor_NoScript.yml");
            var item = new InternalManifestItem
            {
                DocumentType = string.Empty,
                Key = modelFileName,
                FileWithoutExtension = Path.ChangeExtension(modelFileName, null),
                ResourceFile = modelFileName,
                LocalPathFromRoot = modelFileName,
            };
            ProcessTemplate(templateName, null, new[] { item }, model, _outputFolder, Tuple.Create("default.liquid", template));

            var outputFile = Path.Combine(_outputFolder, Path.ChangeExtension(modelFileName, string.Empty));
            Assert.True(File.Exists(outputFile));
            AssertEqualIgnoreCrlf(@"
test1
test2
", File.ReadAllText(outputFile));
        }

        [Trait("Related", "TemplateProcessor")]
        [Trait("Related", "Liquid")]
        [Fact]
        public void TestLiquidTemplateProcessSingleTemplateWithNoScriptWithIncludeShouldWork()
        {
            // 1. Prepare template
            var templateName = "NoScriptWithInclude.liquid";
            string template = @"
{% for item in model -%}
{{ item.name }}
{% endfor -%}
{% include partial1 -%}
";
            string partial1 = @"partial 1:
{% include partial2 -%}";
            string partial2 = @"partial 2:
{% for item in model -%}
{{ item.name }}
{% endfor -%}
";
            var model = new
            {
                model = new[]
               {
                   new {name = "test1"},
                   new {name = "test2"},
               }
            };
            var modelFileName = Path.Combine(_inputFolder, "TestLiquidTemplateProcessor_NoScriptWithPartial.yml");
            var item = new InternalManifestItem
            {
                DocumentType = string.Empty,
                Key = modelFileName,
                FileWithoutExtension = Path.ChangeExtension(modelFileName, null),
                ResourceFile = modelFileName,
                LocalPathFromRoot = modelFileName,
            };
            ProcessTemplate(
                templateName,
                null,
                new[] { item },
                model,
                _outputFolder,
                Tuple.Create("default.liquid", template),
                Tuple.Create("_partial1.liquid", partial1),
                Tuple.Create("_partial2.liquid", partial2));

            var outputFile = Path.Combine(_outputFolder, Path.ChangeExtension(modelFileName, string.Empty));
            Assert.True(File.Exists(outputFile));
            AssertEqualIgnoreCrlf(@"
test1
test2
partial 1:
partial 2:
test1
test2
", File.ReadAllText(outputFile));
        }

        [Trait("Related", "TemplateProcessor")]
        [Trait("Related", "Liquid")]
        [Fact]
        public void TestLiquidTemplateProcessSingleTemplateWithDependenciesShouldWork()
        {
            var templateName = "WithIncludes.liquid";

            string template = @"
{% ref reference1.html -%}
{% ref reference2.html -%}
{% for item in model -%}
{{ item.name }}
{% endfor -%}
";
            string script = @"
exports.transform = function (model){
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

            var modelFileName = Path.Combine(_inputFolder, "TestLiquidTemplateProcessor_WithIncludes.yml");
            string inputFolder = null;
            var item = new InternalManifestItem
            {
                FileWithoutExtension = Path.ChangeExtension(modelFileName, null),
                DocumentType = string.Empty,
                Key = modelFileName,
                LocalPathFromRoot = modelFileName,
            };
            ProcessTemplate(templateName, inputFolder, new[] { item }, model, _outputFolder,
                Tuple.Create("default.html.liquid", template),
                Tuple.Create("default.html.js", script),
                Tuple.Create("reference1.html", string.Empty),
                Tuple.Create("reference2.html", string.Empty)
                );
            var outputFilePath = Path.Combine(_outputFolder, Path.ChangeExtension(modelFileName, "html"));
            Assert.True(File.Exists(outputFilePath));
            Assert.True(File.Exists(Path.Combine(_outputFolder, "reference1.html")));
            Assert.True(File.Exists(Path.Combine(_outputFolder, "reference2.html")));
            AssertEqualIgnoreCrlf(@"
test1
test2
", File.ReadAllText(outputFilePath));
        }

        [Trait("Related", "TemplateProcessor")]
        [Trait("Related", "Liquid")]
        [Fact]
        public void TestLiquidTemplateWithMasterPageShouldWork()
        {
            // 1. Prepare template
            var templateName = "Subfolder/LiquidWithMasterPage";
            string template = @"
{% master _layout/master.html -%}
{% master _layout/master.html -%}
{% for item in model -%}
{{ item.name }}
{% endfor -%}
{% include partial1 -%}
";
            string partial1 = @"partial 1:
{% include partial2 -%}";
            string partial2 = @"partial 2:
{% for item in model -%}
{{ item.name }}
{% endfor -%}
";

            string master = @"
{% ref reference1.html -%}
Hello Master
{% include partial1 -%}
{%- body %}
";
            var model = new
            {
                model = new[]
               {
                   new {name = "test1"},
                   new {name = "test2"},
               }
            };
            var modelFileName = Path.Combine(_inputFolder, "TestTemplateProcessor_LiquidWithMasterPage.yml");
            var item = new InternalManifestItem
            {
                DocumentType = string.Empty,
                Key = modelFileName,
                FileWithoutExtension = Path.ChangeExtension(modelFileName, null),
                ResourceFile = modelFileName,
                LocalPathFromRoot = modelFileName,
            };
            ProcessTemplate(
                templateName,
                null,
                new[] { item },
                model,
                _outputFolder,
                Tuple.Create("default.liquid", template),
                Tuple.Create("_layout/master.html", master),
                Tuple.Create("reference1.html", string.Empty),
                Tuple.Create("_partial1.liquid", partial1),
                Tuple.Create("_partial2.liquid", partial2));

            var outputFile = Path.Combine(_outputFolder, Path.ChangeExtension(modelFileName, string.Empty));
            Assert.True(File.Exists(Path.Combine(_outputFolder, "reference1.html")));
            Assert.True(File.Exists(outputFile));
            AssertEqualIgnoreCrlf(@"
Hello Master
partial 1:
partial 2:
test1
test2

test1
test2
partial 1:
partial 2:
test1
test2
", File.ReadAllText(outputFile));
        }

        [Trait("Related", "TemplateProcessor")]
        [Trait("Related", "Liquid")]
        [Fact]
        public void TestLiquidTemplateProcessTemplateFolderWithDifferentTypeShouldWork()
        {
            var templateName = "TemplateFolder.liquid";
            string defaultTemplate = @"
default:
{% for item in model -%}
{{ item.name }}
{% endfor -%}
";
            string conceptualTemplate = @"
conceptual:
{% for item in model -%}
{{ item.name }}
{% endfor -%}
";
            string script = @"
exports.transform = function (model){
    model.model.push({name:'test2'});
    return model;
}";

            var model = new
            {
                model = new List<object>
               {
                   new {name = "test1"},
               },
                another = new Dictionary<string, object>
                {
                    ["key1"] = new { name = "test3" },
                    ["key2"] = new { name = "test4" }
                }
            };

            string inputFolder = null;
            var item1 = new InternalManifestItem
            {
                FileWithoutExtension = "TestLiquidTemplateProcessor_TemplateFolderWithDifferentType1",
                Key = "x.yml",
                DocumentType = "Conceptual",
                LocalPathFromRoot = "TestLiquidTemplateProcessor_TemplateFolderWithDifferentType1.md",
            };
            var item2 = new InternalManifestItem
            {
                DocumentType = string.Empty,
                FileWithoutExtension = "TestLiquidTemplateProcessor_TemplateFolderWithDifferentType2",
                Key = "y.yml",
                LocalPathFromRoot = "TestLiquidTemplateProcessor_TemplateFolderWithDifferentType2.md",
            };
            ProcessTemplate(templateName, inputFolder, new[] { item1, item2 }, model, _outputFolder,
                Tuple.Create("default.html.liquid", defaultTemplate),
                Tuple.Create("conceptual.md.liquid", conceptualTemplate),
                Tuple.Create("default.html.js", script),
                Tuple.Create("conceptual.md.js", script)
                );
            var outputFilePath1 = Path.Combine(_outputFolder, "TestLiquidTemplateProcessor_TemplateFolderWithDifferentType1.md");
            Assert.True(File.Exists(outputFilePath1));
            AssertEqualIgnoreCrlf(@"
conceptual:
test1
test2
", File.ReadAllText(outputFilePath1));
            var outputFilePath2 = Path.Combine(_outputFolder, "TestLiquidTemplateProcessor_TemplateFolderWithDifferentType2.html");
            Assert.True(File.Exists(outputFilePath2));
            AssertEqualIgnoreCrlf(@"
default:
test1
test2
", File.ReadAllText(outputFilePath2));
        }

        #endregion

        private static void ProcessTemplate(string templateName, string inputFolder, IEnumerable<InternalManifestItem> items, object model, string outputFolder, params Tuple<string, string>[] templateFiles)
        {
            var rootTemplateFolder = "tmpl";
            var templateFolder = Path.Combine(rootTemplateFolder, templateName);
            if (Directory.Exists(templateFolder))
                Directory.Delete(templateFolder, true);
            WriteTemplate(templateFolder, templateFiles);
            using (var resource = new ResourceFinder(null, null).Find(templateFolder))
            {
                var context = new DocumentBuildContext(inputFolder);
                var processor = new TemplateProcessor(resource, context, 4);
                foreach (var item in items)
                {
                    if (item.ResourceFile != null)
                    {
                        var dir = Path.GetDirectoryName(item.ResourceFile);
                        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                        File.Create(item.ResourceFile).Dispose();
                    }
                    if (string.IsNullOrEmpty(item.InputFolder)) item.InputFolder = Directory.GetCurrentDirectory();
                    item.Model = new ModelWithCache(model);
                }
                var settings = new ApplyTemplateSettings(inputFolder, outputFolder);
                EnvironmentContext.SetBaseDirectory(inputFolder);
                EnvironmentContext.SetOutputDirectory(outputFolder);
                try
                {
                    processor.Process(items.ToList(), settings);
                }
                finally
                {
                    EnvironmentContext.Clean();
                }
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
            JsonUtility.Serialize(path, model);
        }

        private static void AssertEqualIgnoreCrlf(string expected, string actual)
        {
            Assert.Equal(expected.Replace("\r\n", "\n"), actual.Replace("\r\n", "\n"));
        }
    }
}
