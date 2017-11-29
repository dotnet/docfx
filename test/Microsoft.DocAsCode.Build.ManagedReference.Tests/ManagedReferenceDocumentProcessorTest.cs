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

    [Trait("Owner", "lianwei")]
    [Trait("EntityType", "ManagedReferenceDocumentProcessor")]
    public class ManagedReferenceDocumentProcessorTest : TestBase
    {
        private string _outputFolder;
        private string _inputFolder;
        private string _templateFolder;
        private FileCollection _defaultFiles;
        private ApplyTemplateSettings _applyTemplateSettings;
        private TemplateManager _templateManager;

        private const string RawModelFileExtension = ".raw.json";
        private const string MrefDirectory = "mref";

        public ManagedReferenceDocumentProcessorTest()
        {
            _outputFolder = GetRandomFolder();
            _inputFolder = GetRandomFolder();
            _templateFolder = GetRandomFolder();
            _defaultFiles = new FileCollection(Directory.GetCurrentDirectory());
            _defaultFiles.Add(DocumentType.Article, new[] { "TestData/mref/CatLibrary.Cat-2.yml" }, "TestData/");
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
            FileCollection files = new FileCollection(_defaultFiles);
            BuildDocument(files);

            var outputRawModelPath = GetRawModelFilePath("CatLibrary.Cat-2.yml");
            Assert.True(File.Exists(outputRawModelPath));
            var model = JsonUtility.Deserialize<ApiBuildOutput>(outputRawModelPath);
            Assert.NotNull(model);

            Assert.Equal("Hello world!", model.Metadata["meta"]);
            Assert.Equal("item level metadata should overwrite page level metadata.", model.Metadata["anotherMeta"]);
            Assert.Equal(1, model.Attributes.Count);
            Assert.Equal("System.SerializableAttribute.#ctor", model.Attributes[0].Constructor);
            Assert.Equal(0, model.Attributes[0].Arguments.Count);
            Assert.Equal("System.SerializableAttribute", model.Attributes[0].Type);

            Assert.Equal(2, model.Implements.Count);

            Assert.Equal(1, model.Inheritance.Count);

            Assert.Equal(6, model.InheritedMembers.Count);

            Assert.Equal(2, model.Syntax.Content.Count);
            Assert.Equal("csharp", model.Syntax.Content[0].Language);
            Assert.Equal("<p><a href=\"http://A/\" data-raw-source=\"[A](http://A/)\" sourcefile=\"TestData/mref/CatLibrary.Cat-2.yml\" sourcestartlinenumber=\"1\" sourceendlinenumber=\"1\">A</a>.</p>", model.AdditionalNotes.Implementer);
            Assert.Equal("[Serializable]\npublic class Cat<T, K> : ICat, IAnimal where T : class, new ()where K : struct", model.Syntax.Content[0].Value);
            Assert.Equal("vb", model.Syntax.Content[1].Language);
            Assert.Equal("<Serializable>\nPublic Class Cat(Of T As {Class, New}, K As Structure)\n\n    Implements ICat, IAnimal", model.Syntax.Content[1].Value);

            Assert.Equal(2, model.Syntax.TypeParameters.Count);
            Assert.Equal("T", model.Syntax.TypeParameters[0].Name);
            Assert.Equal("<p sourcefile=\"TestData/mref/CatLibrary.Cat-2.yml\" sourcestartlinenumber=\"1\" sourceendlinenumber=\"1\">This type should be class and can new instance.</p>\n", model.Syntax.TypeParameters[0].Description);
            Assert.Equal("K", model.Syntax.TypeParameters[1].Name);
            Assert.Equal("<p sourcefile=\"TestData/mref/CatLibrary.Cat-2.yml\" sourcestartlinenumber=\"1\" sourceendlinenumber=\"1\">This type is a struct type, class type can&#39;t be used for this parameter.</p>\n", model.Syntax.TypeParameters[1].Description);

            Assert.Equal(1, model.Examples.Count);
            Assert.Equal("<p>Here&#39;s example of how to create an instance of <strong>Cat</strong> class. As T is limited with <code>class</code> and K is limited with <code>struct</code>.</p>\n<pre><code class=\"c#\">    var a = new Cat(object, int)();\n    int catNumber = new int();\n    unsafe\n    {\n        a.GetFeetLength(catNumber);\n    }</code></pre>\n<p>As you see, here we bring in <strong>pointer</strong> so we need to add <span class=\"languagekeyword\">unsafe</span> keyword.</p>\n", model.Examples[0]);

            Assert.Equal(20, model.Children.Count);
            var cm = model.Children[1];
            Assert.Equal("<p><a href=\"http://A/\" data-raw-source=\"[A](http://A/)\" sourcefile=\"TestData/mref/CatLibrary.Cat-2.yml\" sourcestartlinenumber=\"1\" sourceendlinenumber=\"1\">A</a>.</p>", cm.AdditionalNotes.Implementer);
            Assert.Equal("<p><a href=\"http://B/\" data-raw-source=\"[B](http://B/)\" sourcefile=\"TestData/mref/CatLibrary.Cat-2.yml\" sourcestartlinenumber=\"1\" sourceendlinenumber=\"1\">B</a>.</p>", cm.AdditionalNotes.Inheritor);
            Assert.Equal("<p><a href=\"http://C/\" data-raw-source=\"[C](http://C/)\" sourcefile=\"TestData/mref/CatLibrary.Cat-2.yml\" sourcestartlinenumber=\"1\" sourceendlinenumber=\"1\">C</a>.</p>", cm.AdditionalNotes.Caller);

        }

        [Fact]
        public void ProcessMrefWithComplexFileNameShouldSucceed()
        {
            FileCollection files = new FileCollection(_defaultFiles);
            files.RemoveAll(s => true);
            files.Add(DocumentType.Article, new string[] { "TestData/mref/Namespace1.Class1`2.yml", "TestData/mref/Namespace1.Class1`2.#ctor.yml" }, "TestData/");
            BuildDocument(files);

            var outputRawModelPath = GetRawModelFilePath("Namespace1.Class1`2.yml");
            Assert.True(File.Exists(outputRawModelPath));
            var model = JsonUtility.Deserialize<ApiBuildOutput>(outputRawModelPath);
            Assert.NotNull(model);
            outputRawModelPath = GetRawModelFilePath("Namespace1.Class1`2.#ctor.yml");
            Assert.True(File.Exists(outputRawModelPath));
            model = JsonUtility.Deserialize<ApiBuildOutput>(outputRawModelPath);
            Assert.NotNull(model);
            var outputHtml = GetOutputFilePath("mref/Namespace1.Class1`2.html");
            Assert.True(File.Exists(outputHtml));
            var content = File.ReadAllText(outputHtml);
            Assert.Equal("<p><a class=\"xref\" href=\"Namespace1.Class1%602.%23ctor.html#constructor\">Constructor</a></p>\n", content);
        }

        [Fact]
        public void ProcessMrefWithXRefMapShouldSucceed()
        {
            var files = new FileCollection(_defaultFiles);
            BuildDocument(files);

            var xrefMapPath = Path.Combine(Directory.GetCurrentDirectory(), _outputFolder, XRefArchive.MajorFileName);
            Assert.True(File.Exists(xrefMapPath));

            var xrefMap = YamlUtility.Deserialize<XRefMap>(xrefMapPath);

            Assert.NotNull(xrefMap.References);
            Assert.Equal(34, xrefMap.References.Count);
        }

        [Fact]
        public void ProcessMrefWithDefaultOverwriteShouldSucceed()
        {
            FileCollection files = new FileCollection(_defaultFiles);
            files.Add(DocumentType.Overwrite, new[] { "TestData/overwrite/mref.overwrite.default.md" });
            BuildDocument(files);
            {
                var outputRawModelPath = GetRawModelFilePath("CatLibrary.Cat-2.yml");
                Assert.True(File.Exists(outputRawModelPath));
                var model = JsonUtility.Deserialize<ApiBuildOutput>(outputRawModelPath);
                Assert.Equal("public", model.Children[0].Modifiers["csharp"][0]);
                Assert.Equal("<p sourcefile=\"TestData/overwrite/mref.overwrite.default.md\" sourcestartlinenumber=\"1\" sourceendlinenumber=\"1\">Overwrite summary</p>\n", model.Children[0].Metadata["summary"]);
                Assert.Equal("<p sourcefile=\"TestData/overwrite/mref.overwrite.default.md\" sourcestartlinenumber=\"6\" sourceendlinenumber=\"6\">Overwrite content</p>\n", model.Children[0].Conceptual);
            }
        }

        [Fact]
        public void ProcessMrefWithSimpleOverwriteShouldSucceed()
        {
            FileCollection files = new FileCollection(_defaultFiles);
            files.Add(DocumentType.Overwrite, new[] { "TestData/overwrite/mref.overwrite.simple.md" });
            BuildDocument(files);
            var outputRawModelPath = GetRawModelFilePath("CatLibrary.Cat-2.yml");
            Assert.True(File.Exists(outputRawModelPath));
            var model = JsonUtility.Deserialize<ApiBuildOutput>(outputRawModelPath);
            Assert.Equal("<p sourcefile=\"TestData/overwrite/mref.overwrite.simple.md\" sourcestartlinenumber=\"6\" sourceendlinenumber=\"6\">Overwrite content</p>\n", model.Summary);
            Assert.Null(model.Conceptual);
        }

        [Fact]
        public void ProcessMrefWithParametersOverwriteShouldSucceed()
        {
            var files = new FileCollection(_defaultFiles);
            files.Add(DocumentType.Overwrite, new[] { "TestData/overwrite/mref.overwrite.parameters.md" });
            BuildDocument(files);
            var outputRawModelPath = GetRawModelFilePath("CatLibrary.Cat-2.yml");
            Assert.True(File.Exists(outputRawModelPath));
            var model = JsonUtility.Deserialize<ApiBuildOutput>(outputRawModelPath);

            var method = model.Children.First(s => s.Uid == "CatLibrary.Cat`2.CatLibrary#IAnimal#Eat``1(``0)");

            // Verify overwrite parameters
            Assert.Equal("<p sourcefile=\"TestData/overwrite/mref.overwrite.parameters.md\" sourcestartlinenumber=\"1\" sourceendlinenumber=\"1\">The overwritten description for a</p>\n", method.Syntax.Parameters[0].Description);
            Assert.NotNull(method.Syntax.Parameters[0].Type);
            Assert.Equal("<p sourcefile=\"TestData/overwrite/mref.overwrite.parameters.md\" sourcestartlinenumber=\"12\" sourceendlinenumber=\"12\">This is overwritten type parameters</p>\n", method.Syntax.TypeParameters[0].Description);
            Assert.Null(model.Conceptual);
        }

        [Fact]
        public void ProcessMrefWithNotPredefinedOverwriteShouldSucceed()
        {
            FileCollection files = new FileCollection(_defaultFiles);
            files.Add(DocumentType.Overwrite, new[] { "TestData/overwrite/mref.overwrite.not.predefined.md" });
            BuildDocument(files);
            {
                var outputRawModelPath = GetRawModelFilePath("CatLibrary.Cat-2.yml");
                Assert.True(File.Exists(outputRawModelPath));
                var model = JsonUtility.Deserialize<ApiBuildOutput>(outputRawModelPath);


                Assert.Equal("<p sourcefile=\"TestData/overwrite/mref.overwrite.not.predefined.md\" sourcestartlinenumber=\"6\" sourceendlinenumber=\"6\">Overwrite content</p>\n"
                    , model.Metadata["not_defined_property"]);

                var method = model.Children.First(s => s.Uid == "CatLibrary.Cat`2.#ctor");
                Assert.Equal("<p sourcefile=\"TestData/overwrite/mref.overwrite.not.predefined.md\" sourcestartlinenumber=\"13\" sourceendlinenumber=\"13\">Overwrite content</p>\n"
                    , method.Metadata["not_defined_property"]);
            }
        }

        [Fact]
        public void ProcessMrefWithDynamicDevLangsShouldSucceed()
        {
            FileCollection files = new FileCollection(_defaultFiles);
            files.RemoveAll(s => true);
            files.Add(DocumentType.Article, new [] { "TestData/mref/System.String.yml" }, "TestData/");

            BuildDocument(files);

            var outputRawModelPath = GetRawModelFilePath("System.String.yml");
            Assert.True(File.Exists(outputRawModelPath));
            var model = JsonUtility.Deserialize<ApiBuildOutput>(outputRawModelPath);
            Assert.NotNull(model);
            Assert.NotNull(model.Syntax);
            Assert.NotNull(model.Syntax.Content);
            Assert.Equal(4, model.Syntax.Content.Count);
            Assert.Equal("public ref class String sealed", model.Syntax.Content.First(c => c.Language == "cpp").Value);
            Assert.Equal("public sealed class String", model.Syntax.Content.First(c => c.Language == "csharp").Value);
            Assert.Equal("type String", model.Syntax.Content.First(c => c.Language == "fsharp").Value);
            Assert.Equal("Public NotInheritable Class String", model.Syntax.Content.First(c => c.Language == "vb").Value);
        }

        [Fact]
        public void ProcessMrefWithInvalidCrossReferenceShouldWarn()
        {
            var files = new FileCollection(Directory.GetCurrentDirectory());
            files.Add(DocumentType.Article, new[] { "TestData/mref/System.String.yml" }, "TestData/");
            files.Add(DocumentType.Overwrite, new[] { "TestData/overwrite/mref.overwrite.invalid.ref.md" });

            var listener = TestLoggerListener.CreateLoggerListenerWithPhaseStartFilter(nameof(ProcessMrefWithInvalidCrossReferenceShouldWarn), LogLevel.Info);
            try
            {
                Logger.RegisterListener(listener);

                using (new LoggerPhaseScope(nameof(ProcessMrefWithInvalidCrossReferenceShouldWarn)))
                {
                    BuildDocument(files);
                }

                var warnings = listener.GetItemsByLogLevel(LogLevel.Warning);
                Assert.Equal(1, warnings.Count());
                var warning = warnings.Single();
                Assert.Equal("2 invalid cross reference(s) \"<xref:invalidXref1>\", \"<xref:invalidXref2>\".", warning.Message);
                Assert.Equal("TestData/mref/System.String.yml", warning.File);

                var infos = listener.GetItemsByLogLevel(LogLevel.Info).Where(i => i.Message.Contains("Details for invalid cross reference(s)")).ToList();
                Assert.Equal(1, infos.Count());
                Assert.Equal("Details for invalid cross reference(s): \"<xref:invalidXref1>\" in line 6, \"<xref:invalidXref2>\" in line 8", infos[0].Message);
                Assert.Equal("TestData/overwrite/mref.overwrite.invalid.ref.md", infos[0].File);
                Assert.Null(infos[0].Line);
            }
            finally
            {
                Logger.UnregisterListener(listener);
            }
        }

        [Fact]
        public void ProcessMrefWithInvalidOverwriteShouldFail()
        {
            FileCollection files = new FileCollection(_defaultFiles);
            files.Add(DocumentType.Overwrite, new[] { "TestData/overwrite/mref.overwrite.invalid.md" });
            Assert.Throws<DocumentException>(() => BuildDocument(files));
        }

        [Fact]
        public void ProcessMrefWithRemarksOverwriteShouldSucceed()
        {
            var files = new FileCollection(_defaultFiles);
            files.Add(DocumentType.Overwrite, new[] { "TestData/overwrite/mref.overwrite.remarks.md" });
            BuildDocument(files);
            {
                var outputRawModelPath = GetRawModelFilePath("CatLibrary.Cat-2.yml");
                Assert.True(File.Exists(outputRawModelPath));
                var model = JsonUtility.Deserialize<ApiBuildOutput>(outputRawModelPath);
                var method = model.Children.First(s => s.Uid == "CatLibrary.Cat`2.#ctor(`0)");
                Assert.Equal("<p sourcefile=\"TestData/overwrite/mref.overwrite.remarks.md\" sourcestartlinenumber=\"6\" sourceendlinenumber=\"6\">Remarks content</p>\n", method.Remarks);
            }
        }

        [Fact]
        public void ProcessMrefWithMultiUidOverwriteShouldSucceed()
        {
            var files = new FileCollection(_defaultFiles);
            files.Add(DocumentType.Overwrite, new[] { "TestData/overwrite/mref.overwrite.multi.uid.md" });
            BuildDocument(files);
            {
                var outputRawModelPath = GetRawModelFilePath("CatLibrary.Cat-2.yml");
                Assert.True(File.Exists(outputRawModelPath));
                var model = JsonUtility.Deserialize<ApiBuildOutput>(outputRawModelPath);
                Assert.Equal("<p sourcefile=\"TestData/overwrite/mref.overwrite.multi.uid.md\" sourcestartlinenumber=\"6\" sourceendlinenumber=\"6\">Overwrite content1</p>\n", model.Conceptual);
                Assert.Equal("<p sourcefile=\"TestData/overwrite/mref.overwrite.multi.uid.md\" sourcestartlinenumber=\"13\" sourceendlinenumber=\"13\">Overwrite &quot;content2&quot;</p>\n", model.Summary);
                Assert.Equal("<p sourcefile=\"TestData/overwrite/mref.overwrite.multi.uid.md\" sourcestartlinenumber=\"20\" sourceendlinenumber=\"20\">Overwrite &#39;content3&#39;</p>\n", model.Metadata["not_defined_property"]);
            }
        }

        [Fact]
        public void ProcessMrefModelIsSupportIncremental()
        {
            var ft = _defaultFiles.EnumerateFiles().First();
            var p = new ManagedReferenceDocumentProcessor();
            var expected = p.Load(ft, ImmutableDictionary<string, object>.Empty);
            var expectedVM = (PageViewModel)expected.Content;
            expectedVM.Metadata["a"] = new List<string> { "b" };
            expectedVM.Items[0].Metadata["a"] = new List<int> { 1 };
            var actual = new Func<FileModel>(
                () =>
                {
                    using (var ms = new MemoryStream())
                    {
                        p.SaveIntermediateModel(expected, ms);
                        ms.Position = 0;
                        return p.LoadIntermediateModel(ms);
                    }
                })();
            var actualVM = (PageViewModel)actual.Content;

            Assert.NotSame(expected, actual);
            Assert.NotSame(expectedVM, actualVM);

            Assert.Equal(from uid in expected.Uids select uid.Name, from uid in actual.Uids select uid.Name);
            Assert.Equal(expected.FileAndType, actual.FileAndType);
            Assert.Equal(expected.DocumentType, actual.DocumentType);
            Assert.Equal(expected.Key, actual.Key);
            Assert.Equal(expected.LocalPathFromRoot, actual.LocalPathFromRoot);
            Assert.Equal(expected.OriginalFileAndType, actual.OriginalFileAndType);

            Assert.Equal(expectedVM.Metadata["a"].GetType(), actualVM.Metadata["a"].GetType());
            Assert.Equal((List<string>)expectedVM.Metadata["a"], (List<string>)actualVM.Metadata["a"]);
            Assert.Equal(expectedVM.Items[0].Metadata["a"].GetType(), actualVM.Items[0].Metadata["a"].GetType());
            Assert.Equal((List<int>)expectedVM.Items[0].Metadata["a"], (List<int>)actualVM.Items[0].Metadata["a"]);
            Assert.Equal(expectedVM.Items[0].Names, actualVM.Items[0].Names);
            Assert.Equal(expectedVM.Items[0].NamesWithType, actualVM.Items[0].NamesWithType);
            Assert.Equal(expectedVM.Items[0].FullNames, actualVM.Items[0].FullNames);
            Assert.Equal(expectedVM.Items[0].Modifiers, actualVM.Items[0].Modifiers);
        }

        [Fact]
        public void SystemKeysListShouldBeComplete()
        {
            var files = new FileCollection(_defaultFiles);
            BuildDocument(files);
            var outputRawModelPath = GetRawModelFilePath("CatLibrary.Cat-2.yml");
            Assert.True(File.Exists(outputRawModelPath));
            var model = JsonUtility.Deserialize<Dictionary<string, object>>(outputRawModelPath);
            var systemKeys = (JArray)model[Constants.PropertyName.SystemKeys];
            Assert.NotEmpty(systemKeys);
            foreach (var key in model.Keys.Where(key => key[0] != '_' && key != "meta" && key != "anotherMeta"))
            {
                Assert.Contains(key, systemKeys);
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
        }

        private string GetRawModelFilePath(string fileName)
        {
            return Path.Combine(_outputFolder, MrefDirectory, Path.ChangeExtension(fileName, RawModelFileExtension));
        }

        private string GetOutputFilePath(string fileName)
        {
            return Path.GetFullPath(Path.Combine(_outputFolder, Path.ChangeExtension(fileName, "html")));
        }
    }
}
