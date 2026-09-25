// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Web;
using Docfx.DataContracts.ManagedReference;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Docfx.Dotnet.Tests;

/// <summary>
/// Tests for the <c>assemblyUids</c> and <c>assemblyUidOverride</c> metadata options, which make the
/// assembly a component of the UID of every API it declares, as in <c>Pkg::Shared.Widget</c>, so that
/// assemblies sharing a namespace don't collide.
/// </summary>
[Collection("docfx STA")]
public class AssemblyUidUnitTest : IDisposable
{
    private static readonly Dictionary<string, string> EmptyMSBuildProperties = [];

    private const string SharedLibraryCode =
        """
        namespace Shared;

        /// <summary>A widget.</summary>
        public class Widget
        {
            /// <summary>Does something.</summary>
            public void Do() { }

            /// <summary>Does something else.</summary>
            public void Do(int value) { }
        }
        """;

    public void Dispose()
    {
        VisitorHelper.AssemblyUids = null;
        VisitorHelper.AssemblyUidOverride = null;
        VisitorHelper.AssemblyUidOverrideAssemblies = null;
        VisitorHelper.GlobalNamespaceId = null;
    }

    /// <summary>
    /// Qualifies each assembly by its own name, which is the array form of <c>assemblyUids</c>.
    /// </summary>
    private static void UseAssemblyUids(params string[] assemblyNames)
    {
        VisitorHelper.AssemblyUids = assemblyNames.ToDictionary(name => name, _ => (string)null, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Names the component of each assembly explicitly, which is the object form of <c>assemblyUids</c>.
    /// </summary>
    private static void UseAssemblyUids(params (string assembly, string component)[] components)
    {
        VisitorHelper.AssemblyUids = components.ToDictionary(x => x.assembly, x => x.component, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// The href of a cref is URL encoded on the way into the comment (<c>XmlComment.ResolveCrefLink</c>) and
    /// decoded again on the way out (<c>XRefDetails.From</c>), so the `::` of an assembly component appears
    /// percent encoded in between.
    /// </summary>
    private static string XrefTo(string uid) => $"<xref href=\"{HttpUtility.UrlEncode(uid)}\"";

    private static MetadataItem Verify(string code, string assemblyName = "test.dll", ExtractMetadataConfig config = null, params MetadataReference[] references)
    {
        var compilation = CompilationHelper.CreateCompilationFromCSharpCode(code, EmptyMSBuildProperties, assemblyName, references);
        Assert.Empty(compilation.GetDeclarationDiagnostics());
        return compilation.Assembly.GenerateMetadataItem(compilation, config);
    }

    /// <summary>
    /// Compiles <paramref name="code"/> into an in-memory assembly that can be referenced by another compilation.
    /// </summary>
    private static MetadataReference CreateReference(string code, string assemblyName)
    {
        var compilation = CompilationHelper.CreateCompilationFromCSharpCode(code, EmptyMSBuildProperties, assemblyName);
        Assert.Empty(compilation.GetDeclarationDiagnostics());
        return compilation.ToMetadataReference();
    }

    [Fact]
    public void UidsAreUnchangedWhenNoAssemblyIsQualified()
    {
        var output = Verify(SharedLibraryCode);

        var @namespace = output.Items[0];
        Assert.Equal("Shared", @namespace.Name);
        Assert.Equal("N:Shared", @namespace.CommentId);
        Assert.Equal("Shared", @namespace.DisplayNames[SyntaxLanguage.CSharp]);
        Assert.Equal("Shared.Widget", @namespace.Items[0].Name);
    }

    [Fact]
    public void NamespaceTypeAndMemberUidsCarryTheAssembly()
    {
        UseAssemblyUids(("test.dll", "Pkg"));

        var output = Verify(SharedLibraryCode);

        var @namespace = output.Items[0];
        Assert.Equal("Pkg::Shared", @namespace.Name);
        Assert.Equal("N:Pkg::Shared", @namespace.CommentId);

        var type = @namespace.Items[0];
        Assert.Equal("Pkg::Shared.Widget", type.Name);
        Assert.Equal("T:Pkg::Shared.Widget", type.CommentId);
        Assert.Equal("Pkg::Shared", type.NamespaceName);

        var method = type.Items.First(i => i.Name.Contains("Do(System.Int32)"));
        Assert.Equal("Pkg::Shared.Widget.Do(System.Int32)", method.Name);
        Assert.Equal("M:Pkg::Shared.Widget.Do(System.Int32)", method.CommentId);
        Assert.Equal("Pkg::Shared.Widget.Do*", method.Overload);
    }

    /// <summary>
    /// The component defaults to the name of the assembly, so the UID names something that exists.
    /// </summary>
    [Fact]
    public void TheAssemblyNameIsTheDefaultComponent()
    {
        UseAssemblyUids("MyLib.Tools");

        var output = Verify(SharedLibraryCode, "MyLib.Tools");

        Assert.Equal("MyLib.Tools::Shared", output.Items[0].Name);
        Assert.Equal("MyLib.Tools::Shared.Widget", output.Items[0].Items[0].Name);
    }

    /// <summary>
    /// The assembly is a component of the UID, not a namespace segment, so it has no place in the names
    /// that are displayed. The visitor never appends it — <c>assemblyLabel</c> is applied once the whole
    /// metadata item has been visited, see <see cref="AssemblyLabelAppendsTheAssemblyWhereItIsAsked"/> —
    /// but it does record the component, which both that pass and the nested layout grouping need.
    /// </summary>
    [Fact]
    public void NamespaceDisplayNamesKeepTheRealNamespace()
    {
        UseAssemblyUids(("test.dll", "Pkg"));

        var @namespace = Verify(SharedLibraryCode).Items[0];

        Assert.Equal("Shared", @namespace.DisplayNames[SyntaxLanguage.CSharp]);
        Assert.Equal("Shared", @namespace.DisplayNamesWithType[SyntaxLanguage.CSharp]);
        Assert.Equal("Shared", @namespace.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
        Assert.Equal("Pkg", @namespace.AssemblyUid);

        // Type display names are unaffected, and keep naming the namespace as it is declared.
        var type = @namespace.Items[0];
        Assert.Equal("Widget", type.DisplayNames[SyntaxLanguage.CSharp]);
        Assert.Equal("Shared.Widget", type.DisplayQualifiedNames[SyntaxLanguage.CSharp]);
    }

    /// <summary>
    /// `page` is the one value the visitor acts on, as it names the real assembly rather than the component
    /// it was given. The enums are internal, so the cases name them as strings.
    /// </summary>
    [Theory]
    [InlineData("None", null)]
    [InlineData("Shared", null)]
    [InlineData("Suffix", null)]
    [InlineData("Page", "test.dll")]
    public void AssemblyLabelPageNamesTheAssemblyOnThePage(string assemblyLabel, string expectedNamespaceAssembly)
    {
        UseAssemblyUids(("test.dll", "Pkg"));

        var config = new ExtractMetadataConfig { AssemblyLabel = Enum.Parse<AssemblyLabel>(assemblyLabel) };

        var @namespace = Verify(SharedLibraryCode, config: config).Items[0];

        Assert.Equal(expectedNamespaceAssembly, @namespace.NamespaceAssembly);

        // Whatever is displayed, the UID is the same.
        Assert.Equal("Pkg::Shared", @namespace.Name);
    }

    /// <summary>
    /// `shared` appends the assembly only to the namespaces more than one assembly of the same metadata
    /// item declares, `suffix` to all of them, and the layout has no say in either. The cases pair a
    /// namespace both assemblies declare with one only the first does.
    /// </summary>
    [Theory]
    [InlineData("None", "Flattened", "Shared", "Shared", "Only")]
    [InlineData("None", "Nested", "Shared", "Shared", "Only")]
    [InlineData("Page", "Flattened", "Shared", "Shared", "Only")]
    [InlineData("Shared", "Flattened", "Shared (A)", "Shared (B)", "Only")]
    [InlineData("Shared", "Nested", "Shared (A)", "Shared (B)", "Only")]
    [InlineData("Suffix", "Flattened", "Shared (A)", "Shared (B)", "Only (A)")]
    [InlineData("Suffix", "Nested", "Shared (A)", "Shared (B)", "Only (A)")]
    public void AssemblyLabelAppendsTheAssemblyWhereItIsAsked(
        string assemblyLabel, string namespaceLayout, string expectedSharedInA, string expectedSharedInB, string expectedOnly)
    {
        var members = new[]
        {
            CreateNamespace("A::Shared", "Shared", "A"),
            CreateNamespace("B::Shared", "Shared", "B"),
            CreateNamespace("A::Only", "Only", "A"),
        }.ToDictionary(x => x.Name);

        DotnetApiCatalog.ApplyAssemblyLabel(members, new ExtractMetadataConfig
        {
            AssemblyLabel = Enum.Parse<AssemblyLabel>(assemblyLabel),
            NamespaceLayout = Enum.Parse<NamespaceLayout>(namespaceLayout),
        });

        Assert.Equal(expectedSharedInA, members["A::Shared"].DisplayNames[SyntaxLanguage.Default]);
        Assert.Equal(expectedSharedInB, members["B::Shared"].DisplayNames[SyntaxLanguage.Default]);
        Assert.Equal(expectedOnly, members["A::Only"].DisplayNames[SyntaxLanguage.Default]);

        // All three names are labelled together, so it is enough to spot check the other two.
        Assert.Equal(expectedSharedInA, members["A::Shared"].DisplayNamesWithType[SyntaxLanguage.Default]);
        Assert.Equal(expectedSharedInA, members["A::Shared"].DisplayQualifiedNames[SyntaxLanguage.Default]);
    }

    /// <summary>
    /// A namespace declared by an assembly that is not qualified carries no component, so it is never
    /// labelled — but it still counts as another declaration of its namespace, which is what makes the
    /// qualified one worth labelling under `shared`.
    /// </summary>
    [Fact]
    public void AnUnqualifiedAssemblyCountsAsADeclarationButIsNotLabelled()
    {
        var members = new[]
        {
            CreateNamespace("A::Shared", "Shared", "A"),
            CreateNamespace("Shared", "Shared", assemblyUid: null),
        }.ToDictionary(x => x.Name);

        DotnetApiCatalog.ApplyAssemblyLabel(members, new ExtractMetadataConfig { AssemblyLabel = AssemblyLabel.Shared });

        Assert.Equal("Shared (A)", members["A::Shared"].DisplayNames[SyntaxLanguage.Default]);
        Assert.Equal("Shared", members["Shared"].DisplayNames[SyntaxLanguage.Default]);
    }

    private static MetadataItem CreateNamespace(string uid, string displayName, string assemblyUid)
    {
        return new MetadataItem
        {
            Type = MemberType.Namespace,
            Name = uid,
            AssemblyUid = assemblyUid,
            DisplayNames = new() { [SyntaxLanguage.Default] = displayName },
            DisplayNamesWithType = new() { [SyntaxLanguage.Default] = displayName },
            DisplayQualifiedNames = new() { [SyntaxLanguage.Default] = displayName },
        };
    }

    [Fact]
    public void TwoAssembliesSharingANamespaceGetDistinctUids()
    {
        UseAssemblyUids("a.dll", "b.dll");

        var a = Verify(SharedLibraryCode, "a.dll");
        var b = Verify(SharedLibraryCode, "b.dll");

        Assert.Equal("a.dll::Shared", a.Items[0].Name);
        Assert.Equal("b.dll::Shared", b.Items[0].Name);
        Assert.Equal("a.dll::Shared.Widget", a.Items[0].Items[0].Name);
        Assert.Equal("b.dll::Shared.Widget", b.Items[0].Items[0].Name);
    }

    [Fact]
    public void CrossAssemblyReferencesUseTheTargetAssemblyComponent()
    {
        UseAssemblyUids(("a.dll", "A"), ("b.dll", "B"));

        var reference = CreateReference(SharedLibraryCode, "a.dll");
        var output = Verify(
            """
            namespace Shared;

            /// <summary>A gadget.</summary>
            public class Gadget : Widget { }
            """,
            "b.dll",
            references: reference);

        var type = output.Items[0].Items[0];
        Assert.Equal("B::Shared.Gadget", type.Name);

        // The base type lives in a.dll, so it must carry A's component, not B's.
        Assert.Equal(["System.Object", "A::Shared.Widget"], type.Inheritance);
    }

    [Fact]
    public void UnqualifiedAssembliesAreLeftUnchanged()
    {
        UseAssemblyUids(("b.dll", "B"));

        var reference = CreateReference(SharedLibraryCode, "a.dll");
        var output = Verify(
            """
            namespace Other;

            /// <summary>A gadget.</summary>
            public class Gadget : Shared.Widget { }
            """,
            "b.dll",
            references: reference);

        var type = output.Items[0].Items[0];
        Assert.Equal("B::Other.Gadget", type.Name);

        // Neither the unlisted assembly nor the framework is qualified.
        Assert.Equal(["System.Object", "Shared.Widget"], type.Inheritance);
    }

    [Fact]
    public void CrefsToQualifiedAssembliesAreResolved()
    {
        UseAssemblyUids(("a.dll", "A"), ("b.dll", "B"));

        var reference = CreateReference(SharedLibraryCode, "a.dll");
        var output = Verify(
            """
            namespace Other;

            /// <summary>Wraps a <see cref="Shared.Widget"/>.</summary>
            /// <seealso cref="Shared.Widget"/>
            /// <seealso cref="System.String"/>
            public class Gadget
            {
                /// <summary>Wraps.</summary>
                /// <exception cref="System.InvalidOperationException">Always.</exception>
                /// <exception cref="Shared.Widget">Never.</exception>
                public void Wrap() { }
            }
            """,
            "b.dll",
            references: reference);

        var type = output.Items[0].Items[0];

        Assert.Contains(XrefTo("A::Shared.Widget"), type.Summary);
        Assert.Equal(["A::Shared.Widget", "System.String"], type.SeeAlsos.Select(x => x.LinkId));

        var method = type.Items[0];
        Assert.Equal(["System.InvalidOperationException", "A::Shared.Widget"], method.Exceptions.Select(x => x.Type));
    }

    /// <summary>
    /// `Overload:` crefs only appear in hand authored XML documentation, so this exercises the
    /// comment parser directly instead of going through a C# compilation.
    /// </summary>
    [Fact]
    public void OverloadCrefsCarryTheAssembly()
    {
        UseAssemblyUids(("a.dll", "A"));

        var compilation = CompilationHelper.CreateCompilationFromCSharpCode(SharedLibraryCode, EmptyMSBuildProperties, "a.dll");
        var context = new XmlCommentParserContext
        {
            ResolveAssemblyUid = commentId => VisitorHelper.GetAssemblyUidForCommentId(commentId, compilation),
        };

        var comment = XmlComment.Parse(
            """
            <member name="T:Other.Gadget">
              <summary>See <see cref="Overload:Shared.Widget.Do"/>.</summary>
            </member>
            """, context);

        Assert.Contains(XrefTo("A::Shared.Widget.Do*"), comment.Summary);
    }

    [Fact]
    public void GenericsAndTypeParametersCarryTheAssemblyOnlyOnce()
    {
        UseAssemblyUids(("test.dll", "Pkg"));

        var output = Verify(
            """
            using System.Collections.Generic;

            namespace Shared;

            /// <summary>A box.</summary>
            public class Box<T>
            {
                /// <summary>Gets many.</summary>
                public List<T> GetMany<TOther>(T value, TOther other) => null;
            }
            """);

        var type = output.Items[0].Items[0];
        Assert.Equal("Pkg::Shared.Box`1", type.Name);

        var method = type.Items[0];
        Assert.Equal("Pkg::Shared.Box`1.GetMany``1(`0,``0)", method.Name);
        Assert.Equal("Pkg::Shared.Box`1.GetMany*", method.Overload);

        // The component is applied exactly once, everywhere.
        Assert.DoesNotContain(output.References.Keys, key => key.Contains("Pkg::Pkg"));
    }

    /// <summary>
    /// Several metadata items can document assemblies that share an assembly name, e.g. per target
    /// version variants of one project. The per item component is the only way to tell those apart.
    /// </summary>
    [Fact]
    public void PerItemOverrideDistinguishesAssembliesSharingAnAssemblyName()
    {
        var v1 = CompilationHelper.CreateCompilationFromCSharpCode(SharedLibraryCode, EmptyMSBuildProperties, "MyLib.dll");
        var v2 = CompilationHelper.CreateCompilationFromCSharpCode(SharedLibraryCode, EmptyMSBuildProperties, "MyLib.dll");

        var a = GenerateWithOverride(v1, "V1");
        var b = GenerateWithOverride(v2, "V2");

        Assert.Equal("V1::Shared", a.Items[0].Name);
        Assert.Equal("V2::Shared", b.Items[0].Name);
        Assert.Equal("V1::Shared.Widget", a.Items[0].Items[0].Name);
        Assert.Equal("V2::Shared.Widget", b.Items[0].Items[0].Name);
    }

    [Fact]
    public void PerItemOverrideWinsOverTheProjectLevelSetting()
    {
        UseAssemblyUids(("test.dll", "FromMap"));

        var compilation = CompilationHelper.CreateCompilationFromCSharpCode(SharedLibraryCode, EmptyMSBuildProperties, "test.dll");
        var output = GenerateWithOverride(compilation, "FromItem");

        Assert.Equal("FromItem::Shared.Widget", output.Items[0].Items[0].Name);
    }

    [Fact]
    public void PerItemOverrideDoesNotApplyToOtherAssemblies()
    {
        // `a.dll` is documented by another metadata item, so it is addressed through the project level map.
        UseAssemblyUids(("a.dll", "A"));

        var reference = CreateReference(SharedLibraryCode, "a.dll");
        var compilation = CompilationHelper.CreateCompilationFromCSharpCode(
            """
            namespace Other;

            /// <summary>A gadget.</summary>
            public class Gadget : Shared.Widget { }
            """,
            EmptyMSBuildProperties, "b.dll", reference);

        var output = GenerateWithOverride(compilation, "B");
        var type = output.Items[0].Items[0];

        Assert.Equal("B::Other.Gadget", type.Name);
        Assert.Equal(["System.Object", "A::Shared.Widget"], type.Inheritance);
    }

    /// <summary>
    /// An assembly documented under a per item override can still be listed in <c>assemblyUids</c>, which
    /// makes that entry the target other items link to. This is the only way to give other items something
    /// to point at when several of them document assemblies sharing an assembly name.
    /// </summary>
    [Fact]
    public void TheProjectLevelSettingIsTheDefaultTargetForOtherItems()
    {
        // `Shared.dll` is documented twice, once per version, and the map names V2 as the default target.
        UseAssemblyUids(("Shared", "V2"), ("consumer.dll", "Consumer"));

        var v1 = CompilationHelper.CreateCompilationFromCSharpCode(SharedLibraryCode, EmptyMSBuildProperties, "Shared");
        var v2 = CompilationHelper.CreateCompilationFromCSharpCode(SharedLibraryCode, EmptyMSBuildProperties, "Shared");

        // Each item's own override still wins for the assembly it documents.
        Assert.Equal("V1::Shared.Widget", GenerateWithOverride(v1, "V1").Items[0].Items[0].Name);
        Assert.Equal("V2::Shared.Widget", GenerateWithOverride(v2, "V2").Items[0].Items[0].Name);

        // An item that only references it falls back to the map, so its links land on the V2 pages.
        var consumer = CompilationHelper.CreateCompilationFromCSharpCode(
            """
            namespace Consumes;

            /// <summary>A gadget.</summary>
            public class Gadget : Shared.Widget { }
            """,
            EmptyMSBuildProperties, "consumer.dll", v2.ToMetadataReference());

        var type = consumer.Assembly.GenerateMetadataItem(consumer).Items[0].Items[0];

        Assert.Equal("Consumer::Consumes.Gadget", type.Name);
        Assert.Equal(["System.Object", "V2::Shared.Widget"], type.Inheritance);
    }

    [Fact]
    public void CrefsCarryThePerItemOverrideToo()
    {
        var compilation = CompilationHelper.CreateCompilationFromCSharpCode(
            """
            namespace Shared;

            /// <summary>A widget.</summary>
            public class Widget { }

            /// <summary>Wraps a <see cref="Widget"/>.</summary>
            /// <seealso cref="Widget"/>
            public class Gadget { }
            """,
            EmptyMSBuildProperties, "test.dll");

        var type = GenerateWithOverride(compilation, "Pkg").Items[0].Items.Single(x => x.Name.EndsWith("Gadget"));

        Assert.Equal("Pkg::Shared.Gadget", type.Name);
        Assert.Contains(XrefTo("Pkg::Shared.Widget"), type.Summary);
        Assert.Equal(["Pkg::Shared.Widget"], type.SeeAlsos.Select(x => x.LinkId));
    }

    private static MetadataItem GenerateWithOverride(Compilation compilation, string assemblyUid)
    {
        VisitorHelper.AssemblyUidOverride = assemblyUid;
        VisitorHelper.AssemblyUidOverrideAssemblies = new(SymbolEqualityComparer.Default) { compilation.Assembly };
        try
        {
            return compilation.Assembly.GenerateMetadataItem(compilation);
        }
        finally
        {
            VisitorHelper.AssemblyUidOverride = null;
            VisitorHelper.AssemblyUidOverrideAssemblies = null;
        }
    }

    /// <summary>
    /// A component may start with a digit and contain dashes, unlike a namespace, because assembly names
    /// do and target framework style components are useful.
    /// </summary>
    [Theory]
    [InlineData("net8.0")]
    [InlineData("7zip.Net")]
    [InlineData("my-lib")]
    public void ComponentsAreNotRestrictedToNamespaceGrammar(string component)
    {
        UseAssemblyUids(("test.dll", component));

        var output = Verify(SharedLibraryCode);

        Assert.Equal($"{component}::Shared", output.Items[0].Name);
        Assert.Equal($"{component}::Shared.Widget", output.Items[0].Items[0].Name);
        Assert.Equal($"T:{component}::Shared.Widget", output.Items[0].Items[0].CommentId);
    }

    /// <summary>
    /// The global namespace is qualified as well, otherwise two assemblies whose APIs sit in it would
    /// share the one page named after <c>globalNamespaceId</c>.
    /// </summary>
    [Fact]
    public void GlobalNamespaceIdComposesWithTheAssemblyComponent()
    {
        UseAssemblyUids(("test.dll", "Pkg"));
        VisitorHelper.GlobalNamespaceId = "Global";

        var output = Verify(
            """
            /// <summary>A widget.</summary>
            public class Widget { }
            """);

        var @namespace = output.Items[0];
        Assert.Equal("Pkg::Global", @namespace.Name);
        Assert.Equal("Pkg::Global.Widget", @namespace.Items[0].Name);
        Assert.Equal("Pkg::Global", @namespace.Items[0].NamespaceName);
    }

    /// <summary>
    /// https://github.com/dotnet/docfx/issues/9458: the table of contents node of the global namespace
    /// shows no name at all. Checked here without any assembly component, so it is the plain behaviour.
    /// </summary>
    [Fact]
    public void GlobalNamespaceHasADisplayName()
    {
        VisitorHelper.GlobalNamespaceId = "Global";

        var @namespace = Verify(
            """
            /// <summary>A widget.</summary>
            public class Widget { }
            """).Items[0];

        Assert.Equal("Global", @namespace.Name);
        Assert.Equal("Global", @namespace.DisplayNames[SyntaxLanguage.CSharp]);
    }

    [Fact]
    public void FilterRulesMatchUnqualifiedUids()
    {
        UseAssemblyUids(("test.dll", "Pkg"));

        var output = Verify(
            """
            namespace Shared;

            /// <summary>A widget.</summary>
            public class Widget { }

            /// <summary>A hidden widget.</summary>
            public class HiddenWidget { }
            """,
            config: new() { FilterConfigFile = "TestData/filterconfig.assemblyuid.yml" });

        // The filter file matches `Shared.HiddenWidget`, i.e. the API surface, not `Pkg::Shared.HiddenWidget`.
        Assert.Equal(["Pkg::Shared.Widget"], output.Items[0].Items.Select(x => x.Name));
    }

    /// <summary>
    /// A repeated or empty entry in the array form is a configuration typo, reported later as an invalid
    /// entry, so it must not throw out of the JSON converter that builds this.
    /// </summary>
    [Fact]
    public void RepeatedAndEmptyArrayEntriesAreTolerated()
    {
        var assemblyUids = new AssemblyUidConfig(["MyLib", "MyLib", null, "MyLib.Tools"]);

        Assert.Equal(["MyLib", "", "MyLib.Tools"], assemblyUids.Keys);
        Assert.All(assemblyUids.Values, Assert.Null);
    }

    /// <summary>
    /// A UID becomes a file name, and `:` is not a legal file name character. One dash per character is
    /// what <c>PathUtility.ToCleanUrlFileName</c> produces for the member pages of
    /// <c>memberLayout: separatePages</c>, so the two agree.
    /// </summary>
    [Theory]
    [InlineData("Pkg::Shared.Widget", "Pkg--Shared.Widget")]
    [InlineData("Pkg::Shared.Box`1", "Pkg--Shared.Box-1")]
    [InlineData("Shared.Widget", "Shared.Widget")]
    public void QualifiedUidsMapToLegalFileNames(string uid, string expected)
    {
        Assert.Equal(expected, VisitorHelper.PathFriendlyId(uid));
    }

    [Theory]
    [InlineData("Pkg::Shared.Widget", "Shared.Widget")]
    [InlineData("Shared.Widget", "Shared.Widget")]
    [InlineData("MyLib.Tools::Shared", "Shared")]
    public void TheAssemblyComponentCanBeTrimmedBackOff(string uid, string expected)
    {
        Assert.Equal(expected, VisitorHelper.TrimAssemblyUid(uid));
    }

    /// <summary>
    /// The href of a reference is built from the comment id rather than from the metadata item, so it has
    /// to arrive at the same file name and anchor the pages are written under.
    /// </summary>
    [Fact]
    public void ReferenceUrlsPointAtTheQualifiedPages()
    {
        UseAssemblyUids(("test.dll", "Pkg"));

        var compilation = CompilationHelper.CreateCompilationFromCSharpCode(SharedLibraryCode, EmptyMSBuildProperties, "test.dll");
        var allAssemblies = new HashSet<IAssemblySymbol>(SymbolEqualityComparer.Default) { compilation.Assembly };

        var type = compilation.Assembly.GetTypeByMetadataName("Shared.Widget");
        Assert.NotNull(type);
        var method = type.GetMembers("Do").First();

        Assert.Equal("Pkg--Shared.Widget.html", SymbolUrlResolver.GetDocfxUrl(type, MemberLayout.SamePage, SymbolUrlKind.Html, allAssemblies));
        Assert.Equal("Pkg--Shared.html", SymbolUrlResolver.GetDocfxUrl(type.ContainingNamespace, MemberLayout.SamePage, SymbolUrlKind.Html, allAssemblies));

        // On a type page the member is an anchor, and on its own page it is a file of its own.
        Assert.Equal(
            "Pkg--Shared.Widget.html#Pkg__Shared_Widget_Do",
            SymbolUrlResolver.GetDocfxUrl(method, MemberLayout.SamePage, SymbolUrlKind.Html, allAssemblies));
        Assert.Equal(
            "Pkg--Shared.Widget.Do.html#Pkg__Shared_Widget_Do",
            SymbolUrlResolver.GetDocfxUrl(method, MemberLayout.SeparatePages, SymbolUrlKind.Html, allAssemblies));
    }
}
