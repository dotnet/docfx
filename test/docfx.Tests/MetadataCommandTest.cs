// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Common;
using Docfx.DataContracts.Common;
using Docfx.DataContracts.ManagedReference;
using Docfx.Dotnet;
using Docfx.Tests.Common;

namespace Docfx.Tests;

[Collection("docfx STA")]
public class MetadataCommandTest : TestBase
{
    /// <summary>
    /// Use MetadataCommand to generate YAML files from a c# project and a VB project separately
    /// </summary>
    private readonly string _outputFolder;
    private readonly string _projectFolder;

    public MetadataCommandTest()
    {
        _outputFolder = GetRandomFolder();
        _projectFolder = GetRandomFolder();
    }

    [Fact]
    [Trait("Related", "docfx")]
    public async Task TestMetadataCommandFromCSProject()
    {
        var projectFile = Path.Combine(_projectFolder, "test.csproj");
        var sourceFile = Path.Combine(_projectFolder, "test.cs");
        File.Copy("Assets/test.csproj.sample.1", projectFile);
        File.Copy("Assets/test.cs.sample.1", sourceFile);

        await DotnetApiCatalog.Exec(
            new(new MetadataJsonItemConfig { Dest = _outputFolder, Src = new(new FileMappingItem(projectFile)) { Expanded = true } }),
            new(), Directory.GetCurrentDirectory());

        CheckResult();
    }

    [Fact]
    [Trait("Related", "docfx")]
    public async Task TestMetadataCommandFromDll()
    {
        var dllFile = Path.Combine(_projectFolder, "test.dll");
        File.Copy("Assets/test.dll.sample.1", dllFile);

        await DotnetApiCatalog.Exec(
            new(new MetadataJsonItemConfig { Dest = _outputFolder, Src = new(new FileMappingItem(dllFile)) { Expanded = true } }),
            new(), Directory.GetCurrentDirectory());

        CheckResult();
    }

    [Fact]
    [Trait("Related", "docfx")]
    public async Task TestMetadataCommandFromMultipleFrameworksCSProject()
    {
        // Create default project
        var projectFile = Path.Combine(_projectFolder, "multi-frameworks-test.csproj");
        var sourceFile = Path.Combine(_projectFolder, "test.cs");
        File.Copy("Assets/multi-frameworks-test.csproj.sample.1", projectFile);
        File.Copy("Assets/test.cs.sample.1", sourceFile);

        await DotnetApiCatalog.Exec(
            new(new MetadataJsonItemConfig
            {
                Dest = _outputFolder,
                Src = new(new FileMappingItem(projectFile)) { Expanded = true },
                Properties = new() { ["TargetFramework"] = "net8.0" },
            }),
            new(), Directory.GetCurrentDirectory());

        CheckResult();
    }

