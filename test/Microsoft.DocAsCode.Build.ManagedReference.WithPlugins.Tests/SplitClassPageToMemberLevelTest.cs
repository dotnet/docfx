// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.ManagedReference.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode.Build.Engine;
    using Microsoft.DocAsCode.Build.ManagedReference;
    using Microsoft.DocAsCode.Build.ManagedReference.BuildOutputs;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.DataContracts.ManagedReference;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Tests.Common;

    using Newtonsoft.Json.Linq;
    using Xunit;
    using TableOfContents;
    using System.Text.RegularExpressions;

    [Trait("Owner", "lianwei")]
    [Trait("EntityType", "ManagedReferenceDocumentProcessorWithPlugins")]
    public class SplitClassPageToMemberLevelTest : TestBase
    {
        private string _outputFolder;
        private string _inputFolder;
        private string _templateFolder;
        private FileCollection _defaultFiles;
        private ApplyTemplateSettings _applyTemplateSettings;
        private TemplateManager _templateManager;

        private const string RawModelFileExtension = ".raw.json";
        private const string MrefDirectory = "mref";

        public SplitClassPageToMemberLevelTest()
        {
            _outputFolder = GetRandomFolder();
            _inputFolder = GetRandomFolder();
            _templateFolder = GetRandomFolder();
            _applyTemplateSettings = new ApplyTemplateSettings(_inputFolder, _outputFolder)
            {
                RawModelExportSettings = { Export = true },
                TransformDocument = true,
            };

            _templateManager = new TemplateManager(null, null, new List<string> { "template" }, null, "TestData/");
        }

        [Fact]
        public void ProcessMrefShouldSucceed()
        {
            var files = new FileCollection(Directory.GetCurrentDirectory());
            files.Add(DocumentType.Article, new[] { "TestData/mref/Namespace1.Class1`2.yml" }, "TestData/");
            BuildDocument(files);
            {
                var outputRawModelPath = GetRawModelFilePath("Namespace1.Class1`2.yml");
                Assert.True(File.Exists(outputRawModelPath));
                var model = JsonUtility.Deserialize<ApiBuildOutput>(outputRawModelPath);
                Assert.NotNull(model);

                Assert.Equal("Hello world!", model.Metadata["meta"]);
                Assert.Equal(3, model.Children.Count);
            }
            {
                var outputRawModelPath = GetRawModelFilePath("CatLibrary.Cat`2.#ctor.yml");
                Assert.True(File.Exists(outputRawModelPath));
                var model = JsonUtility.Deserialize<ApiBuildOutput>(outputRawModelPath);
                Assert.NotNull(model);

                Assert.Equal("Hello world!", model.Metadata["meta"]);
                Assert.Equal(MemberType.Constructor, model.Type);
                Assert.Equal(3, model.Children.Count);
            }
        }

        [Fact]
        public void ProcessMrefWithTocShouldSucceed()
        {
            var files = new FileCollection(Directory.GetCurrentDirectory());
            files.Add(DocumentType.Article, new[] { "TestData/mref/Namespace1.Class1`2.yml" }, "TestData/");
            files.Add(DocumentType.Article, new[] { "TestData/mref/toc.yml" }, "TestData/");
            BuildDocument(files);
            {
                var outputRawModelPath = GetRawModelFilePath("Namespace1.Class1`2.yml");
                Assert.True(File.Exists(outputRawModelPath));
                var model = JsonUtility.Deserialize<ApiBuildOutput>(outputRawModelPath);
                Assert.NotNull(model);

                Assert.Equal("Hello world!", model.Metadata["meta"]);
                Assert.Equal(3, model.Children.Count);
            }
            {
                var outputRawModelPath = GetRawModelFilePath("CatLibrary.Cat`2.#ctor.yml");
                Assert.True(File.Exists(outputRawModelPath));
                var model = JsonUtility.Deserialize<ApiBuildOutput>(outputRawModelPath);
                Assert.NotNull(model);

                Assert.Equal("Hello world!", model.Metadata["meta"]);
                Assert.Equal(MemberType.Constructor, model.Type);
                Assert.Equal(3, model.Children.Count);
                Assert.Equal(new List<string> { "net2", "net46" }, model.Platform);
            }
            {
                var outputRawModelPath = GetRawModelFilePath("toc.yml");
                Assert.True(File.Exists(outputRawModelPath));
                var model = JsonUtility.Deserialize<TocItemViewModel>(outputRawModelPath);
                Assert.NotNull(model);
                Assert.Equal(1, model.Items.Count);
                Assert.Equal("Namespace1.Class1%602.html", model.Items[0].TopicHref);
                Assert.Equal(1, model.Items[0].Items.Count);
                Assert.Equal("CatLibrary.Cat%602.%23ctor.html", model.Items[0].Items[0].TopicHref);
                Assert.Equal("Cat", model.Items[0].Items[0].Name);
                Assert.Equal(new List<string> { "net2", "net46" }, JArray.FromObject(model.Items[0].Items[0].Metadata[Constants.PropertyName.Platform]).Select(s => s.ToString()).ToList());
            }
        }

        private void BuildDocument(FileCollection files)
        {
            var parameters = new DocumentBuildParameters
            {
                Files = files,
                OutputBaseDir = _outputFolder,
                ApplyTemplateSettings = _applyTemplateSettings,
                Metadata = new Dictionary<string, object>
                {
                    ["meta"] = "Hello world!",
                }.ToImmutableDictionary(),
                TemplateManager = _templateManager
            };

            using (var builder = new DocumentBuilder(LoadAssemblies(), ImmutableArray<string>.Empty, null))
            {
                builder.Build(parameters);
            }
        }

        private static IEnumerable<System.Reflection.Assembly> LoadAssemblies()
        {
            yield return typeof(ManagedReferenceDocumentProcessor).Assembly;
            yield return typeof(SplitClassPageToMemberLevel).Assembly;
            yield return typeof(TocDocumentProcessor).Assembly;
        }

        private string GetRawModelFilePath(string fileName)
        {
            return Path.GetFullPath(Path.Combine(_outputFolder, MrefDirectory, Path.ChangeExtension(fileName, RawModelFileExtension)));
        }

        private string GetOutputFilePath(string fileName)
        {
            return Path.GetFullPath(Path.Combine(_outputFolder, Path.ChangeExtension(fileName, "html")));
        }
    }
}
