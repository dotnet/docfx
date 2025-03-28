// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Common;
using Docfx.DataContracts.Common;
using Docfx.DataContracts.ManagedReference;
using Docfx.Dotnet;
using Docfx.Tests.Common;

namespace Docfx.Tests;

[DoNotParallelize]
[TestClass]
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

    [TestMethod]
    [TestProperty("Related", "docfx")]
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

    [TestMethod]
    [TestProperty("Related", "docfx")]
    public async Task TestMetadataCommandFromDll()
    {
        var dllFile = Path.Combine(_projectFolder, "test.dll");
        File.Copy("Assets/test.dll.sample.1", dllFile);

        await DotnetApiCatalog.Exec(
            new(new MetadataJsonItemConfig { Dest = _outputFolder, Src = new(new FileMappingItem(dllFile)) { Expanded = true } }),
            new(), Directory.GetCurrentDirectory());

        CheckResult();
    }

    [TestMethod]
    [TestProperty("Related", "docfx")]
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

    [TestMethod]
    [TestProperty("Related", "docfx")]
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

        Assert.IsTrue(File.Exists(Path.Combine(_outputFolder, ".manifest")));

        var file = Path.Combine(_outputFolder, "toc.yml");
        Assert.IsTrue(File.Exists(file));
        var tocViewModel = YamlUtility.Deserialize<TocItemViewModel>(file).Items;
        Assert.AreEqual("testVBproj1.Foo", tocViewModel[0].Uid);
        Assert.AreEqual("testVBproj1.Foo", tocViewModel[0].Name);
        Assert.AreEqual("testVBproj1.Foo.Bar", tocViewModel[0].Items[0].Uid);
        Assert.AreEqual("Bar", tocViewModel[0].Items[0].Name);

        file = Path.Combine(_outputFolder, "testVBproj1.Foo.yml");
        Assert.IsTrue(File.Exists(file));
        var memberViewModel = YamlUtility.Deserialize<PageViewModel>(file);
        Assert.AreEqual("testVBproj1.Foo", memberViewModel.Items[0].Uid);
        Assert.AreEqual("testVBproj1.Foo", memberViewModel.Items[0].Id);
        Assert.AreEqual("testVBproj1.Foo", memberViewModel.Items[0].Name);
        Assert.AreEqual("testVBproj1.Foo", memberViewModel.Items[0].FullName);

        file = Path.Combine(_outputFolder, "testVBproj1.Foo.Bar.yml");
        Assert.IsTrue(File.Exists(file));
        memberViewModel = YamlUtility.Deserialize<PageViewModel>(file);
        Assert.AreEqual("testVBproj1.Foo.Bar", memberViewModel.Items[0].Uid);
        Assert.AreEqual("Bar", memberViewModel.Items[0].Id);
        Assert.AreEqual("Bar", memberViewModel.Items[0].Name);
        Assert.AreEqual("testVBproj1.Foo.Bar", memberViewModel.Items[0].FullName);
        Assert.AreEqual("testVBproj1.Foo.Bar.FooBar``1(System.Int32[],System.Byte,``0,System.Collections.Generic.List{``0[]})", memberViewModel.Items[1].Uid);
        Assert.AreEqual("FooBar``1(System.Int32[],System.Byte,``0,System.Collections.Generic.List{``0[]})", memberViewModel.Items[1].Id);
        Assert.AreEqual("FooBar<TArg>(int[], byte, TArg, List<TArg[]>)", memberViewModel.Items[1].Name);
        Assert.AreEqual("testVBproj1.Foo.Bar.FooBar<TArg>(int[], byte, TArg, System.Collections.Generic.List<TArg[]>)", memberViewModel.Items[1].FullName);
        Assert.IsNotNull(memberViewModel.References.Find(
            s => s.Uid.Equals("System.Collections.Generic.List{System.String}")
            ));
        Assert.IsNotNull(memberViewModel.References.Find(
            s => s.Uid.Equals("System.Int32[]")
            ));
        Assert.IsNotNull(memberViewModel.References.Find(
            s => s.Uid.Equals("System.Byte")
            ));
        Assert.IsNotNull(memberViewModel.References.Find(
            s => s.Uid.Equals("{TArg}")
            ));
        Assert.IsNotNull(memberViewModel.References.Find(
            s => s.Uid.Equals("System.Collections.Generic.List{{TArg}[]}")
            ));
    }

    [TestMethod]
    [TestProperty("Related", "docfx")]
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

        Assert.IsTrue(File.Exists(Path.Combine(_outputFolder, ".manifest")));

        var file = Path.Combine(_outputFolder, "toc.yml");
        Assert.IsTrue(File.Exists(file));
        var tocViewModel = YamlUtility.Deserialize<TocItemViewModel>(file).Items;
        Assert.AreEqual("Foo", tocViewModel[0].Uid);
        Assert.AreEqual("Foo", tocViewModel[0].Name);
        Assert.AreEqual("Foo.Bar", tocViewModel[0].Items[0].Uid);
        Assert.AreEqual("Bar", tocViewModel[0].Items[0].Name);

        file = Path.Combine(_outputFolder, "Foo.yml");
        Assert.IsTrue(File.Exists(file));
        var memberViewModel = YamlUtility.Deserialize<PageViewModel>(file);
        Assert.AreEqual("Foo", memberViewModel.Items[0].Uid);
        Assert.AreEqual("Foo", memberViewModel.Items[0].Id);
        Assert.AreEqual("Foo", memberViewModel.Items[0].Name);
        Assert.AreEqual("Foo", memberViewModel.Items[0].FullName);

        file = Path.Combine(_outputFolder, "Foo.Bar.yml");
        Assert.IsTrue(File.Exists(file));
        memberViewModel = YamlUtility.Deserialize<PageViewModel>(file);
        Assert.AreEqual("Foo.Bar", memberViewModel.Items[0].Uid);
        Assert.AreEqual("Bar", memberViewModel.Items[0].Id);
        Assert.AreEqual("Bar", memberViewModel.Items[0].Name);
        Assert.AreEqual("Foo.Bar", memberViewModel.Items[0].FullName);
        Assert.ContainsSingle(memberViewModel.Items);
        Assert.IsNotNull(memberViewModel.References.Find(s => s.Uid.Equals("Foo")));
    }

    [TestMethod]
    [TestProperty("Related", "docfx")]
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

    [TestMethod]
    [TestProperty("Related", "docfx")]
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
        Assert.IsTrue(File.Exists(file));
        var tocViewModel = YamlUtility.Deserialize<TocItemViewModel>(file).Items;
        Assert.AreEqual("OtherNamespace", tocViewModel[0].Uid);
        Assert.AreEqual("OtherNamespace", tocViewModel[0].Name);

        Assert.AreEqual("OtherNamespace.OtherBar", tocViewModel[0].Items[0].Uid);
        Assert.AreEqual("OtherBar", tocViewModel[0].Items[0].Name);

        Assert.AreEqual("Samples.Foo", tocViewModel[1].Uid);
        Assert.AreEqual("Samples.Foo", tocViewModel[1].Name);

        Assert.AreEqual("Samples.Foo.Sub", tocViewModel[1].Items[0].Uid);
        Assert.AreEqual("Sub", tocViewModel[1].Items[0].Name);
        Assert.AreEqual("Samples.Foo.Sub.SubBar", tocViewModel[1].Items[0].Items[0].Uid);
        Assert.AreEqual("SubBar", tocViewModel[1].Items[0].Items[0].Name);

        Assert.AreEqual("Samples.Foo.Bar", tocViewModel[1].Items[1].Uid);
        Assert.AreEqual("Bar", tocViewModel[1].Items[1].Name);
    }

    [TestMethod]
    [TestProperty("Related", "docfx")]
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
        Assert.IsTrue(File.Exists(file));
        var tocViewModel = YamlUtility.Deserialize<TocItemViewModel>(file).Items;
        Assert.AreEqual("OtherNamespace", tocViewModel[0].Uid);
        Assert.AreEqual("OtherNamespace", tocViewModel[0].Name);

        Assert.AreEqual("OtherNamespace.OtherBar", tocViewModel[0].Items[0].Uid);
        Assert.AreEqual("OtherBar", tocViewModel[0].Items[0].Name);

        Assert.AreEqual("Samples.Foo", tocViewModel[1].Uid);
        Assert.AreEqual("Samples.Foo", tocViewModel[1].Name);
        Assert.AreEqual("Samples.Foo.Bar", tocViewModel[1].Items[0].Uid);
        Assert.AreEqual("Bar", tocViewModel[1].Items[0].Name);

        Assert.AreEqual("Samples.Foo.Sub", tocViewModel[2].Uid);
        Assert.AreEqual("Samples.Foo.Sub", tocViewModel[2].Name);

        Assert.AreEqual("Samples.Foo.Sub.SubBar", tocViewModel[2].Items[0].Uid);
        Assert.AreEqual("SubBar", tocViewModel[2].Items[0].Name);
    }

    [TestMethod]
    [TestProperty("Related", "docfx")]
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
        Assert.IsTrue(File.Exists(file));
        var tocViewModel = YamlUtility.Deserialize<TocItemViewModel>(file).Items;
        Assert.AreEqual("OtherNamespace", tocViewModel[0].Uid);
        Assert.AreEqual("OtherNamespace", tocViewModel[0].Name);

        Assert.AreEqual("OtherNamespace.OtherBar", tocViewModel[0].Items[0].Uid);
        Assert.AreEqual("OtherBar", tocViewModel[0].Items[0].Name);

        Assert.AreEqual("Samples.Foo", tocViewModel[1].Uid);
        Assert.AreEqual("Samples.Foo", tocViewModel[1].Name);

        Assert.AreEqual("Samples.Foo.Sub", tocViewModel[1].Items[0].Uid);
        Assert.AreEqual("Sub", tocViewModel[1].Items[0].Name);
        Assert.AreEqual("Samples.Foo.Sub.Subber1", tocViewModel[1].Items[0].Items[0].Uid);
        Assert.AreEqual("Subber1", tocViewModel[1].Items[0].Items[0].Name);
        Assert.AreEqual("Samples.Foo.Sub.Subber1.SubberBar", tocViewModel[1].Items[0].Items[0].Items[0].Uid);
        Assert.AreEqual("SubberBar", tocViewModel[1].Items[0].Items[0].Items[0].Name);

        Assert.AreEqual("Samples.Foo.Sub.Subber2", tocViewModel[1].Items[0].Items[1].Uid);
        Assert.AreEqual("Subber2", tocViewModel[1].Items[0].Items[1].Name);
        Assert.AreEqual("Samples.Foo.Sub.Subber2.Subber2Bar", tocViewModel[1].Items[0].Items[1].Items[0].Uid);
        Assert.AreEqual("Subber2Bar", tocViewModel[1].Items[0].Items[1].Items[0].Name);

        Assert.AreEqual("Samples.Foo.Bar", tocViewModel[1].Items[1].Uid);
        Assert.AreEqual("Bar", tocViewModel[1].Items[1].Name);
    }

    private void CheckResult()
    {
        Assert.IsTrue(File.Exists(Path.Combine(_outputFolder, ".manifest")));

        var file = Path.Combine(_outputFolder, "toc.yml");
        Assert.IsTrue(File.Exists(file));
        var tocViewModel = YamlUtility.Deserialize<TocItemViewModel>(file).Items;
        Assert.AreEqual("Foo", tocViewModel[0].Uid);
        Assert.AreEqual("Foo", tocViewModel[0].Name);
        Assert.AreEqual("Foo.Bar", tocViewModel[0].Items[0].Uid);
        Assert.AreEqual("Bar", tocViewModel[0].Items[0].Name);

        file = Path.Combine(_outputFolder, "Foo.yml");
        Assert.IsTrue(File.Exists(file));
        var memberViewModel = YamlUtility.Deserialize<PageViewModel>(file);
        Assert.AreEqual("Foo", memberViewModel.Items[0].Uid);
        Assert.AreEqual("Foo", memberViewModel.Items[0].Id);
        Assert.AreEqual("Foo", memberViewModel.Items[0].Name);
        Assert.AreEqual("Foo", memberViewModel.Items[0].FullName);

        file = Path.Combine(_outputFolder, "Foo.Bar.yml");
        Assert.IsTrue(File.Exists(file));
        memberViewModel = YamlUtility.Deserialize<PageViewModel>(file);
        Assert.AreEqual("Foo.Bar", memberViewModel.Items[0].Uid);
        Assert.AreEqual("Bar", memberViewModel.Items[0].Id);
        Assert.AreEqual("Bar", memberViewModel.Items[0].Name);
        Assert.AreEqual("Foo.Bar", memberViewModel.Items[0].FullName);
        Assert.AreEqual("Foo.Bar.FooBar``1(System.Int32[],System.Byte*,``0,System.Collections.Generic.List{``0[]})", memberViewModel.Items[1].Uid);
        Assert.AreEqual("FooBar``1(System.Int32[],System.Byte*,``0,System.Collections.Generic.List{``0[]})", memberViewModel.Items[1].Id);
        Assert.AreEqual("FooBar<TArg>(int[], byte*, TArg, List<TArg[]>)", memberViewModel.Items[1].Name);
        Assert.AreEqual("Foo.Bar.FooBar<TArg>(int[], byte*, TArg, System.Collections.Generic.List<TArg[]>)", memberViewModel.Items[1].FullName);
        Assert.IsNotNull(memberViewModel.References.Find(
            s => s.Uid.Equals("System.Collections.Generic.List{System.String}")
            ));
        Assert.IsNotNull(memberViewModel.References.Find(
            s => s.Uid.Equals("System.Int32[]")
            ));
        Assert.IsNotNull(memberViewModel.References.Find(
            s => s.Uid.Equals("System.Byte*")
            ));
        Assert.IsNotNull(memberViewModel.References.Find(
            s => s.Uid.Equals("{TArg}")
            ));
        Assert.IsNotNull(memberViewModel.References.Find(
            s => s.Uid.Equals("System.Collections.Generic.List{{TArg}[]}")
            ));
    }
}
