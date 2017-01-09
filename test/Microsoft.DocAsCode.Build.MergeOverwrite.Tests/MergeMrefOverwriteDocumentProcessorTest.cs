// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.MergeOverwrite.Tests
{
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode.Build.Engine;
    using Microsoft.DocAsCode.Build.ManagedReference;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.DataContracts.ManagedReference;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Tests.Common;

    using Xunit;

    [Trait("Owner", "yufeih")]
    [Trait("EntityType", nameof(MergeMrefOverwriteDocumentProcessor))]
    public class MergeMrefOverwriteDocumentProcessorTest : TestBase
    {
        [Fact]
        public void ProcessMrefWithMergeOverwriteProcessorShouldSucceed()
        {
            var files = new FileCollection(Directory.GetCurrentDirectory());

            files.Add(DocumentType.Article, new[] { "TestData/mref/CatLibrary.Cat-2.yml" }, "TestData/");
            files.Add(DocumentType.Overwrite, new[]
            {
                "TestData/overwrite/mref.overwrite.default.md",
                "TestData/overwrite/mref.overwrite.simple.md",
            });

            var outputDir = GetRandomFolder();

            var parameters = new DocumentBuildParameters
            {
                Files = files,
                OutputBaseDir = outputDir,
                MarkdownEngineName = "momd",
                ApplyTemplateSettings = new ApplyTemplateSettings("", outputDir),
            };

            var assemblies = new[]
            {
                typeof(ManagedReferenceDocumentProcessor).Assembly,
                typeof(MergeMrefOverwriteDocumentProcessor).Assembly,
            };

            using (var builder = new DocumentBuilder(assemblies, ImmutableArray<string>.Empty, null))
            {
                builder.Build(parameters);
            }

            var yaml = YamlUtility.Deserialize<PageViewModel>(Path.Combine(outputDir, "mref/CatLibrary.Cat-2.yml"));

            Assert.Collection(
                yaml.Items.Where(item => item.Uid == "CatLibrary.Cat`2.#ctor"),
                e =>
                {
                    Assert.Equal("Overwrite *markdown* summary\n\n", e.Summary);
                    Assert.Equal("Overwrite *markdown* content\n\n", e.Conceptual);
                });

            Assert.Collection(
                yaml.Items.Where(item => item.Uid == "CatLibrary.Cat`2"),
                e =>
                {
                    Assert.Equal("Overwrite <b>html</b> content\n\n", e.Summary);
                    Assert.Equal("original conceptual", e.Conceptual);
                });
        }
    }
}
