// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.Tests
{
    using System.Collections.Generic;
    using System.Linq;
    using EntityModel;
    using Xunit;
    using System;
    using System.IO;
    using System.Collections;

    [Trait("Owner", "lianwei")]
    [Trait("EntityType", "TemplateManager")]
    public class TemplateManagerUnitTest
    {
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
        public void TestTemplateProcessor_NoScript()
        {
            var model = new
            {
                model = new[]
               {
                   new {name = "test1"},
                   new {name = "test2"},
               }
            };

            string template = @"
{{#model}}
{{name}}
{{/model}}
";

            var outputPath = "TestTemplateProcessor_NoScript.txt";
            File.Delete(outputPath);
            var processor = new TemplateProcessor(template, null, ".txt", null);
            processor.Process(new[] { new TemplateModelInfo(outputPath, model) }, null);
            Assert.True(File.Exists(outputPath));
            var content = File.ReadAllText(outputPath);
            Assert.Equal(@"
test1
test2
", content);
        }

        [Trait("Related", "TemplateProcessor")]
        [Fact]
        public void TestTemplateProcessor_Script()
        {
            var model = new
            {
                model = new List<dynamic>
               {
                   new {name = "test1"},
                   new {name = "test2"},
               }
            };

            string template = @"
{{#model}}
{{name}}
{{/model}}
";
            string script = @"
function transform(text){
    var model = JSON.parse(text);
    return innerTransform(model);
    function innerTransform(model){
        model.model.push({name:'test3'});
        return model;
    }
}
";
            var outputPath = "TestTemplateProcessor_Script.txt";
            File.Delete(outputPath);
            var processor = new TemplateProcessor(template, script, ".txt", null);
            processor.Process(new[] { new TemplateModelInfo(outputPath, model) }, null);
            Assert.True(File.Exists(outputPath));
            var content = File.ReadAllText(outputPath);
            Assert.Equal(@"
test1
test2
test3
", content);
        }

        [Trait("Related", "TemplateProcessor")]
        [Fact]
        public void TestTemplateProcessor_WithIncludes()
        {
            var model = new
            {
                model = new List<dynamic>
               {
                   new {name = "test1"},
               }
            };

            string template = @"
{{ !include('tmpl1.dot.$') }}
{{#model}}
{{name}}
{{/model}}
";
            string script = @"
function transform(text){
    var model = JSON.parse(text);
    return model;
}
";
            var outputPath = "TestTemplateProcessor_WithIncludes.txt";
            File.Delete(outputPath);
            using (var resource = new ResourceFinder(this.GetType().Assembly, "tmpl").Find("tmpl1"))
            {
                var processor = new TemplateProcessor(template, script, "txt", resource);
                processor.Process(new[] { new TemplateModelInfo(outputPath, model) }, null);
                Assert.True(File.Exists(outputPath));
                var content = File.ReadAllText(outputPath);
                Assert.Equal(@"
test1
", content);
                Assert.True(File.Exists("tmpl1.dot.$"));
            }
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
    }
}
