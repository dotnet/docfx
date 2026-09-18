// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Compression;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;
using Docfx.Common;
using Docfx.Common.Git;
using Docfx.DataContracts.ManagedReference;
using Docfx.Tests.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Debugging;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using Microsoft.SourceLink.Tools;
using Xunit;
using Xunit.Abstractions;

namespace Docfx.Dotnet.Tests;

[Collection("docfx STA")]
public class SourceLinkGeneratorReproTest(ITestOutputHelper output) : TestBase
{
    private const string HandwrittenPath = "/repo/MessageProperties.cs";
    private const string RawUrl = "https://raw.githubusercontent.com/dotnet/docfx/0123456789abcdef0123456789abcdef01234567/";

    [Theory]
    [InlineData("none", false)]
    [InlineData("none", true)]
    [InlineData("wildcard", false)]
    [InlineData("wildcard", true)]
    [InlineData("explicit", false)]
    [InlineData("explicit", true)]
    public void GeneratedMemberSourceLinksCanBeExcludedWithoutRemovingApis(string mapping, bool embedHandwritten)
    {
        var (compilation, generatedTree) = RunGenerator(HandwrittenPath);
        var documents = new Dictionary<string, string>
        {
            [HandwrittenPath] = RawUrl + "MessageProperties.cs",
        };
        if (mapping == "wildcard")
            documents["*"] = RawUrl + "*";
        else if (mapping == "explicit")
            documents[generatedTree.FilePath] = RawUrl + "checked-in/MessageProperties.g.cs";

        var emitted = Emit(compilation, generatedTree, documents, embedHandwritten);
        var resolvedDocuments = InspectPdb(emitted.Pdb);
        var generated = resolvedDocuments[generatedTree.FilePath];
        Assert.True(generated.IsEmbedded);
        Assert.Contains("WithContent", generated.EmbeddedText);
        Assert.Equal(mapping != "none", generated.MappedUrl is not null);
        Assert.Equal(embedHandwritten, resolvedDocuments[HandwrittenPath].IsEmbedded);

        var (loadedCompilation, assembly) = LoadAssembly(emitted);
        var type = assembly.GetTypeByMetadataName("NetCord.Rest.MessageProperties");
        Assert.NotNull(type);
        var property = Assert.Single(type.GetMembers("Content"));
        var method = Assert.Single(type.GetMembers("WithContent"));
        Assert.Empty(method.DeclaringSyntaxReferences);
        Assert.True(method.Locations[0].IsInMetadata);

        var handwrittenUrl = GitUtility.RawContentUrlToContentUrl(RawUrl + "MessageProperties.cs");
        var methodUrl = VisitorHelper.GetSourceDetail(method, loadedCompilation)?.Href;
        output.WriteLine($"DocFX generated-member URL: {methodUrl ?? "<none>"}");
        Assert.Equal(handwrittenUrl, VisitorHelper.GetSourceDetail(property, loadedCompilation)?.Href);
        Assert.Equal(generated.MappedUrl is null ? null : GitUtility.RawContentUrlToContentUrl(generated.MappedUrl), methodUrl);

        var filter = new SourceLinkFilter(["**/*WithContentGenerator/**"]);
        Assert.Null(VisitorHelper.GetSourceDetail(method, loadedCompilation, filter));
        Assert.Equal(handwrittenUrl, VisitorHelper.GetSourceDetail(property, loadedCompilation, filter)?.Href);
        Assert.Equal(handwrittenUrl, SymbolUrlResolver.GetPdbSourceLinkUrl(loadedCompilation, type, filter));

        var metadata = assembly.GenerateMetadataItem(loadedCompilation, new() { SourceLinkFilter = filter });
        var classMetadata = Assert.Single(Assert.Single(metadata.Items).Items);
        var methodMetadata = Assert.Single(classMetadata.Items, item => item.Name == "NetCord.Rest.MessageProperties.WithContent(System.String)");
        Assert.Null(methodMetadata.Source);
        Assert.Contains("WithContent", methodMetadata.Syntax.Content[SyntaxLanguage.CSharp]);
        var propertyMetadata = Assert.Single(classMetadata.Items, item => item.Name == "NetCord.Rest.MessageProperties.Content");
        Assert.Equal(handwrittenUrl, propertyMetadata.Source.Href);

        Assert.Equal(methodUrl, VisitorHelper.GetSourceDetail(method, loadedCompilation)?.Href);
        Assert.Equal(methodUrl, VisitorHelper.GetSourceDetail(method, loadedCompilation, new(["https://raw.githubusercontent.com/**"]))?.Href);
        Assert.Null(VisitorHelper.GetSourceDetail(property, loadedCompilation, new(["**/MessageProperties.cs"])));
        Assert.Equal(handwrittenUrl, VisitorHelper.GetSourceDetail(property, loadedCompilation, new([]))?.Href);
    }

