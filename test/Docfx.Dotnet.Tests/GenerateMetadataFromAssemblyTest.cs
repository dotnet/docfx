// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.DataContracts.ManagedReference;
using Xunit;

namespace Docfx.Dotnet.Tests;

[Collection("docfx STA")]
public class GenerateMetadataFromAssemblyTest
{
    [Fact]
    public void TestGenerateMetadataFromAssembly()
    {
        {
            var (compilation, assembly) = CompilationHelper.CreateCompilationFromAssembly("TestData/CatLibrary.dll");
            Assert.Empty(compilation.GetDeclarationDiagnostics());

            var output = assembly.GenerateMetadataItem(compilation);
            var @class = output.Items[0].Items[2];
            Assert.NotNull(@class);
            Assert.Equal("Cat<T, K>", @class.DisplayNames.First().Value);
            Assert.Equal("Cat<T, K>", @class.DisplayNamesWithType.First().Value);
            Assert.Equal("CatLibrary.Cat<T, K>", @class.DisplayQualifiedNames.First().Value);
        }

        {
            var (compilation, assembly) = CompilationHelper.CreateCompilationFromAssembly("TestData/CatLibrary2.dll");
            Assert.Empty(compilation.GetDeclarationDiagnostics());

            var output = assembly.GenerateMetadataItem(compilation);
            var @class = output.Items[0].Items[0];
            Assert.NotNull(@class);
            Assert.Equal("CarLibrary2.Cat2", @class.Name);
            Assert.Equal(new[] { "System.Object", "CatLibrary.Cat{CatLibrary.Dog{System.String},System.Int32}" }, @class.Inheritance);
        }
    }

    [Fact]
    public void TestGenerateMetadataFromAssemblyWithReferences()
    {
        var (compilation, assembly) = CompilationHelper.CreateCompilationFromAssembly("TestData/TupleLibrary.dll");
        Assert.Empty(compilation.GetDeclarationDiagnostics());

        var output = assembly.GenerateMetadataItem(compilation);
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

            Assert.Equal("public (string, string) ToNamespace(string prefix, string uri)", method.Syntax.Content[SyntaxLanguage.CSharp]);
        }

        {
            var method = @class.Items.Single(i => i.Name == "TupleLibrary.XmlTasks.XmlPeek(System.String,System.ValueTuple{System.String,System.String}[])");
            Assert.Equal("XmlPeek(string, params (string, string)[])", method.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.Equal("XmlTasks.XmlPeek(string, params (string, string)[])", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.Equal("TupleLibrary.XmlTasks.XmlPeek(string, params (string, string)[])", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);

            Assert.Equal("public string XmlPeek(string xpath, params (string, string)[] namespaces)", method.Syntax.Content[SyntaxLanguage.CSharp]);
        }
    }
}
