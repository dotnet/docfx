// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference.Tests
{
    using System.Linq;

    using Xunit;

    using Microsoft.DocAsCode.DataContracts.ManagedReference;

    using static Microsoft.DocAsCode.Metadata.ManagedReference.RoslynIntermediateMetadataExtractor;

    [Collection("docfx STA")]
    public class GenerateMetadataFromAssemblyTest
    {
        public readonly string[] AssemblyFiles =
            new[]
            {
                @"TestData/BaseClassForTestClass1.dll",
                @"TestData/CatLibrary.dll",
                @"TestData/CatLibrary2.dll",
            };

        public readonly string[] TupleAssemblyFiles =
            new[]
            {
                @"TestData/TupleLibrary.dll",
            };

        public readonly string[] TupleReferencesFiles =
            new[]
            {
                @"TestDataReferences/System.ValueTuple.dll",
            };

        [Fact]
        public void TestGenerateMetadataFromAssembly()
        {
            var compilation = CompilationUtility.CreateCompilationFromAssembly(AssemblyFiles);
            var referenceAssembly = CompilationUtility.GetAssemblyFromAssemblyComplation(compilation, AssemblyFiles)
                .Select(s => s.assembly).OrderBy(s => s.Name).ToList();
            
            {
                var output = GenerateYamlMetadata(compilation, referenceAssembly[2]);
                var @class = output.Items[0].Items[2];
                Assert.NotNull(@class);
                Assert.Equal("Cat<T, K>", @class.DisplayNames.First().Value);
                Assert.Equal("Cat<T, K>", @class.DisplayNamesWithType.First().Value);
                Assert.Equal("CatLibrary.Cat<T, K>", @class.DisplayQualifiedNames.First().Value);
            }

            {
                var output = GenerateYamlMetadata(compilation, referenceAssembly[1]);
                var @class = output.Items[0].Items[0];
                Assert.NotNull(@class);
                Assert.Equal("CarLibrary2.Cat2", @class.Name);
                Assert.Equal(new[] { "System.Object", "CatLibrary.Cat{CatLibrary.Dog{System.String},System.Int32}" }, @class.Inheritance);
            }
        }

        [Fact]
        public void TestGenerateMetadataFromAssemblyWithReferences()
        {
            var compilation = CompilationUtility.CreateCompilationFromAssembly(TupleAssemblyFiles.Concat(TupleReferencesFiles));
            var referenceAssembly = CompilationUtility.GetAssemblyFromAssemblyComplation(compilation, TupleAssemblyFiles).Select(s => s.assembly).ToList();

            var output = GenerateYamlMetadata(compilation, referenceAssembly[0]);
            var @class = output.Items[0].Items[0];
            Assert.NotNull(@class);
            Assert.Equal("XmlTasks", @class.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.Equal("XmlTasks", @class.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.Equal("TupleLibrary.XmlTasks", @class.DisplayQualifiedNames[SyntaxLanguage.CSharp]);

            {
                var method = @class.Items.Single(i => i.Name == "TupleLibrary.XmlTasks.ToNamespace(System.String,System.String)");
                Assert.Equal("ToNamespace(string, string)", method.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("XmlTasks.ToNamespace(string, string)", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("TupleLibrary.XmlTasks.ToNamespace(string, string)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);

                Assert.Equal("public (string prefix, string uri) ToNamespace(string prefix, string uri)", method.Syntax.Content[SyntaxLanguage.CSharp]);
            }

            {
                var method = @class.Items.Single(i => i.Name == "TupleLibrary.XmlTasks.XmlPeek(System.String,System.ValueTuple{System.String,System.String}[])");
                Assert.Equal("XmlPeek(string, params (string prefix, string uri)[])", method.DisplayNames[SyntaxLanguage.CSharp]);
                Assert.Equal("XmlTasks.XmlPeek(string, params (string prefix, string uri)[])", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
                Assert.Equal("TupleLibrary.XmlTasks.XmlPeek(string, params (string prefix, string uri)[])", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);

                Assert.Equal("public string XmlPeek(string xpath, params (string prefix, string uri)[] namespaces)", method.Syntax.Content[SyntaxLanguage.CSharp]);
            }
        }
    }
}