    [Fact]
    [Trait("Related", "docfx")]
    public async Task TestMetadataCommandFromVBProject()
    {
        if (!OperatingSystem.IsWindows())
            return;

        // Create default project
        var projectFile = Path.Combine(_projectFolder, "test.vbproj");
        var sourceFile = Path.Combine(_projectFolder, "test.vb");
        File.Copy("Assets/test.vbproj.sample.1", projectFile);
        File.Copy("Assets/test.vb.sample.1", sourceFile);

        await DotnetApiCatalog.Exec(
            new(new MetadataJsonItemConfig { Dest = _outputFolder, Src = new(new FileMappingItem(projectFile)) { Expanded = true } }),
            new(), Directory.GetCurrentDirectory());

        Assert.True(File.Exists(Path.Combine(_outputFolder, ".manifest")));

        var file = Path.Combine(_outputFolder, "toc.yml");
        Assert.True(File.Exists(file));
        var tocViewModel = YamlUtility.Deserialize<TocItemViewModel>(file).Items;
        Assert.Equal("testVBproj1.Foo", tocViewModel[0].Uid);
        Assert.Equal("testVBproj1.Foo", tocViewModel[0].Name);
        Assert.Equal("testVBproj1.Foo.Bar", tocViewModel[0].Items[0].Uid);
        Assert.Equal("Bar", tocViewModel[0].Items[0].Name);

        file = Path.Combine(_outputFolder, "testVBproj1.Foo.yml");
        Assert.True(File.Exists(file));
        var memberViewModel = YamlUtility.Deserialize<PageViewModel>(file);
        Assert.Equal("testVBproj1.Foo", memberViewModel.Items[0].Uid);
        Assert.Equal("testVBproj1.Foo", memberViewModel.Items[0].Id);
        Assert.Equal("testVBproj1.Foo", memberViewModel.Items[0].Name);
        Assert.Equal("testVBproj1.Foo", memberViewModel.Items[0].FullName);

        file = Path.Combine(_outputFolder, "testVBproj1.Foo.Bar.yml");
        Assert.True(File.Exists(file));
        memberViewModel = YamlUtility.Deserialize<PageViewModel>(file);
        Assert.Equal("testVBproj1.Foo.Bar", memberViewModel.Items[0].Uid);
        Assert.Equal("Bar", memberViewModel.Items[0].Id);
        Assert.Equal("Bar", memberViewModel.Items[0].Name);
        Assert.Equal("testVBproj1.Foo.Bar", memberViewModel.Items[0].FullName);
        Assert.Equal("testVBproj1.Foo.Bar.FooBar``1(System.Int32[],System.Byte,``0,System.Collections.Generic.List{``0[]})", memberViewModel.Items[1].Uid);
        Assert.Equal("FooBar``1(System.Int32[],System.Byte,``0,System.Collections.Generic.List{``0[]})", memberViewModel.Items[1].Id);
        Assert.Equal("FooBar<TArg>(int[], byte, TArg, List<TArg[]>)", memberViewModel.Items[1].Name);
        Assert.Equal("testVBproj1.Foo.Bar.FooBar<TArg>(int[], byte, TArg, System.Collections.Generic.List<TArg[]>)", memberViewModel.Items[1].FullName);
        Assert.NotNull(memberViewModel.References.Find(
            s => s.Uid.Equals("System.Collections.Generic.List{System.String}")
            ));
        Assert.NotNull(memberViewModel.References.Find(
            s => s.Uid.Equals("System.Int32[]")
            ));
        Assert.NotNull(memberViewModel.References.Find(
            s => s.Uid.Equals("System.Byte")
            ));
        Assert.NotNull(memberViewModel.References.Find(
            s => s.Uid.Equals("{TArg}")
            ));
        Assert.NotNull(memberViewModel.References.Find(
            s => s.Uid.Equals("System.Collections.Generic.List{{TArg}[]}")
            ));
    }

    [Fact]
    [Trait("Related", "docfx")]
    public async Task TestMetadataCommandFromCSProjectWithFilterInOption()
    {
        // Create default project
        var projectFile = Path.Combine(_projectFolder, "test.csproj");
        var sourceFile = Path.Combine(_projectFolder, "test.cs");
        var filterFile = Path.Combine(_projectFolder, "filter.yaml");
        File.Copy("Assets/test.csproj.sample.1", projectFile);
        File.Copy("Assets/test.cs.sample.1", sourceFile);
        File.Copy("Assets/filter.yaml.sample", filterFile);

        await DotnetApiCatalog.Exec(
            new(new MetadataJsonItemConfig
            {
                Dest = _outputFolder,
                Src = new(new FileMappingItem(projectFile)) { Expanded = true },
                Filter = filterFile,
            }),
            new(), Directory.GetCurrentDirectory());

        Assert.True(File.Exists(Path.Combine(_outputFolder, ".manifest")));

        var file = Path.Combine(_outputFolder, "toc.yml");
        Assert.True(File.Exists(file));
        var tocViewModel = YamlUtility.Deserialize<TocItemViewModel>(file).Items;
        Assert.Equal("Foo", tocViewModel[0].Uid);
        Assert.Equal("Foo", tocViewModel[0].Name);
        Assert.Equal("Foo.Bar", tocViewModel[0].Items[0].Uid);
        Assert.Equal("Bar", tocViewModel[0].Items[0].Name);

        file = Path.Combine(_outputFolder, "Foo.yml");
        Assert.True(File.Exists(file));
        var memberViewModel = YamlUtility.Deserialize<PageViewModel>(file);
        Assert.Equal("Foo", memberViewModel.Items[0].Uid);
        Assert.Equal("Foo", memberViewModel.Items[0].Id);
        Assert.Equal("Foo", memberViewModel.Items[0].Name);
        Assert.Equal("Foo", memberViewModel.Items[0].FullName);

        file = Path.Combine(_outputFolder, "Foo.Bar.yml");
        Assert.True(File.Exists(file));
        memberViewModel = YamlUtility.Deserialize<PageViewModel>(file);
        Assert.Equal("Foo.Bar", memberViewModel.Items[0].Uid);
        Assert.Equal("Bar", memberViewModel.Items[0].Id);
        Assert.Equal("Bar", memberViewModel.Items[0].Name);
        Assert.Equal("Foo.Bar", memberViewModel.Items[0].FullName);
        Assert.Single(memberViewModel.Items);
        Assert.NotNull(memberViewModel.References.Find(s => s.Uid.Equals("Foo")));
    }

