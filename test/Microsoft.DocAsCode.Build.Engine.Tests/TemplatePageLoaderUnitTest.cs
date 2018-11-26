// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine.Tests
{
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.DocAsCode.Tests.Common;

    using Xunit;

    [Trait("Owner", "lianwei")]
    [Trait("EntityType", "TemplatePage")]
    [Collection("docfx STA")]
    public class TemplatePageLoaderUnitTest : TestBase
    {
        private readonly string _inputFolder;
        private readonly string _outputFolder;

        public TemplatePageLoaderUnitTest()
        {
            _inputFolder = GetRandomFolder();
            _outputFolder = GetRandomFolder();
        }

        [Fact]
        public void TestLoaderWhenNoFileExists()
        {
            using (var listener = new TestListenerScope("NoTemplate"))
            {
                var templates = LoadAllTemplates();
                Assert.Equal(0, listener.Items.Count);
                Assert.Equal(0, templates.Count);

                var file1 = CreateFile("a.js", string.Empty, _inputFolder);
                templates = LoadAllTemplates();
                Assert.Equal(0, listener.Items.Count);
                Assert.Equal(0, templates.Count);

                // only allowes file under root folder
                var file2 = CreateFile("sub/a.tmpl", string.Empty, _inputFolder);
                templates = LoadAllTemplates();
                Assert.Equal(0, listener.Items.Count);
                Assert.Equal(0, templates.Count);

                var file3 = CreateFile("a.tmpl.js", string.Empty, _inputFolder);
                templates = LoadAllTemplates();
                Assert.Equal(0, listener.Items.Count);
                Assert.Equal(0, templates.Count);
            }
        }

        [Fact]
        public void TestLoaderWhenRendererExists()
        {
            var file1 = CreateFile("a.tmpl", string.Empty, _inputFolder);

            using (var listener = new TestListenerScope("TestLoaderWhenRendererExists"))
            {
                var templates = LoadAllTemplates();

                Assert.Equal(0, listener.Items.Count);

                Assert.Equal(1, templates.Count);
                var template = templates[0];
                Assert.NotNull(template.Renderer);
                Assert.Equal(TemplateType.Default, template.TemplateType);
                Assert.Equal("a", template.Name);
                Assert.Equal("a", template.Type);
                Assert.Equal(string.Empty, template.Extension);
                Assert.Null(template.Preprocessor);
                Assert.False(template.ContainsGetOptions);
                Assert.False(template.ContainsModelTransformation);
            }
        }

        [Fact]
        public void TestLoaderWhenPreprocessorExists()
        {
            var file1 = CreateFile("a.primary.tmpl", string.Empty, _inputFolder);
            var file2 = CreateFile("a.primary.js", "exports.transform = function(){}", _inputFolder);

            using (var listener = new TestListenerScope("TestLoaderWhenPreprocessorExists"))
            {
                var templates = LoadAllTemplates();

                Assert.Equal(0, listener.Items.Count);

                Assert.Equal(1, templates.Count);
                var template = templates[0];
                Assert.NotNull(template.Renderer);
                Assert.Equal(TemplateType.Primary, template.TemplateType);
                Assert.Equal("a.primary", template.Name);
                Assert.Equal("a", template.Type);
                Assert.Equal(string.Empty, template.Extension);
                Assert.NotNull(template.Preprocessor);
                Assert.False(template.ContainsGetOptions);
                Assert.True(template.ContainsModelTransformation);

                var output = template.TransformModel(new { a = 1 });
                Assert.Null(output);
            }
        }

        [Fact]
        public void TestLoaderWhenStandalonePreprocessorExists()
        {
            var file1 = CreateFile("a.ext.TMPL.js", "exports.transform = function(){}", _inputFolder);

            using (var listener = new TestListenerScope("TestLoaderWhenStandalonePreprocessorExists"))
            {
                var templates = LoadAllTemplates();

                Assert.Equal(0, listener.Items.Count);

                Assert.Equal(1, templates.Count);
                var template = templates[0];
                Assert.Null(template.Renderer);
                Assert.Equal(TemplateType.Default, template.TemplateType);
                Assert.Equal("a.ext", template.Name);
                Assert.Equal("a", template.Type);
                Assert.Equal(".ext", template.Extension);
                Assert.NotNull(template.Preprocessor);
                Assert.False(template.ContainsGetOptions);
                Assert.True(template.ContainsModelTransformation);

                var output = template.TransformModel(new { a = 1 });
                Assert.Null(output);
            }
        }

        private List<Template> LoadAllTemplates()
        {
            var loader = new TemplatePageLoader(new LocalFileResourceReader(_inputFolder), null, 64);
            return loader.LoadAll().ToList();
        }
    }
}
