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
    [Trait("EntityType", "TemplateProcessor")]
    [Collection("docfx STA")]
    public class TemplateProcessorUnitTest : TestBase
    {
        private readonly string _inputFolder;
        private readonly string _outputFolder;
        private readonly string _templateFolder;

        public TemplateProcessorUnitTest()
        {
            _inputFolder = GetRandomFolder();
            _outputFolder = GetRandomFolder();
            _templateFolder = GetRandomFolder();
        }

        [Fact]
        public void TestXrefWithTemplate()
        {
            var xrefTmpl = CreateFile("partials/xref.html.tmpl", @"<h2>{{uid}}</h2><p>{{summary}}</p>", _templateFolder);
            var tmpl = CreateFile("index.html.tmpl", @"
<xref uid=""{{reference}}"" template=""partials/xref.html.tmpl"" />
", _templateFolder);

            var xref = new XRefSpec
            {
                Uid = "reference",
                Href = "ref.html",
                ["summary"] = "hello world"
            };
            var output = Process("index", "input", new { reference = "reference" }, xref);

            Assert.Equal($"{_outputFolder}/input.html".ToNormalizedFullPath(), output.OutputFiles[".html"].RelativePath.ToNormalizedPath());
            Assert.Equal(@"
<h2>reference</h2><p>hello world</p>
", File.ReadAllText(Path.Combine(_outputFolder, "input.html")));

        }

        private ManifestItem Process(string documentType, string fileName, object content, XRefSpec spec)
        {
            var reader = new LocalFileResourceReader(_templateFolder);
            var context = new DocumentBuildContext(_outputFolder);
            context.RegisterInternalXrefSpec(spec);
            var processor = new TemplateProcessor(reader, context, 64);
            var inputItem = new InternalManifestItem
            {
                DocumentType = documentType,
                Extension = "html",
                FileWithoutExtension = Path.GetFullPath(Path.Combine(_outputFolder, Path.GetFileNameWithoutExtension(fileName))),
                LocalPathFromRoot = fileName,
                Model = new ModelWithCache(content),
            };
            return processor.Process(new List<InternalManifestItem> { inputItem }, new ApplyTemplateSettings(_inputFolder, _outputFolder))[0];
        }
    }
}