    [Fact]
    [Trait("Related", "docfx")]
    public async Task TestMetadataCommandFromCSProjectWithDuplicateProjectReference()
    {
        // Create default project
        var projectFile = Path.Combine(_projectFolder, "test.csproj");
        var refProjectFile = Path.Combine(_projectFolder, "ref.csproj");
        var sourceFile = Path.Combine(_projectFolder, "test.cs");
        File.Copy("Assets/test.csproj.sample.1", projectFile);
        File.Copy("Assets/ref.csproj.sample.1", refProjectFile);
        File.Copy("Assets/test.cs.sample.1", sourceFile);

        await DotnetApiCatalog.Exec(
            new(new MetadataJsonItemConfig { Dest = _outputFolder, Src = new(new FileMappingItem(projectFile)) { Expanded = true } }),
            new(), Directory.GetCurrentDirectory());

        CheckResult();
    }

    [Fact]
    [Trait("Related", "docfx")]
    public async Task TestMetadataCommandFromCSProjectWithMultipleNamespaces()
    {
        var projectFile = Path.Combine(_projectFolder, "test.csproj");
        var sourceFile = Path.Combine(_projectFolder, "test.cs");
        File.Copy("Assets/test.csproj.sample.1", projectFile);
        File.Copy("Assets/test-multinamespace.cs.sample.1", sourceFile);

        await DotnetApiCatalog.Exec(
            new(new MetadataJsonItemConfig
            {
                Dest = _outputFolder,
                Src = new(new FileMappingItem(projectFile)) { Expanded = true },
                NamespaceLayout = NamespaceLayout.Nested,
            }),
            new(), Directory.GetCurrentDirectory());

        var file = Path.Combine(_outputFolder, "toc.yml");
        Assert.True(File.Exists(file));
        var tocViewModel = YamlUtility.Deserialize<TocItemViewModel>(file).Items;
        Assert.Equal("OtherNamespace", tocViewModel[0].Uid);
        Assert.Equal("OtherNamespace", tocViewModel[0].Name);

        Assert.Equal("OtherNamespace.OtherBar", tocViewModel[0].Items[0].Uid);
        Assert.Equal("OtherBar", tocViewModel[0].Items[0].Name);

        Assert.Equal("Samples.Foo", tocViewModel[1].Uid);
        Assert.Equal("Samples.Foo", tocViewModel[1].Name);

        Assert.Equal("Samples.Foo.Sub", tocViewModel[1].Items[0].Uid);
        Assert.Equal("Sub", tocViewModel[1].Items[0].Name);
        Assert.Equal("Samples.Foo.Sub.SubBar", tocViewModel[1].Items[0].Items[0].Uid);
        Assert.Equal("SubBar", tocViewModel[1].Items[0].Items[0].Name);

        Assert.Equal("Samples.Foo.Bar", tocViewModel[1].Items[1].Uid);
        Assert.Equal("Bar", tocViewModel[1].Items[1].Name);
    }

