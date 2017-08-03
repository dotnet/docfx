// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Tests
{
    using System;
    using System.IO;

    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.SubCommands;

    using Xunit;
    using Newtonsoft.Json;

    [Collection("docfx STA")]
    public class ArgsParserTest
    {
        [Fact]
        [Trait("Related", "docfx")]
        public void TestCompositeCommandParser()
        {
            var args = new string[0];
            var controller = ArgsParser.Instance.Parse(args);
            var result = controller.TryGetCommandCreator("metadata", out ISubCommandCreator creator);
            Assert.True(result);
            result = controller.TryGetCommandCreator("build", out creator);
            Assert.True(result);

            Assert.Throws<FileNotFoundException>(() => controller.Create());
            args = new string[] { "-h", "other", "parameters" };

            controller = ArgsParser.Instance.Parse(args);
            var command = controller.Create();
            Assert.Equal(typeof(HelpCommand), command.GetType());

            args = new string[] { "Assets/docfx.json_metadata_build/docfx.json", "-f", "--invalid", "InvalidInput" };
            controller = ArgsParser.Instance.Parse(args);
            command = controller.Create();
            Assert.Equal(typeof(CompositeCommand), command.GetType());
            var commands = ((CompositeCommand)command).Commands;
            Assert.Equal(2, commands.Count);
            Assert.Equal(typeof(MetadataCommand), commands[0].GetType());
            var metadataCommand = (MetadataCommand)commands[0];
            Assert.Equal(2, metadataCommand.Config.Count);
            Assert.Equal(true, metadataCommand.Config[0].Force);
            Assert.Equal(true, metadataCommand.Config[1].Force);
            Assert.Equal(@"Assets\docfx.json_metadata_build", metadataCommand.BaseDirectory);
            Assert.Equal(typeof(BuildCommand), commands[1].GetType());
            var buildCommand = (BuildCommand)commands[1];
            Assert.Equal(true, buildCommand.Config.Force);
            Assert.Equal(@"Assets\docfx.json_metadata_build", buildCommand.Config.BaseDirectory);

            args = new string[] { "Assets/docfx.json_empty/docfx.json" };
            controller = ArgsParser.Instance.Parse(args);
            command = controller.Create();
            Assert.Equal(typeof(CompositeCommand), command.GetType());
            commands = ((CompositeCommand)command).Commands;
            Assert.Equal(0, commands.Count);

            args = new string[] { "Assets/docfx.json_invalid_key/docfx.json" };
            controller = ArgsParser.Instance.Parse(args);
            command = controller.Create();
            Assert.Equal(typeof(CompositeCommand), command.GetType());
            commands = ((CompositeCommand)command).Commands;
            Assert.Equal(0, commands.Count);

            args = new string[] { "Assets/docfx.json_invalid_format/docfx.json" };
            controller = ArgsParser.Instance.Parse(args);
            Assert.Throws<JsonSerializationException>(() => controller.Create());
        }

        [Fact]
        [Trait("Related", "docfx")]
        public void TestMetadataCommandParser()
        {
            var args = new string[] { "metadata", "--help" };
            var controller = ArgsParser.Instance.Parse(args);
            var command = controller.Create();
            Assert.Equal(typeof(HelpCommand), command.GetType());

            args = new string[] { "metadata" };
            controller = ArgsParser.Instance.Parse(args);
            Assert.Throws<OptionParserException>(() => controller.Create());

            args = new string[] { "metadata", "Assets/docfx.json_metadata_build/docfx.json", "-f" };
            controller = ArgsParser.Instance.Parse(args);
            command = controller.Create();
            Assert.Equal(typeof(MetadataCommand), command.GetType());
            var metadataCommand = (MetadataCommand)command;
            Assert.Equal(2, metadataCommand.Config.Count);
            Assert.Equal(true, metadataCommand.Config[0].Force);
            Assert.Equal(true, metadataCommand.Config[1].Force);
            Assert.Equal(@"Assets\docfx.json_metadata_build",  metadataCommand.BaseDirectory);
            Assert.Null(metadataCommand.OutputFolder);

            args = new string[] { "metadata", "Assets/docfx.json_empty/docfx.json", "A.csproj" };
            controller = ArgsParser.Instance.Parse(args);
            Assert.Throws<DocumentException>(() => controller.Create());

            args = new string[] { "metadata", "A.csproj", "-f" };
            controller = ArgsParser.Instance.Parse(args);
            command = controller.Create();
            Assert.Equal(typeof(MetadataCommand), command.GetType());
            metadataCommand = (MetadataCommand)command;
            Assert.Equal(1, metadataCommand.Config.Count);
            Assert.Equal(true, metadataCommand.Config[0].Force);
            Assert.Equal(true, metadataCommand.Config[0].Source.Expanded);
            Assert.Equal("A.csproj", metadataCommand.Config[0].Source.Items[0].Files[0]);
        }

        [Fact]
        [Trait("Related", "docfx")]
        public void TestBuildCommandParser()
        {
            var args = new string[] { "build", "--help" };
            var controller = ArgsParser.Instance.Parse(args);
            var command = controller.Create();
            Assert.Equal(typeof(HelpCommand), command.GetType());

            args = new string[] { "build" };
            controller = ArgsParser.Instance.Parse(args);
            Assert.Throws<OptionParserException>(() => controller.Create());

            args = new string[] { "build", "Assets/docfx.json_metadata_build/docfx.json", "-f", "-o", "output" };
            controller = ArgsParser.Instance.Parse(args);
            command = controller.Create();
            Assert.Equal(typeof(BuildCommand), command.GetType());
            var buildCommand = (BuildCommand)command;
            Assert.Equal(true, buildCommand.Config.Force);
            Assert.Equal(true, buildCommand.Config.ForcePostProcess);
            Assert.Equal(@"Assets\docfx.json_metadata_build", buildCommand.Config.BaseDirectory);
            Assert.Equal(@"output", buildCommand.Config.OutputFolder);

            args = new string[] { "build", "Assets/docfx.json_empty/docfx.json" };
            controller = ArgsParser.Instance.Parse(args);
            Assert.Throws<DocumentException>(() => controller.Create());

            args = new string[] { "build", "Assets/docfx.json_metadata_build/docfx.json", "-f", "--forcePostProcess" };
            controller = ArgsParser.Instance.Parse(args);
            command = controller.Create();
            Assert.Equal(typeof(BuildCommand), command.GetType());
            buildCommand = (BuildCommand)command;
            Assert.Equal(true, buildCommand.Config.Force);
            Assert.Equal(true, buildCommand.Config.ForcePostProcess);

            args = new string[] { "build", "Assets/docfx.json_metadata_build/docfx.json", "--forcePostProcess" };
            controller = ArgsParser.Instance.Parse(args);
            command = controller.Create();
            Assert.Equal(typeof(BuildCommand), command.GetType());
            buildCommand = (BuildCommand)command;
            Assert.Null(buildCommand.Config.Force);
            Assert.Equal(true, buildCommand.Config.ForcePostProcess);

            args = new string[] { "build", "Assets/docfx.json_metadata_build/docfx.json" };
            controller = ArgsParser.Instance.Parse(args);
            command = controller.Create();
            Assert.Equal(typeof(BuildCommand), command.GetType());
            buildCommand = (BuildCommand)command;
            Assert.Null(buildCommand.Config.Force);
            Assert.Null(buildCommand.Config.ForcePostProcess);
        }

        [Fact]
        [Trait("Related", "docfx")]
        public void TestInitCommandParser()
        {
            var args = new string[] { "init" };
            var controller = ArgsParser.Instance.Parse(args);
            var command = controller.Create();
            Assert.Equal(typeof(InitCommand), command.GetType());

            args = new string[] { "init", "--help" };
            controller = ArgsParser.Instance.Parse(args);
            command = controller.Create();
            Assert.Equal(typeof(HelpCommand), command.GetType());
        }

        [Fact]
        [Trait("Related", "docfx")]
        public void TestHelpCommandParser()
        {
            var args = new string[] { "help" };
            var controller = ArgsParser.Instance.Parse(args);
            var command = controller.Create();
            Assert.Equal(typeof(HelpCommand), command.GetType());
        }

        [Fact]
        [Trait("Related", "docfx")]
        public void TestServeCommandParser()
        {
            var args = new string[] { "serve" };
            var controller = ArgsParser.Instance.Parse(args);
            var command = controller.Create();
            Assert.Equal(typeof(ServeCommand), command.GetType());

            args = new string[] { "serve", "-h" };
            controller = ArgsParser.Instance.Parse(args);
            command = controller.Create();
            Assert.Equal(typeof(HelpCommand), command.GetType());

            args = new string[] { "serve", "--invalid" };
            controller = ArgsParser.Instance.Parse(args);
            Assert.Throws<OptionParserException>(() => controller.Create());
        }
    }
}