    [Fact]
    public void HandwrittenSourceUnderObjKeepsItsExplicitMapping()
    {
        const string handwrittenPath = "/repo/obj/MessageProperties.cs";
        var (compilation, generatedTree) = RunGenerator(handwrittenPath);
        var emitted = Emit(compilation, generatedTree, new()
        {
            [handwrittenPath] = RawUrl + "obj/MessageProperties.cs",
        }, embedHandwritten: false);
        var documents = InspectPdb(emitted.Pdb);
        Assert.False(documents[handwrittenPath].IsEmbedded);
        Assert.Equal(RawUrl + "obj/MessageProperties.cs", documents[handwrittenPath].MappedUrl);

        var (loadedCompilation, assembly) = LoadAssembly(emitted);
        var type = assembly.GetTypeByMetadataName("NetCord.Rest.MessageProperties");
        Assert.NotNull(type);
        var property = Assert.Single(type.GetMembers("Content"));
        Assert.Equal(GitUtility.RawContentUrlToContentUrl(RawUrl + "obj/MessageProperties.cs"),
            VisitorHelper.GetSourceDetail(property, loadedCompilation)?.Href);
        Assert.Null(VisitorHelper.GetSourceDetail(property, loadedCompilation, new(["**/obj/**"])));
    }

    [Fact]
    public void EmbeddedHandwrittenSourceKeepsItsWildcardMapping()
    {
        var (compilation, generatedTree) = RunGenerator(HandwrittenPath);
        var emitted = Emit(compilation, generatedTree, new()
        {
            ["/repo/*"] = RawUrl + "*",
        }, embedHandwritten: true);
        var documents = InspectPdb(emitted.Pdb);
        Assert.True(documents[HandwrittenPath].IsEmbedded);
        Assert.Equal(RawUrl + "MessageProperties.cs", documents[HandwrittenPath].MappedUrl);

        var (loadedCompilation, assembly) = LoadAssembly(emitted);
        var type = assembly.GetTypeByMetadataName("NetCord.Rest.MessageProperties");
        Assert.NotNull(type);
        var property = Assert.Single(type.GetMembers("Content"));
        Assert.Equal(GitUtility.RawContentUrlToContentUrl(RawUrl + "MessageProperties.cs"),
            VisitorHelper.GetSourceDetail(property, loadedCompilation)?.Href);
        Assert.Null(VisitorHelper.GetSourceDetail(property, loadedCompilation, new(["**/MessageProperties.cs"])));
    }