    [Fact]
    [Trait("Related", "docfx")]
    public async Task TestMetadataCommandFromCSProjectWithMultipleNamespacesWithFlatToc()
    {
        var projectFile = Path.Combine(_projectFolder, "test.csproj");
        var sourceFile = Path.Combine(_projectFolder, "test.cs");
        File.Copy("Assets/test.csproj.sample.1", projectFile);
        File.Copy("Assets/test-multinamespace.cs.sample.1", sourceFile);

        await DotnetApiCatalog.Exec(
            new(new MetadataJsonItemConfig
            {
                Dest = _outputFolder,
                Src = new(new FileMappingItem(projectFile)) { Expanded = true },
                NamespaceLayout = NamespaceLayout.Flattened,
            }),
            new(), Directory.GetCurrentDirectory());

        var file = Path.Combine(_outputFolder, "toc.yml");
        Assert.True(File.Exists(file));
        var tocViewModel = YamlUtility.Deserialize<TocItemViewModel>(file).Items;
        Assert.Equal("OtherNamespace", tocViewModel[0].Uid);
        Assert.Equal("OtherNamespace", tocViewModel[0].Name);

        Assert.Equal("OtherNamespace.OtherBar", tocViewModel[0].Items[0].Uid);
        Assert.Equal("OtherBar", tocViewModel[0].Items[0].Name);

        Assert.Equal("Samples.Foo", tocViewModel[1].Uid);
        Assert.Equal("Samples.Foo", tocViewModel[1].Name);
        Assert.Equal("Samples.Foo.Bar", tocViewModel[1].Items[0].Uid);
        Assert.Equal("Bar", tocViewModel[1].Items[0].Name);

        Assert.Equal("Samples.Foo.Sub", tocViewModel[2].Uid);
        Assert.Equal("Samples.Foo.Sub", tocViewModel[2].Name);

        Assert.Equal("Samples.Foo.Sub.SubBar", tocViewModel[2].Items[0].Uid);
        Assert.Equal("SubBar", tocViewModel[2].Items[0].Name);
    }

    [Fact]
    [Trait("Related", "docfx")]
    public async Task TestMetadataCommandFromCSProjectWithMultipleNamespacesWithGapsWithNestedToc()
    {
        var projectFile = Path.Combine(_projectFolder, "test.csproj");
        var sourceFile = Path.Combine(_projectFolder, "test.cs");
        File.Copy("Assets/test.csproj.sample.1", projectFile);
        File.Copy("Assets/test-multinamespace-withgaps.cs.sample.1", sourceFile);

        await DotnetApiCatalog.Exec(
            new(new MetadataJsonItemConfig
            {
                Dest = _outputFolder,
                Src = new(new FileMappingItem(projectFile)) { Expanded = true },
                NamespaceLayout = NamespaceLayout.Nested,
            }),
            new(), Directory.GetCurrentDirectory());

        var file = Path.Combine(_outputFolder, "toc.yml");
        Assert.True(File.Exists(file));
        var tocViewModel = YamlUtility.Deserialize<TocItemViewModel>(file).Items;
        Assert.Equal("OtherNamespace", tocViewModel[0].Uid);
        Assert.Equal("OtherNamespace", tocViewModel[0].Name);

        Assert.Equal("OtherNamespace.OtherBar", tocViewModel[0].Items[0].Uid);
        Assert.Equal("OtherBar", tocViewModel[0].Items[0].Name);

        Assert.Equal("Samples.Foo", tocViewModel[1].Uid);
        Assert.Equal("Samples.Foo", tocViewModel[1].Name);

        Assert.Equal("Samples.Foo.Sub", tocViewModel[1].Items[0].Uid);
        Assert.Equal("Sub", tocViewModel[1].Items[0].Name);
        Assert.Equal("Samples.Foo.Sub.Subber1", tocViewModel[1].Items[0].Items[0].Uid);
        Assert.Equal("Subber1", tocViewModel[1].Items[0].Items[0].Name);
        Assert.Equal("Samples.Foo.Sub.Subber1.SubberBar", tocViewModel[1].Items[0].Items[0].Items[0].Uid);
        Assert.Equal("SubberBar", tocViewModel[1].Items[0].Items[0].Items[0].Name);

        Assert.Equal("Samples.Foo.Sub.Subber2", tocViewModel[1].Items[0].Items[1].Uid);
        Assert.Equal("Subber2", tocViewModel[1].Items[0].Items[1].Name);
        Assert.Equal("Samples.Foo.Sub.Subber2.Subber2Bar", tocViewModel[1].Items[0].Items[1].Items[0].Uid);
        Assert.Equal("Subber2Bar", tocViewModel[1].Items[0].Items[1].Items[0].Name);

        Assert.Equal("Samples.Foo.Bar", tocViewModel[1].Items[1].Uid);
        Assert.Equal("Bar", tocViewModel[1].Items[1].Name);
    }

