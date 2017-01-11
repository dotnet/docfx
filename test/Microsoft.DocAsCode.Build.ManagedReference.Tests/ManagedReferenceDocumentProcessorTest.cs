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
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Tests.Common;

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
                RawModelExportSettings = {Export = true}
            };
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
            Assert.Equal("CatLibrary_Cat_2", model.Id);
            Assert.Equal(1, model.Attributes.Count);
            Assert.Equal("System.SerializableAttribute.#ctor", model.Attributes[0].Constructor);
            Assert.Equal(0, model.Attributes[0].Arguments.Count);
            Assert.Equal("System.SerializableAttribute", model.Attributes[0].Type);

            Assert.Equal(2, model.Implements.Count);
            Assert.Equal("CatLibrary_ICat", model.Implements[0].Id);
            Assert.Equal("CatLibrary_IAnimal", model.Implements[1].Id);

            Assert.Equal(1, model.Inheritance.Count);
            Assert.Equal("System_Object", model.Inheritance[0].Id);

            Assert.Equal(6, model.InheritedMembers.Count);
            Assert.Equal("System_Object_ToString", model.InheritedMembers[0].Id);
            Assert.Equal("System_Object_Equals_System_Object_System_Object_", model.InheritedMembers[1].Id);
            Assert.Equal("System_Object_ReferenceEquals_System_Object_System_Object_", model.InheritedMembers[2].Id);
            Assert.Equal("System_Object_GetHashCode", model.InheritedMembers[3].Id);
            Assert.Equal("System_Object_GetType", model.InheritedMembers[4].Id);
            Assert.Equal("System_Object_MemberwiseClone", model.InheritedMembers[5].Id);

            Assert.Equal(2, model.Syntax.Content.Count);
            Assert.Equal("csharp", model.Syntax.Content[0].Language);
            Assert.Equal("[Serializable]\npublic class Cat<T, K> : ICat, IAnimal where T : class, new ()where K : struct", model.Syntax.Content[0].Value);
            Assert.Equal("vb", model.Syntax.Content[1].Language);
            Assert.Equal("<Serializable>\nPublic Class Cat(Of T As {Class, New}, K As Structure)\n\n    Implements ICat, IAnimal", model.Syntax.Content[1].Value);

            Assert.Equal(2, model.Syntax.TypeParameters.Count);
            Assert.Equal("T", model.Syntax.TypeParameters[0].Name);
            Assert.Equal("<p sourcefile=\"TestData/mref/CatLibrary.Cat-2.yml\" sourcestartlinenumber=\"1\" sourceendlinenumber=\"1\">This type should be class and can new instance.</p>\n", model.Syntax.TypeParameters[0].Description);
            Assert.Equal("K", model.Syntax.TypeParameters[1].Name);
            Assert.Equal("<p sourcefile=\"TestData/mref/CatLibrary.Cat-2.yml\" sourcestartlinenumber=\"1\" sourceendlinenumber=\"1\">This type is a struct type, class type can&#39;t be used for this parameter.</p>\n", model.Syntax.TypeParameters[1].Description);

            Assert.Equal(20, model.Children.Count);
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
                }.ToImmutableDictionary()
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
    }
}
