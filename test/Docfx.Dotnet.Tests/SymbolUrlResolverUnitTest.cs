// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Docfx.Common.Git;
using Docfx.Tests.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Xunit;

namespace Docfx.Dotnet.Tests;

public class SymbolUrlResolverUnitTest : TestBase
{
    [Fact]
    public void GetMicrosoftLearnUrlFromCommentIdTest()
    {
        var failures = new List<(string, Uri, Uri[])>();

        // The xrefmap for https://github.com/dotnet/dotnet-api-docs
        // downloaded and processed from https://learn.microsoft.com/en-us/dotnet/.xrefmap.json
        foreach (var line in File.ReadAllLines("TestData/dotnet-xrefmap.txt"))
        {
            var split = line.Split('\t');
            var commentId = split[0];
            var expectedUrl = new Uri($"https://learn.microsoft.com/dotnet/api/{split[1]}");

            var actualUrls = commentId[0] switch
            {
                // Enum fields and ordinary fields have different URL schemas
                'F' => new[]
                {
                    new Uri(SymbolUrlResolver.GetMicrosoftLearnUrl(commentId, isEnumMember: false, hasOverloads: false)),
                    new Uri(SymbolUrlResolver.GetMicrosoftLearnUrl(commentId, isEnumMember: true, hasOverloads: false)),
                },

                // Overload methods and ordinary methods have different URL schemas
                'M' =>
                [
                    new Uri(SymbolUrlResolver.GetMicrosoftLearnUrl(commentId, isEnumMember: false, hasOverloads: false)),
                    new Uri(SymbolUrlResolver.GetMicrosoftLearnUrl(commentId, isEnumMember: false, hasOverloads: true)),
                ],

                _ =>
                [
                    new Uri(SymbolUrlResolver.GetMicrosoftLearnUrl(commentId, isEnumMember: false, hasOverloads: false)),
                ],
            };

            if (!actualUrls.Contains(expectedUrl))
            {
                failures.Add((commentId, expectedUrl, actualUrls));
            }
        }

        // Ignore these remaining edge cases:
        //
        // - When UID differs only by case, mslearn appends _1 to disambiguate.
        // - When > 180 chars, type name is used over fully-qualified names to reduce URL length.
        Assert.Equal(56, failures.Count);
    }

    [Theory]
    [InlineData("A.b[]", "a-b()")]
    [InlineData("a b", "a-b")]
    [InlineData("a\"b", "ab")]
    [InlineData("a%b", "ab")]
    [InlineData("a^b", "ab")]
    [InlineData("a\\b", "ab")]
    [InlineData("Dictionary<string, List<int>>*", "dictionary(string-list(int))*")]
    [InlineData("a'b'c", "abc")]
    [InlineData("{a|b_c'}", "((a-b-c))")]
    [InlineData("---&&$$##List<string> test(int a`, int a@, string b*)---&&$$##", "list(string)-test(int-a-int-a@-string-b*)")]
    [InlineData(
        "Microsoft.StreamProcessing.Streamable.AggregateByKey``4(Microsoft.StreamProcessing.IStreamable{Microsoft.StreamProcessing.Empty,``0},System.Linq.Expressions.Expression{System.Func{``0,``1}},Microsoft.StreamProcessing.Aggregates.IAggregate{``0,``22,``23}},Microsoft.StreamProcessing.Aggregates.IAggregate{``0,``30,``31}},System.Linq.Expressions.Expression{System.Func{``3,``5,``7,``9,``11,``13,``15,``17,``19,``21,``23,``25,``27,``29,``31,``32}})",
        "microsoft-streamprocessing-streamable-aggregatebykey-4(microsoft-streamprocessing-istreamable((microsoft-streamprocessing-empty-0))-system-linq-expressions-expression((system-func((-0-1))))-microsoft-streamprocessing-aggregates-iaggregate((-0-22-23))))-microsoft-streamprocessing-aggregates-iaggregate((-0-30-31))))-system-linq-expressions-expression((system-func((-3-5-7-9-11-13-15-17-19-21-23-25-27-29-31-32)))))")]
    public static void GetUrlFragmentFromUidTest(string uid, string expectedFragment)
    {
        Assert.Equal(expectedFragment, SymbolUrlResolver.GetUrlFragmentFromUid(uid));
    }