    [Fact]
    [Trait("Related", "docfx")]
    public async Task TestMetadataCommandWithoutAssemblyUidsDropsDuplicatedApis()
    {
        var projects = CreateProjectsSharingANamespace();

        using var listener = new TestListenerScope();

        await DotnetApiCatalog.Exec(
            new(new MetadataJsonItemConfig { Dest = _outputFolder, Src = new(new FileMappingItem([.. projects])) { Expanded = true } }),
            new(), Directory.GetCurrentDirectory());

        // Both assemblies declare `Shared.Widget`, so one of them is dropped.
        Assert.Contains(listener.GetItemsByLogLevel(LogLevel.Warning), x => x.Message.Contains("Ignore duplicated member"));
        Assert.True(File.Exists(Path.Combine(_outputFolder, "Shared.Widget.yml")));
        Assert.False(File.Exists(Path.Combine(_outputFolder, "a--Shared.Widget.yml")));
        Assert.False(File.Exists(Path.Combine(_outputFolder, "b--Shared.Widget.yml")));
    }

    [Fact]
    [Trait("Related", "docfx")]
    public async Task TestMetadataCommandWithAssemblyUids()
    {
        var projects = CreateProjectsSharingANamespace();

        using var listener = new TestListenerScope();

        await DotnetApiCatalog.Exec(
            new(new MetadataJsonItemConfig
            {
                Dest = _outputFolder,
                Src = new(new FileMappingItem([.. projects])) { Expanded = true },
            }),
            new(), Directory.GetCurrentDirectory(),
            // The array form: each assembly is qualified by its own name.
            assemblyUids: new(["a", "b"]));

        Assert.DoesNotContain(listener.GetItemsByLogLevel(LogLevel.Warning), x => x.Message.Contains("Ignore duplicated member"));

        // Each assembly gets its own page instead of overwriting the other one, and `::` becomes dashes in
        // the file name, as `:` is not legal there.
        foreach (var (assembly, description) in new[] { ("a", "The widget of assembly A."), ("b", "The widget of assembly B.") })
        {
            var file = Path.Combine(_outputFolder, $"{assembly}--Shared.Widget.yml");
            Assert.True(File.Exists(file), $"{file} is missing.");

            var memberViewModel = YamlUtility.Deserialize<PageViewModel>(file);
            Assert.Equal($"{assembly}::Shared.Widget", memberViewModel.Items[0].Uid);
            Assert.Equal($"T:{assembly}::Shared.Widget", memberViewModel.Items[0].CommentId);
            Assert.Equal($"{assembly}::Shared", memberViewModel.Items[0].NamespaceName);
            Assert.Equal(description, memberViewModel.Items[0].Summary);

            // The members of the type are on the same page and carry the component too.
            Assert.Contains($"{assembly}::Shared.Widget.Do", memberViewModel.Items.Select(x => x.Uid));
        }

        // The namespaces are distinguishable in the TOC, and read as the namespace they are, with the
        // assembly appended because a flattened layout has nothing else to tell them apart.
        var tocViewModel = YamlUtility.Deserialize<TocItemViewModel>(Path.Combine(_outputFolder, "toc.yml")).Items;
        Assert.Equal(["a::Other", "a::Shared", "b::Other", "b::Shared"], tocViewModel.Select(x => x.Uid));
        Assert.Equal(["Other (a)", "Shared (a)", "Other (b)", "Shared (b)"], tocViewModel.Select(x => x.Name));

        // ... and both are addressable through the manifest.
        var manifest = JsonUtility.Deserialize<Dictionary<string, string>>(Path.Combine(_outputFolder, ".manifest"));
        Assert.Equal("a--Shared.Widget.yml", manifest["a::Shared.Widget"]);
        Assert.Equal("b--Shared.Widget.yml", manifest["b::Shared.Widget"]);
    }

