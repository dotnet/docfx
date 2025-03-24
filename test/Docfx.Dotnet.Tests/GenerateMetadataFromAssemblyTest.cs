// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.DataContracts.ManagedReference;

namespace Docfx.Dotnet.Tests;

[DoNotParallelize]
[TestClass]
public class GenerateMetadataFromAssemblyTest
{
    [TestMethod]
    public void TestGenerateMetadataFromAssembly()
    {
        {
            var (compilation, assembly) = CompilationHelper.CreateCompilationFromAssembly("TestData/CatLibrary.dll");
            Assert.IsEmpty(compilation.GetDeclarationDiagnostics());

            var output = assembly.GenerateMetadataItem(compilation);
            var @class = output.Items[0].Items[2];
            Assert.IsNotNull(@class);
            Assert.AreEqual("Cat<T, K>", @class.DisplayNames.First().Value);
            Assert.AreEqual("Cat<T, K>", @class.DisplayNamesWithType.First().Value);
            Assert.AreEqual("CatLibrary.Cat<T, K>", @class.DisplayQualifiedNames.First().Value);
        }

        {
            var (compilation, assembly) = CompilationHelper.CreateCompilationFromAssembly("TestData/CatLibrary2.dll");
            Assert.IsEmpty(compilation.GetDeclarationDiagnostics());

            var output = assembly.GenerateMetadataItem(compilation);
            var @class = output.Items[0].Items[0];
            Assert.IsNotNull(@class);
            Assert.AreEqual("CarLibrary2.Cat2", @class.Name);
            Assert.AreEqual(new[] { "System.Object", "CatLibrary.Cat{CatLibrary.Dog{System.String},System.Int32}" }, @class.Inheritance.ToArray());
        }
    }

    [TestMethod]
    public void TestGenerateMetadataFromAssemblyWithReferences()
    {
        var (compilation, assembly) = CompilationHelper.CreateCompilationFromAssembly("TestData/TupleLibrary.dll");
        Assert.IsEmpty(compilation.GetDeclarationDiagnostics());

        var output = assembly.GenerateMetadataItem(compilation);
        var @class = output.Items[0].Items[0];
        Assert.IsNotNull(@class);
        Assert.AreEqual("XmlTasks", @class.DisplayNames[SyntaxLanguage.CSharp]);
        Assert.AreEqual("XmlTasks", @class.DisplayNamesWithType[SyntaxLanguage.CSharp]);
        Assert.AreEqual("TupleLibrary.XmlTasks", @class.DisplayQualifiedNames[SyntaxLanguage.CSharp]);

        {
            var method = @class.Items.Single(i => i.Name == "TupleLibrary.XmlTasks.ToNamespace(System.String,System.String)");
            Assert.AreEqual("ToNamespace(string, string)", method.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("XmlTasks.ToNamespace(string, string)", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("TupleLibrary.XmlTasks.ToNamespace(string, string)", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);

            Assert.AreEqual("public (string, string) ToNamespace(string prefix, string uri)", method.Syntax.Content[SyntaxLanguage.CSharp]);
        }

        {
            var method = @class.Items.Single(i => i.Name == "TupleLibrary.XmlTasks.XmlPeek(System.String,System.ValueTuple{System.String,System.String}[])");
            Assert.AreEqual("XmlPeek(string, params (string, string)[])", method.DisplayNames[SyntaxLanguage.CSharp]);
            Assert.AreEqual("XmlTasks.XmlPeek(string, params (string, string)[])", method.DisplayNamesWithType[SyntaxLanguage.CSharp]);
            Assert.AreEqual("TupleLibrary.XmlTasks.XmlPeek(string, params (string, string)[])", method.DisplayQualifiedNames[SyntaxLanguage.CSharp]);

            Assert.AreEqual("public string XmlPeek(string xpath, params (string, string)[] namespaces)", method.Syntax.Content[SyntaxLanguage.CSharp]);
        }
    }
}