    [Fact]
    public static void GetPdbSourceLinkUrlTest()
    {
        var (compilation, assembly) = CompilationHelper.CreateCompilationFromAssembly($"{typeof(DotnetApiCatalog).Assembly.GetName().Name}.dll");

        var type = assembly.GetTypeByMetadataName(typeof(DotnetApiCatalog).FullName);
        Assert.NotNull(type);
        var compilationLink = ReplaceSHA(SymbolUrlResolver.GetPdbSourceLinkUrl(compilation, type));
        Assert.True(compilationLink?.StartsWith("https://github.com/"));
        Assert.True(compilationLink?.EndsWith(".cs"));

        var method = type.GetMembers(nameof(DotnetApiCatalog.GenerateManagedReferenceYamlFiles)).FirstOrDefault();
        Assert.NotNull(method);
        var methodLink = ReplaceSHA(SymbolUrlResolver.GetPdbSourceLinkUrl(compilation, method));
        Assert.True(compilationLink?.StartsWith("https://github.com/"));
        Assert.True(compilationLink?.EndsWith(".cs"));

        static string ReplaceSHA(string value)
        {
            return Regex.Replace(value, "\\/[0-9a-zA-Z]{40}\\/", "/*/");
        }
    }

    [Theory]
    [InlineData("", "NetCord/obj/Release/net10.0/MethodsForPropertiesGenerator/MethodsForPropertiesGenerator.MethodsForPropertiesGenerator/NetCord.Rest.MessageProperties.g.cs", false)]
    [InlineData("/repo/", "obj/Generated.g.cs", false)]
    [InlineData(@"C:\repo\", @"obj\Generated.g.cs", false)]
    [InlineData("", "obj/Generated.g.cs", false)]
    [InlineData("", @"obj\Generated.g.cs", false)]
    [InlineData(@"C:\repo/", @"obj\Generated.g.cs", false)]
    [InlineData(@"/repo\", "obj/Generated.g.cs", false)]
    [InlineData("/repo/", "generated/Generated.g.cs", true)]
    [InlineData(@"C:\repo\", @"generated\Generated.g.cs", true)]
    [InlineData("/repo/", "objects/Generated.g.cs", true)]
    [InlineData("", "obj.cs", true)]
    [InlineData("/repo/", "obj.Generated.g.cs", true)]
    [InlineData("/repo/", "Obj/Generated.g.cs", false)]
    public void GetPdbSourceLinkUrlWithConfiguredExclusions(string prefix, string relativePath, bool expectSourceLink)
    {
        const string rawUrl = "https://raw.githubusercontent.com/dotnet/docfx/0123456789abcdef0123456789abcdef01234567/";
        var (compilation, assembly) = CreateAssemblyWithSourceLink(prefix + relativePath, new()
        {
            [prefix + "*"] = rawUrl + "*",
            ["/NetCord/Rest/MessageProperties.cs"] = rawUrl + "NetCord/Rest/MessageProperties.cs",
        });

        var type = assembly.GetTypeByMetadataName("NetCord.Rest.MessageProperties");
        Assert.NotNull(type);
        var property = Assert.Single(type.GetMembers("Content"));
        var method = Assert.Single(type.GetMembers("WithContent"));
        Assert.Empty(method.DeclaringSyntaxReferences);
        Assert.True(method.Locations[0].IsInMetadata);

        var handwrittenUrl = GitUtility.RawContentUrlToContentUrl(rawUrl + "NetCord/Rest/MessageProperties.cs");
        Assert.Equal(handwrittenUrl, SymbolUrlResolver.GetPdbSourceLinkUrl(compilation, property));
        Assert.Equal(handwrittenUrl, VisitorHelper.GetSourceDetail(property, compilation)?.Href);

        var unfilteredUrl = GitUtility.RawContentUrlToContentUrl(rawUrl + relativePath.Replace('\\', '/'));
        Assert.Equal(unfilteredUrl, SymbolUrlResolver.GetPdbSourceLinkUrl(compilation, method));
        var filter = new SourceLinkFilter(["**/obj/**"]);
        var expectedUrl = expectSourceLink ? unfilteredUrl : null;
        Assert.Equal(expectedUrl, SymbolUrlResolver.GetPdbSourceLinkUrl(compilation, method, filter));
        Assert.Equal(expectedUrl, VisitorHelper.GetSourceDetail(method, compilation, filter)?.Href);
        if (!expectSourceLink)
        {
            Assert.Null(VisitorHelper.GetSourceDetail(method, compilation, filter));
            Assert.Equal(handwrittenUrl, SymbolUrlResolver.GetPdbSourceLinkUrl(compilation, type, filter));
        }
        Assert.Equal(unfilteredUrl, SymbolUrlResolver.GetPdbSourceLinkUrl(compilation, method));
    }

    [Fact]
    public void GetPdbSourceLinkUrlWithoutMatchingDocument()
    {
        var (compilation, assembly) = CreateAssemblyWithSourceLink("NetCord/obj/Generated.g.cs", new()
        {
            ["/NetCord/*"] = "https://raw.githubusercontent.com/dotnet/docfx/0123456789abcdef0123456789abcdef01234567/NetCord/*",
        });
        var type = assembly.GetTypeByMetadataName("NetCord.Rest.MessageProperties");
        Assert.NotNull(type);
        Assert.NotNull(VisitorHelper.GetSourceDetail(Assert.Single(type.GetMembers("Content")), compilation)?.Href);
        Assert.Null(VisitorHelper.GetSourceDetail(Assert.Single(type.GetMembers("WithContent")), compilation));
    }

    private (Compilation, IAssemblySymbol) CreateAssemblyWithSourceLink(string generatedDocument, Dictionary<string, string> documents)
    {
        var handwrittenTree = CSharpSyntaxTree.ParseText(
            """
            namespace NetCord.Rest;
            public partial class MessageProperties
            {
                public string Content { get; set; }
            }
            """, path: "/NetCord/Rest/MessageProperties.cs", encoding: Encoding.UTF8);
        var generatedTree = CSharpSyntaxTree.ParseText(
            """
            namespace NetCord.Rest;
            public partial class MessageProperties
            {
                public MessageProperties WithContent(string content)
                {
                    Content = content;
                    return this;
                }
            }
            """, path: generatedDocument, encoding: Encoding.UTF8);
        var sourceCompilation = CompilationHelper.CreateCompilationFromCSharpCode("", new Dictionary<string, string>(), "SourceLinkTest")
            .RemoveAllSyntaxTrees()
            .AddSyntaxTrees(handwrittenTree, generatedTree);

        using var peStream = new MemoryStream();
        using var pdbStream = new MemoryStream();
        using var sourceLinkStream = new MemoryStream(JsonSerializer.SerializeToUtf8Bytes(new { documents }));
        var result = sourceCompilation.Emit(peStream, pdbStream,
            options: new EmitOptions(debugInformationFormat: DebugInformationFormat.PortablePdb),
            sourceLinkStream: sourceLinkStream,
            embeddedTexts: [EmbeddedText.FromSource(generatedDocument, generatedTree.GetText())]);
        Assert.True(result.Success, string.Join(Environment.NewLine, result.Diagnostics));

        pdbStream.Position = 0;
        using var pdbReaderProvider = MetadataReaderProvider.FromPortablePdbStream(pdbStream, MetadataStreamOptions.LeaveOpen);
        var pdbReader = pdbReaderProvider.GetMetadataReader();
        Assert.Contains(generatedDocument, pdbReader.Documents.Select(handle => pdbReader.GetString(pdbReader.GetDocument(handle).Name)));

        var assemblyPath = Path.GetFullPath(Path.Combine(GetRandomFolder(), "SourceLinkTest.dll"));
        File.WriteAllBytes(assemblyPath, peStream.ToArray());
        File.WriteAllBytes(Path.ChangeExtension(assemblyPath, ".pdb"), pdbStream.ToArray());
        return CompilationHelper.CreateCompilationFromAssembly(assemblyPath);
    }
}