    [Fact]
    [Trait("Related", "docfx")]
    public async Task TestMetadataCommandWithAssemblyUidsAndNestedToc()
    {
        var projects = CreateProjectsSharingANamespace();

        await DotnetApiCatalog.Exec(
            new(new MetadataJsonItemConfig
            {
                Dest = _outputFolder,
                Src = new(new FileMappingItem([.. projects])) { Expanded = true },
                NamespaceLayout = NamespaceLayout.Nested,
            }),
            new(), Directory.GetCurrentDirectory(),
            assemblyUids: new() { ["a"] = "A", ["b"] = "B" });

        // A node per assembly groups the namespaces of that assembly. It names the assembly and has no
        // page of its own, so it carries no uid, and the namespaces under it keep their own names.
        var tocViewModel = YamlUtility.Deserialize<TocItemViewModel>(Path.Combine(_outputFolder, "toc.yml")).Items;
        Assert.Equal(["A", "B"], tocViewModel.Select(x => x.Name));
        Assert.Equal([null, null], tocViewModel.Select(x => x.Uid));
        Assert.Equal(["A::Other", "A::Shared"], tocViewModel[0].Items.Select(x => x.Uid));
        Assert.Equal(["Other", "Shared"], tocViewModel[0].Items.Select(x => x.Name));
        Assert.Equal(["B::Other", "B::Shared"], tocViewModel[1].Items.Select(x => x.Uid));
        Assert.Equal("A::Shared.Widget", tocViewModel[0].Items[1].Items[0].Uid);
        Assert.Equal("B::Shared.Widget", tocViewModel[1].Items[1].Items[0].Uid);
    }

    /// <summary>
    /// The assembly is a component of the UID and not a namespace segment, so the page reads as the
    /// namespace it documents: the title, the namespace its types report, and the namespace they link to
    /// all agree, and an authored xref names a real namespace behind the real assembly.
    /// </summary>
    [Fact]
    [Trait("Related", "docfx")]
    public async Task TestMetadataCommandAssemblyUidDoesNotSurfaceAsANamespace()
    {
        var projects = CreateProjectsSharingANamespace();

        await DotnetApiCatalog.Exec(
            new(new MetadataJsonItemConfig
            {
                Dest = _outputFolder,
                Src = new(new FileMappingItem([.. projects])) { Expanded = true },
            }),
            new(), Directory.GetCurrentDirectory(),
            assemblyUids: new(["a", "b"]));

        // The namespace page is titled `Namespace {name}`, and that name is the namespace, qualified by
        // the assembly it comes from rather than by a namespace segment that does not exist.
        var @namespace = YamlUtility.Deserialize<PageViewModel>(Path.Combine(_outputFolder, "a--Shared.yml")).Items[0];
        Assert.Equal("Shared (a)", @namespace.Name);
        Assert.Equal("Shared (a)", @namespace.NameWithType);
        Assert.Equal("Shared (a)", @namespace.FullName);

        // The type pages of that namespace agree with it.
        var type = YamlUtility.Deserialize<PageViewModel>(Path.Combine(_outputFolder, "a--Shared.Widget.yml"));
        Assert.Equal("Shared.Widget", type.Items[0].FullName);
        Assert.Equal("a::Shared", type.Items[0].NamespaceName);
        Assert.Equal("Shared", type.References.Single(x => x.Uid == "a::Shared").Name);

        // An authored xref names the real assembly and the real namespace.
        var manifest = JsonUtility.Deserialize<Dictionary<string, string>>(Path.Combine(_outputFolder, ".manifest"));
        Assert.Contains("a::Shared.Widget", manifest.Keys);
        Assert.DoesNotContain("a.Shared.Widget", manifest.Keys);
    }

