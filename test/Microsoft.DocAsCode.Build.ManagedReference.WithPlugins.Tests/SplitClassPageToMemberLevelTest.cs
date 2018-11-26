// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.ManagedReference.Tests
{
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

    [Trait("Owner", "lianwei")]
    [Trait("EntityType", "ManagedReferenceDocumentProcessorWithPlugins")]
    public class SplitClassPageToMemberLevelTest : TestBase
    {
        private string _outputFolder;
        private string _inputFolder;
        private string _templateFolder;
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
            files.Add(DocumentType.Article, new[] { "TestData/mref/CatLibrary.Cat`2.yml" }, "TestData/");
            BuildDocument(files);
            {
                var outputRawModelPath = GetRawModelFilePath("CatLibrary.Cat`2.yml");
                Assert.True(File.Exists(outputRawModelPath));
                var model = JsonUtility.Deserialize<ApiBuildOutput>(outputRawModelPath);
                Assert.NotNull(model);

                Assert.Equal("Hello world!", model.Metadata["meta"]);
                Assert.Equal(true, model.Metadata["_splitReference"]);
                Assert.Equal(true, model.Metadata["_splitFrom"]);
                Assert.Equal(20, model.Children.Count);
            }
            {
                var outputRawModelPath = GetRawModelFilePath("CatLibrary.Cat-2.-ctor.yml");
                Assert.True(File.Exists(outputRawModelPath));
                var model = JsonUtility.Deserialize<ApiBuildOutput>(outputRawModelPath);
                Assert.NotNull(model);

                Assert.Equal("Hello world!", model.Metadata["meta"]);
                Assert.Equal(true, model.Metadata["_splitReference"]);
                Assert.Equal(false, model.Metadata.ContainsKey("_splitFrom"));
                Assert.Equal(MemberType.Constructor, model.Type);
                Assert.Equal(3, model.Children.Count);
            }
        }

        [Fact]
        public void ProcessMrefEnumShouldSucceed()
        {
            var files = new FileCollection(Directory.GetCurrentDirectory());
            files.Add(DocumentType.Article, new[] { "TestData/mref/Microsoft.DocAsCode.Build.SchemaDriven.MergeType.yml" }, "TestData/");
            BuildDocument(files);
            {
                var outputRawModelPath = GetRawModelFilePath("Microsoft.DocAsCode.Build.SchemaDriven.MergeType.yml");
                Assert.True(File.Exists(outputRawModelPath));
                var model = JsonUtility.Deserialize<ApiBuildOutput>(outputRawModelPath);
                Assert.NotNull(model);

                Assert.Equal("Hello world!", model.Metadata["meta"]);
                Assert.False(model.Metadata.TryGetValue("_splitReference", out var split));
                Assert.False(model.Metadata.TryGetValue("_splitFrom", out var from));
                Assert.Equal(4, model.Children.Count);
            }
            {
                var xrefmap = YamlUtility.Deserialize<XRefMap>(Path.Combine(_outputFolder, "xrefmap.yml"));
                Assert.Equal(5, xrefmap.References.Count);
            }
        }

        [Fact]
        public void ProcessMrefWithTocShouldSucceed()
        {
            var files = new FileCollection(Directory.GetCurrentDirectory());
            files.Add(DocumentType.Article, new[] { "TestData/mref/CatLibrary.Cat`2.yml" }, "TestData/");
            files.Add(DocumentType.Article, new[] { "TestData/mref/toc.yml" }, "TestData/");
            BuildDocument(files);
            {
                var outputRawModelPath = GetRawModelFilePath("CatLibrary.Cat`2.yml");
                Assert.True(File.Exists(outputRawModelPath));
                var model = JsonUtility.Deserialize<ApiBuildOutput>(outputRawModelPath);
                Assert.NotNull(model);

                Assert.Equal("Hello world!", model.Metadata["meta"]);
                Assert.Equal(true, model.Metadata["_splitReference"]);
                Assert.Equal(20, model.Children.Count);
            }
            {
                var outputRawModelPath = GetRawModelFilePath("CatLibrary.Cat-2.-ctor.yml");
                Assert.True(File.Exists(outputRawModelPath));
                var model = JsonUtility.Deserialize<ApiBuildOutput>(outputRawModelPath);
                Assert.NotNull(model);

                Assert.Equal("Hello world!", model.Metadata["meta"]);
                Assert.Equal(true, model.Metadata["_splitReference"]);
                Assert.Equal(MemberType.Constructor, model.Type);
                Assert.Equal(3, model.Children.Count);
                Assert.Equal(new List<string> { "net2", "net46" }, model.Platform);
                Assert.Equal("<p sourcefile=\"TestData/mref/CatLibrary.Cat`2.yml\" sourcestartlinenumber=\"1\" sourceendlinenumber=\"1\">Overload summary</p>\n", model.Summary);
                Assert.Equal("<p sourcefile=\"TestData/mref/CatLibrary.Cat`2.yml\" sourcestartlinenumber=\"1\" sourceendlinenumber=\"1\">Overload <em>remarks</em></p>\n", model.Remarks);
                Assert.Equal(new List<string>
                {
                    "<p sourcefile=\"TestData/mref/CatLibrary.Cat`2.yml\" sourcestartlinenumber=\"1\" sourceendlinenumber=\"1\">Overload example 1</p>\n",
                    "<p sourcefile=\"TestData/mref/CatLibrary.Cat`2.yml\" sourcestartlinenumber=\"1\" sourceendlinenumber=\"1\">Overload <strong>example 2</strong></p>\n"
                }, model.Examples);
                Assert.Equal("Not defined Property", model.Metadata["not-defined"]);
                Assert.NotNull(model.Source);

                Assert.Equal("net46", JArray.FromObject(model.Children[0].Metadata[Constants.MetadataName.Version])[0].ToString());
                Assert.Equal("net2", JArray.FromObject(model.Children[1].Metadata[Constants.MetadataName.Version])[0].ToString());
            }
            {
                var outputRawModelPath = GetRawModelFilePath("toc.yml");
                Assert.True(File.Exists(outputRawModelPath));
                var model = JsonUtility.Deserialize<TocItemViewModel>(outputRawModelPath);
                Assert.NotNull(model);
                Assert.Equal(1, model.Items.Count);
                Assert.Equal("CatLibrary.Cat%602.html", model.Items[0].TopicHref);
                Assert.Equal(16, model.Items[0].Items.Count);
                Assert.Equal("CatLibrary.Cat-2.op_Addition.html", model.Items[0].Items[0].TopicHref);
                Assert.Equal("Addition", model.Items[0].Items[0].Name);
                Assert.Equal("CatLibrary.Cat-2.op_Subtraction.html", model.Items[0].Items[15].TopicHref);
                Assert.Equal("Subtraction", model.Items[0].Items[15].Name);

                var ctor = model.Items[0].Items.FirstOrDefault(s => s.Name == "Cat");
                Assert.NotNull(ctor);
                Assert.Equal("CatLibrary.Cat`2.#ctor*", ctor.TopicUid);
                Assert.Equal("Constructor", ctor.Metadata["type"].ToString());
                Assert.Equal(new List<string> { "net2", "net46" }, JArray.FromObject(ctor.Metadata[Constants.PropertyName.Platform]).Select(s => s.ToString()).ToList());
                Assert.Equal(new List<string> { "net2", "net46" }, JArray.FromObject(ctor.Metadata[Constants.MetadataName.Version]).Select(s => s.ToString()).ToList());
            }
            {
                var manifestFile = Path.GetFullPath(Path.Combine(_outputFolder, "manifest.json"));
                var manifest = JsonUtility.Deserialize<Manifest>(manifestFile);
                Assert.Equal(17, manifest.Files.Count);

                // NOTE: split output files have the same source file path
                var groups = manifest.Files.GroupBy(s => s.SourceRelativePath).ToList().OrderByDescending(s => s.Count()).ToList();
                Assert.Equal(1, groups.Count);
            }
        }

        [Fact]
        public void ProcessMrefWithLongPathShouldSucceed()
        {
            var files = new FileCollection(Directory.GetCurrentDirectory());
            files.Add(DocumentType.Article, new[] { "TestData/mref/System.Activities.Presentation.Model.ModelItemDictionary.yml" }, "TestData/");
            files.Add(DocumentType.Article, new[] { "TestData/mref/ModelItemDictionary/toc.yml" }, "TestData/");
            BuildDocument(files);
            {
                var outputRawModelPath = GetRawModelFilePath("System.Activities.Presentation.Model.ModelItemDictionary.yml");
                Assert.True(File.Exists(outputRawModelPath));
                var model = JsonUtility.Deserialize<ApiBuildOutput>(outputRawModelPath);
                Assert.NotNull(model);

                Assert.Equal("Hello world!", model.Metadata["meta"]);
                Assert.Equal(43, model.Children.Count);
            }
            {
                var outputRawModelPath = GetRawModelFilePath("System.Activities.Presentation.Model.ModelItemDictionary.Add.yml");
                Assert.True(File.Exists(outputRawModelPath));
                var model = JsonUtility.Deserialize<ApiBuildOutput>(outputRawModelPath);
                Assert.NotNull(model);

                Assert.Equal("Hello world!", model.Metadata["meta"]);
                Assert.Equal(MemberType.Method, model.Type);
                Assert.Equal(2, model.Children.Count);
                Assert.Equal(new List<string> { "net-11", "net-20", "netcore-10" }, model.Platform);
            }
            {
                var outputRawModelPath = GetRawModelFilePath("System.Activities.Presentation.Model.ModelItemDictionary.Add_1.yml");
                Assert.True(File.Exists(outputRawModelPath));
                var model = JsonUtility.Deserialize<ApiBuildOutput>(outputRawModelPath);
                Assert.NotNull(model);

                Assert.Equal("Hello world!", model.Metadata["meta"]);
                Assert.Equal(MemberType.Method, model.Type);
                Assert.Equal(1, model.Children.Count);
                Assert.Equal(new List<string> { "net-11", "net-20", "netcore-10" }, model.Platform);
                Assert.True(model.IsExplicitInterfaceImplementation);
            }
            {
                var outputRawModelPath = GetRawModelFilePath("ModelItemDictionary\\toc.yml");
                Assert.True(File.Exists(outputRawModelPath));
                var model = JsonUtility.Deserialize<TocItemViewModel>(outputRawModelPath);
                Assert.NotNull(model);
                Assert.Equal(1, model.Items.Count);
                Assert.Equal("../System.Activities.Presentation.Model.ModelItemDictionary.html", model.Items[0].TopicHref);
                Assert.Equal(38, model.Items[0].Items.Count);

                Assert.Equal("../System.Activities.Presentation.Model.ModelItemDictionary.Add.html", model.Items[0].Items[0].TopicHref);
                Assert.Equal("Add", model.Items[0].Items[0].Name);

                var eiiItem = model.Items[0].Items[20];
                Assert.Equal(5, eiiItem.Metadata.Count);
                Assert.True(eiiItem.Metadata["isEii"] as bool?);
            }
        }

        [Fact]
        public void CheckDuplicateFileName()
        {
            var files = new FileCollection(Directory.GetCurrentDirectory());
            files.Add(DocumentType.Article, new[] { "TestData/mref/com.microsoft.azure.management.sql.SqlServer.FirewallRules.yml" }, "TestData/");
            files.Add(DocumentType.Article, new[] { "TestData/mref/com.microsoft.azure.management.sql.SqlServer.yml" }, "TestData/");
            files.Add(DocumentType.Article, new[] { "TestData/mref/com.microsoft.azure.management.sql.yml" }, "TestData/");
            files.Add(DocumentType.Article, new[] { "TestData/mref/sql/toc.yml" }, "TestData/");
            BuildDocument(files);

            var ignoreCase = PathUtility.IsPathCaseInsensitive();
            {
                var outputRawModelPath = GetRawModelFilePath("com.microsoft.azure.management.sql.SqlServer.firewallRules(Method).yml");
                Assert.True(File.Exists(outputRawModelPath));
                var model = JsonUtility.Deserialize<ApiBuildOutput>(outputRawModelPath);
                Assert.NotNull(model);

                Assert.Equal("mref/com.microsoft.azure.management.sql.SqlServer.firewallRules(Method).html", model.Metadata["_path"].ToString(), ignoreCase);
                Assert.Equal("TestData/mref/com.microsoft.azure.management.sql.SqlServer.firewallRules(Method).yml", model.Metadata["_key"].ToString(), ignoreCase);
            }
            {
                var outputRawModelPath = GetRawModelFilePath("com.microsoft.azure.management.sql.SqlServer.FirewallRules(Interface).yml");
                Assert.True(File.Exists(outputRawModelPath));
                var model = JsonUtility.Deserialize<ApiBuildOutput>(outputRawModelPath);
                Assert.NotNull(model);

                Assert.Equal("mref/com.microsoft.azure.management.sql.SqlServer.FirewallRules(Interface).html", model.Metadata["_path"].ToString(), ignoreCase);
                Assert.Equal("TestData/mref/com.microsoft.azure.management.sql.SqlServer.FirewallRules(Interface).yml", model.Metadata["_key"].ToString(), ignoreCase);
            }
            {
                var outputRawModelPath = GetRawModelFilePath("com.microsoft.azure.management.sql.SqlServer.firewallRules(Interface)_1.yml");
                Assert.True(File.Exists(outputRawModelPath));
                var model = JsonUtility.Deserialize<ApiBuildOutput>(outputRawModelPath);
                Assert.NotNull(model);

                Assert.Equal("mref/com.microsoft.azure.management.sql.SqlServer.firewallRules(Interface)_1.html", model.Metadata["_path"].ToString(), ignoreCase);
                Assert.Equal("TestData/mref/com.microsoft.azure.management.sql.SqlServer.firewallRules(Interface)_1.yml", model.Metadata["_key"].ToString(), ignoreCase);
            }
            {
                var outputRawModelPath = GetRawModelFilePath("sql\\toc.yml");
                Assert.True(File.Exists(outputRawModelPath));
                var model = JsonUtility.Deserialize<TocItemViewModel>(outputRawModelPath);
                Assert.NotNull(model);

                var topicHref = new List<string>()
                {
                    model.Items[0].Items[0].Items[0].TopicHref,
                    model.Items[0].Items[1].TopicHref,
                    model.Items[0].Items[2].TopicHref
                };
                Assert.Contains("../com.microsoft.azure.management.sql.SqlServer.firewallRules%28Method%29.html", topicHref, FilePathComparer.OSPlatformSensitiveStringComparer);
                Assert.Contains("../com.microsoft.azure.management.sql.SqlServer.FirewallRules%28Interface%29.html", topicHref, FilePathComparer.OSPlatformSensitiveStringComparer);
                Assert.Contains("../com.microsoft.azure.management.sql.SqlServer.firewallRules%28Interface%29_1.html", topicHref, FilePathComparer.OSPlatformSensitiveStringComparer);
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