    [Fact]
    public async Task MetadataConfigurationExcludesOnlySelectedSourceLinks()
    {
        var (compilation, generatedTree) = RunGenerator(HandwrittenPath);
        var emitted = Emit(compilation, generatedTree, new()
        {
            ["*"] = RawUrl + "*",
            [HandwrittenPath] = RawUrl + "MessageProperties.cs",
        }, embedHandwritten: false);
        var folder = Path.GetFullPath(GetRandomFolder());
        File.WriteAllBytes(Path.Combine(folder, "GeneratorSourceLinkTest.dll"), emitted.Pe);
        File.WriteAllBytes(Path.Combine(folder, "GeneratorSourceLinkTest.pdb"), emitted.Pdb);
        var configPath = Path.Combine(folder, "docfx.json");
        await File.WriteAllTextAsync(configPath, JsonSerializer.Serialize(new
        {
            metadata = new[]
            {
                new { src = new[] { new { files = new[] { "GeneratorSourceLinkTest.dll" } } }, dest = "default", sourceLinkExclude = (string[])null },
                new { src = new[] { new { files = new[] { "GeneratorSourceLinkTest.dll" } } }, dest = "excluded", sourceLinkExclude = new[] { "**/*WithContentGenerator/**" } },
                new { src = new[] { new { files = new[] { "GeneratorSourceLinkTest.dll" } } }, dest = "empty", sourceLinkExclude = Array.Empty<string>() },
            },
        }));

        await DotnetApiCatalog.GenerateManagedReferenceYamlFiles(configPath);

        foreach (var dest in new[] { "default", "excluded", "empty" })
        {
            var page = YamlUtility.Deserialize<PageViewModel>(Path.Combine(folder, dest, "NetCord.Rest.MessageProperties.yml"));
            var method = Assert.Single(page.Items, item => item.Uid == "NetCord.Rest.MessageProperties.WithContent(System.String)");
            var property = Assert.Single(page.Items, item => item.Uid == "NetCord.Rest.MessageProperties.Content");
            Assert.Equal(GitUtility.RawContentUrlToContentUrl(RawUrl + "MessageProperties.cs"), property.Source.Href);
            Assert.Equal(dest != "excluded", method.Source?.Href is not null);
            Assert.Contains("WithContent", method.Syntax.Content);
        }
    }

    [Fact]
    public void GeneratorOutputAndEquivalentInputSourceHaveIdenticalPortablePdbs()
    {
        var (compilation, generatedTree) = RunGenerator(HandwrittenPath);
        var inputTree = CSharpSyntaxTree.ParseText(generatedTree.GetText(),
            (CSharpParseOptions)generatedTree.Options, generatedTree.FilePath);
        var inputCompilation = compilation.ReplaceSyntaxTree(generatedTree, inputTree);
        var documents = new Dictionary<string, string> { ["*"] = RawUrl + "*" };

        var generated = Emit(compilation, generatedTree, documents, embedHandwritten: false);
        var input = Emit(inputCompilation, inputTree, documents, embedHandwritten: false);

        Assert.Equal(generated.Pe, input.Pe);
        Assert.Equal(generated.Pdb, input.Pdb);
        output.WriteLine("Generator-produced and equivalent parsed input source emitted byte-identical DLLs and portable PDBs.");
    }

