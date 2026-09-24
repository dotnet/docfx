// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.DataContracts.ManagedReference;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Docfx.Dotnet.Tests;

[Collection("docfx STA")]
public class NamespaceReferenceTest
{
    [Theory]
    [InlineData("Foo.Bar.Baz", false)]
    [InlineData("Foo.Bar.Baz", true)]
    [InlineData("httpTools.Child", false)]
    [InlineData("httpTools.Child", true)]
    [InlineData("httpsTools.Child", false)]
    [InlineData("httpsTools.Child", true)]
    public void LocalNamespacesUseXrefs(string namespaceName, bool fromAssembly)
    {
        var (compilation, assembly) = Compile($"namespace {namespaceName} {{ public class Client {{ }} }}", fromAssembly);
        var reference = assembly.GenerateMetadataItem(compilation).References[namespaceName];
        var names = namespaceName.Split('.');
        var uids = Enumerable.Range(1, names.Length).Select(count => string.Join(".", names.Take(count))).ToArray();

        foreach (var parts in new[] { reference.NameParts, reference.NameWithTypeParts, reference.QualifiedNameParts })
        {
            Assert.Equal(2, parts.Count);
            foreach (var items in parts.Values)
            {
                Assert.Equal(uids, items.Where(item => item.Name != null).Select(item => item.Name));
                Assert.All(items, item => Assert.Null(item.Href));
            }
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ExternalNamespacesKeepUrls(bool fromAssembly)
    {
        var (compilation, assembly) = Compile(
            """
            namespace Foo.Bar
            {
                public class Client
                {
                    public System.Collections.Generic.List<int> GetValues() => null;
                }
            }
            """, fromAssembly);
        var reference = assembly.GenerateMetadataItem(compilation).References["System.Collections.Generic"];

        foreach (var items in reference.NameParts.Values)
        {
            Assert.All(items.Where(item => item.Name != null), item =>
                Assert.Equal($"https://learn.microsoft.com/dotnet/api/{item.Name.ToLowerInvariant()}", item.Href));
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OtherOutputFormatsKeepNamespaceUrls(bool markdown)
    {
        var (compilation, assembly) = Compile("namespace Foo.Bar { public class Client { } }", false);
        var symbol = assembly.GetTypeByMetadataName("Foo.Bar.Client").ContainingNamespace;
        var parts = SymbolFormatter.GetNameParts(symbol, SyntaxLanguage.CSharp).ToLinkItems(
            compilation, MemberLayout.SamePage, new([assembly], SymbolEqualityComparer.Default),
            overload: false, filter: new(new(), new()),
            urlKind: markdown ? SymbolUrlKind.Markdown : SymbolUrlKind.Html);
        var extension = markdown ? ".md" : ".html";

        Assert.Equal($"Foo{extension}", parts.Single(part => part.Name == "Foo").Href);
        Assert.Equal($"Foo.Bar{extension}", parts.Single(part => part.Name == "Foo.Bar").Href);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ConstructedNestedTypesKeepUrls(bool fromAssembly)
    {
        var (compilation, assembly) = Compile(
            """
            namespace Foo.Bar
            {
                public class Outer<T> { public class Inner { } }
                public class Derived : Outer<int>.Inner { }
            }
            """, fromAssembly);
        var output = assembly.GenerateMetadataItem(compilation);
        var allMembers = new Dictionary<string, MetadataItem>();
        foreach (var ns in output.Items)
            AddMember(ns);

        const string constructedUid = "Foo.Bar.Outer{System.Int32}.Inner";
        Assert.DoesNotContain(constructedUid, allMembers.Keys);
        Assert.Contains("Foo.Bar.Outer`1.Inner", allMembers.Keys);

        var model = YamlMetadataResolver.ResolveMetadata(allMembers, output.References, NamespaceLayout.Flattened);
        Assert.Contains(model.Members, member => member.Name == "Foo.Bar.Outer`1.Inner");
        var derivedPage = model.Members.Single(member => member.Name == "Foo.Bar.Derived");
        var reference = derivedPage.References[constructedUid];

        foreach (var parts in reference.NameParts.Values)
        {
            Assert.Equal("Foo.Bar.Outer-1.Inner.html", parts.Single(part => part.DisplayName == "Inner").Href);
        }
        var viewModel = derivedPage.ToPageViewModel(new()).References.Single(item => item.Uid == constructedUid);
        foreach (var parts in viewModel.Specs.Values)
        {
            Assert.Equal("Foo.Bar.Outer-1.Inner.html", parts.Single(part => part.Name == "Inner").Href);
        }

        void AddMember(MetadataItem member)
        {
            allMembers.Add(member.Name, member);
            foreach (var child in member.Items ?? [])
                AddMember(child);
        }
    }

    private static (Compilation, IAssemblySymbol) Compile(string code, bool fromAssembly)
    {
        var compilation = CompilationHelper.CreateCompilationFromCSharpCode(code, new Dictionary<string, string>(), "TestAssembly");
        if (!fromAssembly)
            return (compilation, compilation.Assembly);

        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        Assert.True(result.Success, string.Join(Environment.NewLine, result.Diagnostics));
        var reference = MetadataReference.CreateFromImage(stream.ToArray());
        compilation = CompilationHelper.CreateCompilationFromCSharpCode("", new Dictionary<string, string>(), "Consumer", reference);
        return (compilation, Assert.IsAssignableFrom<IAssemblySymbol>(compilation.GetAssemblyOrModuleSymbol(reference)));
    }
}
