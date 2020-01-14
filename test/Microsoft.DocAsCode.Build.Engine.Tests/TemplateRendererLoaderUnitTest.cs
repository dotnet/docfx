// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine.Tests
{
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.DocAsCode.Tests.Common;

    using Xunit;

    [Trait("Owner", "lianwei")]
    [Trait("EntityType", "TemplateRenderer")]
    [Collection("docfx STA")]
    public class TemplateRendererLoaderUnitTest : TestBase
    {
        private readonly string _inputFolder;

        public TemplateRendererLoaderUnitTest()
        {
            _inputFolder = GetRandomFolder();
        }

        [Fact]
        public void TestLoaderWhenNoFileExists()
        {
            using var listener = new TestListenerScope("NoTemplate");
            var renderers = LoadAllRenderers();
            Assert.Empty(listener.Items);
            Assert.Empty(renderers);

            var file1 = CreateFile("a.js", string.Empty, _inputFolder);
            renderers = LoadAllRenderers();
            Assert.Empty(listener.Items);
            Assert.Empty(renderers);

            // only allows file under root folder
            var file2 = CreateFile("sub/a.tmpl", string.Empty, _inputFolder);
            renderers = LoadAllRenderers();
            Assert.Empty(listener.Items);
            Assert.Empty(renderers);

            var file3 = CreateFile("a.tmpl.js", string.Empty, _inputFolder);
            renderers = LoadAllRenderers();
            Assert.Empty(listener.Items);
            Assert.Empty(renderers);
        }

        [Fact]
        public void TestLoaderWithValidInput()
        {
            var file1 = CreateFile("a.tmpl", "{{name}}", _inputFolder);

            using var listener = new TestListenerScope("TestLoaderWithValidInput");
            var renderers = LoadAllRenderers();

            Assert.Empty(listener.Items);

            Assert.Single(renderers);
            var renderer = renderers[0];
            Assert.NotNull(renderer);

            var model = new { name = "model" };

            var output = renderer.Render(model);
            Assert.Equal("model", output);
        }

        [Fact]
        public void TestSingleFileLoaderWithValidInput()
        {
            var path = "a.tmpl";
            var file1 = CreateFile(path, "{{name}}", _inputFolder);

            using var listener = new TestListenerScope("TestLoaderWithValidInput");
            var renderer = Load(path);

            Assert.Empty(listener.Items);

            Assert.NotNull(renderer);

            var model = new { name = "model" };

            var output = renderer.Render(model);
            Assert.Equal("model", output);
        }


        private List<ITemplateRenderer> LoadAllRenderers()
        {
            var loader = new RendererLoader(new LocalFileResourceReader(_inputFolder), 64);
            return loader.LoadAll().ToList();
        }

        private ITemplateRenderer Load(string path)
        {
            var loader = new RendererLoader(new LocalFileResourceReader(_inputFolder), 64);
            return loader.Load(path);
        }
    }
}
