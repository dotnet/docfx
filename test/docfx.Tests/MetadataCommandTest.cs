// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    using Xunit;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.DataContracts.ManagedReference;
    using Microsoft.DocAsCode.SubCommands;
    using Microsoft.DocAsCode.Tests.Common;

    [Collection("docfx STA")]
    public class MetadataCommandTest : TestBase
    {
        /// <summary>
        /// Use MetadataCommand to generate YAML files from a c# project and a VB project separately
        /// </summary>
        private string _outputFolder;
        private string _projectFolder;

        public MetadataCommandTest()
        {
            _outputFolder = GetRandomFolder();
            _projectFolder = GetRandomFolder();
        }

        [Fact]
        [Trait("Related", "docfx")]
        [Trait("Language", "CSharp")]
        public void TestMetadataCommandFromCSProject()
        {
            // Create default project
            var projectFile = Path.Combine(_projectFolder, "test.csproj");
            var sourceFile = Path.Combine(_projectFolder, "test.cs");
            File.Copy("Assets/test.csproj.sample.1", projectFile);
            File.Copy("Assets/test.cs.sample.1", sourceFile);

            new MetadataCommand(new MetadataCommandOptions
            {
                OutputFolder = Path.Combine(Directory.GetCurrentDirectory(), _outputFolder),
                Projects = new List<string> { projectFile },
            }).Exec(null);

            Assert.True(File.Exists(Path.Combine(_outputFolder, ".manifest")));

            var file = Path.Combine(_outputFolder, "toc.yml");
            Assert.True(File.Exists(file));
            var tocViewModel = YamlUtility.Deserialize<TocViewModel>(file);
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
            Assert.Equal("FooBar<TArg>(Int32[], Byte*, TArg, List<TArg[]>)", memberViewModel.Items[1].Name);
            Assert.Equal("Foo.Bar.FooBar<TArg>(System.Int32[], System.Byte*, TArg, System.Collections.Generic.List<TArg[]>)", memberViewModel.Items[1].FullName);
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

        [Fact]
        [Trait("Related", "docfx")]
        [Trait("Language", "CSharp")]
        [Trait("Framework", "netcore")]
        public void TestMetadataCommandFromNet46CSProject()
        {
            // Create default project
            var projectFile = Path.Combine(_projectFolder, "net46-test.csproj");
            var sourceFile = Path.Combine(_projectFolder, "test.cs");
            File.Copy("Assets/net46-test.csproj.sample.1", projectFile);
            File.Copy("Assets/test.cs.sample.1", sourceFile);

            new MetadataCommand(new MetadataCommandOptions
            {
                OutputFolder = Path.Combine(Directory.GetCurrentDirectory(), _outputFolder),
                Projects = new List<string> { projectFile },
            }).Exec(null);

            Assert.True(File.Exists(Path.Combine(_outputFolder, ".manifest")));

            var file = Path.Combine(_outputFolder, "toc.yml");
            Assert.True(File.Exists(file));
            var tocViewModel = YamlUtility.Deserialize<TocViewModel>(file);
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
            Assert.Equal("FooBar<TArg>(Int32[], Byte*, TArg, List<TArg[]>)", memberViewModel.Items[1].Name);
            Assert.Equal("Foo.Bar.FooBar<TArg>(System.Int32[], System.Byte*, TArg, System.Collections.Generic.List<TArg[]>)", memberViewModel.Items[1].FullName);
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

        [Fact]
        [Trait("Related", "docfx")]
        [Trait("Language", "CSharp")]
        [Trait("Framework", "netcore")]
        public void TestMetadataCommandFromMultipleFrameworksCSProject()
        {
            // Create default project
            var projectFile = Path.Combine(_projectFolder, "multi-frameworks-test.csproj");
            var sourceFile = Path.Combine(_projectFolder, "test.cs");
            File.Copy("Assets/multi-frameworks-test.csproj.sample.1", projectFile);
            File.Copy("Assets/test.cs.sample.1", sourceFile);

            new MetadataCommand(new MetadataCommandOptions
            {
                OutputFolder = Path.Combine(Directory.GetCurrentDirectory(), _outputFolder),
                Projects = new List<string> { projectFile },
                MSBuildProperties = "TargetFramework=net46"
            }).Exec(null);

            Assert.True(File.Exists(Path.Combine(_outputFolder, ".manifest")));

            var file = Path.Combine(_outputFolder, "toc.yml");
            Assert.True(File.Exists(file));
            var tocViewModel = YamlUtility.Deserialize<TocViewModel>(file);
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
            Assert.Equal("FooBar<TArg>(Int32[], Byte*, TArg, List<TArg[]>)", memberViewModel.Items[1].Name);
            Assert.Equal("Foo.Bar.FooBar<TArg>(System.Int32[], System.Byte*, TArg, System.Collections.Generic.List<TArg[]>)", memberViewModel.Items[1].FullName);
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

        [Fact]
        [Trait("Related", "docfx")]
        [Trait("Language", "VB")]
        public void TestMetadataCommandFromVBProject()
        {
            // Create default project
            var projectFile = Path.Combine(_projectFolder, "test.vbproj");
            var sourceFile = Path.Combine(_projectFolder, "test.vb");
            File.Copy("Assets/test.vbproj.sample.1", projectFile);
            File.Copy("Assets/test.vb.sample.1", sourceFile);

            new MetadataCommand(new MetadataCommandOptions
            {
                OutputFolder = Path.Combine(Directory.GetCurrentDirectory(), _outputFolder),
                Projects = new List<string> { projectFile },
            }).Exec(null);
            Assert.True(File.Exists(Path.Combine(_outputFolder, ".manifest")));

            var file = Path.Combine(_outputFolder, "toc.yml");
            Assert.True(File.Exists(file));
            var tocViewModel = YamlUtility.Deserialize<TocViewModel>(file);
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
            Assert.Equal("FooBar<TArg>(Int32[], Byte, TArg, List<TArg[]>)", memberViewModel.Items[1].Name);
            Assert.Equal("testVBproj1.Foo.Bar.FooBar<TArg>(System.Int32[], System.Byte, TArg, System.Collections.Generic.List<TArg[]>)", memberViewModel.Items[1].FullName);
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
        [Trait("Language", "CSharp")]
        public void TestMetadataCommandFromCSProjectWithFilterInConfig()
        {
            // Create default project
            Directory.CreateDirectory(Path.Combine(_projectFolder, "src"));
            Directory.CreateDirectory(Path.Combine(_projectFolder, "doc"));
            var projectFile = Path.Combine(_projectFolder, "src", "test.csproj");
            var sourceFile = Path.Combine(_projectFolder, "src", "test.cs");
            var docfxFile = Path.Combine(_projectFolder, "doc", "docfx.json");
            var filterFile = Path.Combine(_projectFolder, "doc", "filter.yaml");
            File.Copy("Assets/test.csproj.sample.1", projectFile);
            File.Copy("Assets/test.cs.sample.1", sourceFile);
            File.Copy("Assets/docfx.json_metadata/docfxWithFilter.json", docfxFile);
            File.Copy("Assets/filter.yaml.sample", filterFile);

            new MetadataCommand(
                new MetadataCommandOptions
                {
                    Projects = new List<string> { docfxFile },
                    OutputFolder = Path.GetFullPath(_outputFolder),
                }).Exec(null);

            Assert.True(File.Exists(Path.Combine(_outputFolder, ".manifest")));

            var file = Path.Combine(_outputFolder, "toc.yml");
            Assert.True(File.Exists(file));
            var tocViewModel = YamlUtility.Deserialize<TocViewModel>(file);
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
            Assert.Equal(1, memberViewModel.Items.Count);
            Assert.NotNull(memberViewModel.References.Find(s => s.Uid.Equals("Foo")));
        }

        [Fact]
        [Trait("Related", "docfx")]
        [Trait("Language", "CSharp")]
        public void TestMetadataCommandFromCSProjectWithFilterInOption()
        {
            // Create default project
            var projectFile = Path.Combine(_projectFolder, "test.csproj");
            var sourceFile = Path.Combine(_projectFolder, "test.cs");
            var filterFile = Path.Combine(_projectFolder, "filter.yaml");
            File.Copy("Assets/test.csproj.sample.1", projectFile);
            File.Copy("Assets/test.cs.sample.1", sourceFile);
            File.Copy("Assets/filter.yaml.sample", filterFile);

            new MetadataCommand(new MetadataCommandOptions
            {
                OutputFolder = Path.Combine(Directory.GetCurrentDirectory(), _outputFolder),
                Projects = new List<string> { projectFile },
                FilterConfigFile = filterFile,
            }).Exec(null);

            Assert.True(File.Exists(Path.Combine(_outputFolder, ".manifest")));

            var file = Path.Combine(_outputFolder, "toc.yml");
            Assert.True(File.Exists(file));
            var tocViewModel = YamlUtility.Deserialize<TocViewModel>(file);
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
            Assert.Equal(1, memberViewModel.Items.Count);
            Assert.NotNull(memberViewModel.References.Find(s => s.Uid.Equals("Foo")));
        }

        [Fact]
        [Trait("Related", "docfx")]
        [Trait("Language", "CSharp")]
        public void TestMetadataCommandFromCSProjectWithDuplicateProjectReference()
        {
            // Create default project
            var projectFile = Path.Combine(_projectFolder, "test.csproj");
            var refProjectFile = Path.Combine(_projectFolder, "ref.csproj");
            var sourceFile = Path.Combine(_projectFolder, "test.cs");
            File.Copy("Assets/test.csproj.sample.1", projectFile);
            File.Copy("Assets/ref.csproj.sample.1", refProjectFile);
            File.Copy("Assets/test.cs.sample.1", sourceFile);

            new MetadataCommand(new MetadataCommandOptions
            {
                OutputFolder = Path.Combine(Directory.GetCurrentDirectory(), _outputFolder),
                Projects = new List<string> { projectFile },
            }).Exec(null);

            Assert.True(File.Exists(Path.Combine(_outputFolder, ".manifest")));

            var file = Path.Combine(_outputFolder, "toc.yml");
            Assert.True(File.Exists(file));
            var tocViewModel = YamlUtility.Deserialize<TocViewModel>(file);
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
            Assert.Equal("FooBar<TArg>(Int32[], Byte*, TArg, List<TArg[]>)", memberViewModel.Items[1].Name);
            Assert.Equal("Foo.Bar.FooBar<TArg>(System.Int32[], System.Byte*, TArg, System.Collections.Generic.List<TArg[]>)", memberViewModel.Items[1].FullName);
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
}