    /// <summary>
    /// An assembly root is kept even when the assembly contributes a single namespace, where the single
    /// child collapse in <c>YamlMetadataResolver.GenerateNestedToc</c> would otherwise promote that
    /// namespace and drop the only node naming the assembly.
    /// </summary>
    [Fact]
    [Trait("Related", "docfx")]
    public async Task TestMetadataCommandNestedTocKeepsTheAssemblyRootOfASingleNamespace()
    {
        var projects = CreateProjectsSharingTheirOnlyNamespace();

        await DotnetApiCatalog.Exec(
            new(new MetadataJsonItemConfig
            {
                Dest = _outputFolder,
                Src = new(new FileMappingItem([.. projects])) { Expanded = true },
                NamespaceLayout = NamespaceLayout.Nested,
            }),
            new(), Directory.GetCurrentDirectory(),
            assemblyUids: new() { ["a"] = "A", ["b"] = "B" });

        var tocViewModel = YamlUtility.Deserialize<TocItemViewModel>(Path.Combine(_outputFolder, "toc.yml")).Items;

        Assert.Equal(["A", "B"], tocViewModel.Select(x => x.Name));
        Assert.Equal(["Shared"], tocViewModel[0].Items.Select(x => x.Name));
        Assert.Equal(["A::Shared"], tocViewModel[0].Items.Select(x => x.Uid));
        Assert.Equal("A::Shared.Widget", tocViewModel[0].Items[0].Items[0].Uid);
    }

    /// <summary>
    /// assemblyUidOverride is scoped to its own metadata item, so two items can use it even
    /// though the project level assemblyUids could not tell their assemblies apart by name.
    /// </summary>
    [Fact]
    [Trait("Related", "docfx")]
    public async Task TestMetadataCommandWithAssemblyUidOverride()
    {
        var projects = CreateProjectsSharingANamespace();
        var otherOutputFolder = GetRandomFolder();

        await DotnetApiCatalog.Exec(
            new(
                new MetadataJsonItemConfig
                {
                    Dest = _outputFolder,
                    Src = new(new FileMappingItem(projects[0])) { Expanded = true },
                    AssemblyUidOverride = "First",
                },
                new MetadataJsonItemConfig
                {
                    Dest = otherOutputFolder,
                    Src = new(new FileMappingItem(projects[1])) { Expanded = true },
                    AssemblyUidOverride = "Second",
                }),
            new(), Directory.GetCurrentDirectory());

        foreach (var (folder, prefix) in new[] { (_outputFolder, "First"), (otherOutputFolder, "Second") })
        {
            var file = Path.Combine(folder, $"{prefix}--Shared.Widget.yml");
            Assert.True(File.Exists(file), $"{file} is missing.");
            Assert.Equal($"{prefix}::Shared.Widget", YamlUtility.Deserialize<PageViewModel>(file).Items[0].Uid);
        }
    }

    [Fact]
    [Trait("Related", "docfx")]
    public async Task TestMetadataCommandWithInvalidAssemblyUids()
    {
        var projects = CreateProjectsSharingANamespace();

        using var listener = new TestListenerScope();

        await DotnetApiCatalog.Exec(
            new(new MetadataJsonItemConfig
            {
                Dest = _outputFolder,
                Src = new(new FileMappingItem(projects[0])) { Expanded = true },
                AssemblyUidOverride = "not a component",
            }),
            new(), Directory.GetCurrentDirectory(),
            assemblyUids: new() { ["a"] = "also/invalid", [""] = "A" });

        // Every invalid value is reported and dropped, and generation continues unqualified.
        Assert.Equal(3, listener.GetItemsByLogLevel(LogLevel.Warning).Count(x => x.Code == "InvalidAssemblyUid"));
        Assert.True(File.Exists(Path.Combine(_outputFolder, "Shared.Widget.yml")));
    }