    private static (Compilation Compilation, SyntaxTree GeneratedTree) RunGenerator(string handwrittenPath)
    {
        var tree = CSharpSyntaxTree.ParseText(
            """
            namespace NetCord.Rest;
            public partial class MessageProperties
            {
                public string Content { get; set; }
            }
            """, path: handwrittenPath, encoding: Encoding.UTF8);
        var input = CompilationHelper.CreateCompilationFromCSharpCode("", new Dictionary<string, string>(), "GeneratorSourceLinkTest")
            .RemoveAllSyntaxTrees()
            .AddSyntaxTrees(tree);
        input = input.WithOptions(input.Options.WithDeterministic(true));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new WithContentGenerator().AsSourceGenerator());
        driver = driver.RunGeneratorsAndUpdateCompilation(input, out var compilation, out var diagnostics);
        Assert.Empty(diagnostics);
        var generatedTree = Assert.Single(driver.GetRunResult().GeneratedTrees);
        Assert.Contains("WithContent", generatedTree.GetText().ToString());
        Assert.DoesNotContain("WithContent", tree.GetText().ToString());
        return (compilation, generatedTree);
    }

    private static (byte[] Pe, byte[] Pdb) Emit(Compilation compilation, SyntaxTree generatedTree,
        Dictionary<string, string> documents, bool embedHandwritten)
    {
        // The command-line compiler embeds generator output; GeneratorDriver alone does not emit a PDB.
        var embeddedTexts = new List<EmbeddedText>
        {
            EmbeddedText.FromSource(generatedTree.FilePath, generatedTree.GetText()),
        };
        if (embedHandwritten)
        {
            var handwrittenTree = compilation.SyntaxTrees.First();
            embeddedTexts.Add(EmbeddedText.FromSource(handwrittenTree.FilePath, handwrittenTree.GetText()));
        }

        using var pe = new MemoryStream();
        using var pdb = new MemoryStream();
        using var sourceLink = new MemoryStream(JsonSerializer.SerializeToUtf8Bytes(new { documents }));
        var result = compilation.Emit(pe, pdb,
            options: new EmitOptions(debugInformationFormat: DebugInformationFormat.PortablePdb),
            sourceLinkStream: sourceLink, embeddedTexts: embeddedTexts);
        Assert.True(result.Success, string.Join(Environment.NewLine, result.Diagnostics));
        return (pe.ToArray(), pdb.ToArray());
    }

    private Dictionary<string, (bool IsEmbedded, string EmbeddedText, string MappedUrl)> InspectPdb(byte[] pdb)
    {
        using var stream = new MemoryStream(pdb);
        using var provider = MetadataReaderProvider.FromPortablePdbStream(stream);
        var reader = provider.GetMetadataReader();
        var sourceLink = Assert.Single(reader.GetCustomDebugInformation(EntityHandle.ModuleDefinition)
            .Select(reader.GetCustomDebugInformation),
            info => reader.GetGuid(info.Kind) == PortableCustomDebugInfoKinds.SourceLink);
        var map = SourceLinkMap.Parse(Encoding.UTF8.GetString(reader.GetBlobBytes(sourceLink.Value)));
        var documents = new Dictionary<string, (bool IsEmbedded, string EmbeddedText, string MappedUrl)>();

        foreach (var handle in reader.Documents)
        {
            var name = reader.GetString(reader.GetDocument(handle).Name);
            var embeddedInfo = reader.GetCustomDebugInformation(handle)
                .Select(reader.GetCustomDebugInformation)
                .Where(info => reader.GetGuid(info.Kind) == PortableCustomDebugInfoKinds.EmbeddedSource)
                .ToArray();
            string embeddedText = null;
            if (embeddedInfo.Length != 0)
            {
                var blob = reader.GetBlobReader(Assert.Single(embeddedInfo).Value);
                var uncompressedSize = blob.ReadInt32();
                var bytes = blob.ReadBytes(blob.RemainingBytes);
                if (uncompressedSize != 0)
                {
                    using var compressed = new MemoryStream(bytes);
                    using var deflate = new DeflateStream(compressed, CompressionMode.Decompress);
                    using var decompressed = new MemoryStream();
                    deflate.CopyTo(decompressed);
                    bytes = decompressed.ToArray();
                    Assert.Equal(uncompressedSize, bytes.Length);
                }
                embeddedText = Encoding.UTF8.GetString(bytes);
            }
            map.TryGetUri(name, out var uri);
            output.WriteLine($"PDB document: {name}; embedded: {embeddedText is not null}; raw mapped URL: {uri ?? "<none>"}");
            documents.Add(name, (embeddedText is not null, embeddedText, uri));
        }
        return documents;
    }

    private (Compilation, IAssemblySymbol) LoadAssembly((byte[] Pe, byte[] Pdb) emitted)
    {
        var path = Path.GetFullPath(Path.Combine(GetRandomFolder(), "GeneratorSourceLinkTest.dll"));
        File.WriteAllBytes(path, emitted.Pe);
        File.WriteAllBytes(Path.ChangeExtension(path, ".pdb"), emitted.Pdb);
        return CompilationHelper.CreateCompilationFromAssembly(path);
    }

    private sealed class WithContentGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            context.RegisterPostInitializationOutput(static context =>
                context.AddSource("MessageProperties.g.cs", SourceText.From(
                    """
                    // <auto-generated/>
                    namespace NetCord.Rest;
                    public partial class MessageProperties
                    {
                        [System.CodeDom.Compiler.GeneratedCode("WithContentGenerator", "1.0")]
                        public MessageProperties WithContent(string content)
                        {
                            Content = content;
                            return this;
                        }
                    }
                    """, Encoding.UTF8)));
        }
    }
}