    /// <summary>
    /// Creates two projects, `a` and `b`, that both declare `Shared.Widget`.
    /// </summary>
    private List<string> CreateProjectsSharingANamespace()
    {
        return CreateProjects("assemblyuid.{0}.cs.sample.1");
    }

    /// <summary>
    /// Creates two projects, `a` and `b`, that both declare `Shared.Widget` and nothing else, so each
    /// assembly contributes exactly one namespace.
    /// </summary>
    private List<string> CreateProjectsSharingTheirOnlyNamespace()
    {
        return CreateProjects("assemblyuid.single.{0}.cs.sample.1");
    }

    private List<string> CreateProjects(string sourceAssetFormat)
    {
        var result = new List<string>();

        foreach (var name in new[] { "a", "b" })
        {
            var folder = Path.Combine(_projectFolder, name);
            Directory.CreateDirectory(folder);

            var projectFile = Path.Combine(folder, $"{name}.csproj");
            File.Copy("Assets/assemblyuid.csproj.sample.1", projectFile);
            File.Copy($"Assets/{string.Format(sourceAssetFormat, name)}", Path.Combine(folder, "Widget.cs"));

            result.Add(projectFile);
        }

        return result;
    }

    private void CheckResult()
    {
        Assert.True(File.Exists(Path.Combine(_outputFolder, ".manifest")));

        var file = Path.Combine(_outputFolder, "toc.yml");
        Assert.True(File.Exists(file));
        var tocViewModel = YamlUtility.Deserialize<TocItemViewModel>(file).Items;
        Assert.Equal("Foo", tocViewModel[0].Uid);
        Assert.Equal("Foo", tocViewModel[0].Name);
        Assert.Equal("Foo.Bar", tocViewModel[0].Items[0].Uid);
        Assert.Equal("Bar", tocViewModel[0].Items[0].Name);

        file = Path.Combine(_outputFolder, "Foo.yml");
        Assert.True(File.Exists(file));
        var memberViewModel = YamlUtility.Deserialize<PageViewModel>(file);
        Assert.Equal("Foo", memberViewModel.Items[0].Uid);
        Assert.Equal("Foo", memberViewModel.Items[0].Id);
        Assert.Equal("Foo", memberViewModel.Items[0].Name);
        Assert.Equal("Foo", memberViewModel.Items[0].FullName);

        file = Path.Combine(_outputFolder, "Foo.Bar.yml");
        Assert.True(File.Exists(file));
        memberViewModel = YamlUtility.Deserialize<PageViewModel>(file);
        Assert.Equal("Foo.Bar", memberViewModel.Items[0].Uid);
        Assert.Equal("Bar", memberViewModel.Items[0].Id);
        Assert.Equal("Bar", memberViewModel.Items[0].Name);
        Assert.Equal("Foo.Bar", memberViewModel.Items[0].FullName);
        Assert.Equal("Foo.Bar.FooBar``1(System.Int32[],System.Byte*,``0,System.Collections.Generic.List{``0[]})", memberViewModel.Items[1].Uid);
        Assert.Equal("FooBar``1(System.Int32[],System.Byte*,``0,System.Collections.Generic.List{``0[]})", memberViewModel.Items[1].Id);
        Assert.Equal("FooBar<TArg>(int[], byte*, TArg, List<TArg[]>)", memberViewModel.Items[1].Name);
        Assert.Equal("Foo.Bar.FooBar<TArg>(int[], byte*, TArg, System.Collections.Generic.List<TArg[]>)", memberViewModel.Items[1].FullName);
        Assert.NotNull(memberViewModel.References.Find(
            s => s.Uid.Equals("System.Collections.Generic.List{System.String}")
            ));
        Assert.NotNull(memberViewModel.References.Find(
            s => s.Uid.Equals("System.Int32[]")
            ));
        Assert.NotNull(memberViewModel.References.Find(
            s => s.Uid.Equals("System.Byte*")
            ));
        Assert.NotNull(memberViewModel.References.Find(
            s => s.Uid.Equals("{TArg}")
            ));
        Assert.NotNull(memberViewModel.References.Find(
            s => s.Uid.Equals("System.Collections.Generic.List{{TArg}[]}")
            ));
    }
}
