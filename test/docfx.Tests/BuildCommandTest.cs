// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    using Xunit;

    using Microsoft.DocAsCode.SubCommands;
    using Microsoft.DocAsCode.Tests.Common;

    [Collection("docfx STA")]
    public class BuildCommandTest : TestBase
    {
        private string _outputFolder;
        private string _inputFolder;
        private string _templateFolder;

        public BuildCommandTest()
        {
            _outputFolder = GetRandomFolder();
            _inputFolder = GetRandomFolder();
            _templateFolder = GetRandomFolder();
        }

        /// <summary>
        /// As similar to run docfx.exe directly: search for docfx.json in current directory
        /// </summary>
        [Fact]
        [Trait("Related", "docfx")]
        public void TestGetConfigWithNoInputAndDocfxJsonExists()
        {
            // Create default template
            var defaultTemplate = @"
{{{rawTitle}}}{{{conceptual}}}
";
            Directory.CreateDirectory(Path.Combine(_templateFolder, "default"));
            Directory.CreateDirectory(_inputFolder);
            Directory.CreateDirectory(_outputFolder);
            File.WriteAllText(Path.Combine(_templateFolder, "default", "Conceptual.html.tmpl"), defaultTemplate);

            // use `/` as it will be used in glob pattern
            // In glob pattern, `/` is considered as file separator while `\` is considered as escape character
            var conceptualFile1 = _inputFolder + "/test1.md";
            File.WriteAllLines(
                conceptualFile1,
                new[]
                {
                    "---",
                    "uid: xref1",
                    "a: b",
                    "b:",
                    "  c: e",
                    "---",
                    "# Hello Test1",
                    "Test XRef: @xref2",
                    "Test XRef: @unknown_xref",
                    "Test link: [link text](test2.md)",
                    "<p>",
                    "test",
                });
            var conceptualFile2 = _inputFolder + "/test2.md";
            File.WriteAllLines(
                conceptualFile2,
                new[]
                {
                    "---",
                    "uid: xref2",
                    "a: b",
                    "b:",
                    "  c: e",
                    "---",
                    "# Hello World",
                    "Test XRef Shortcut: [Xref](@xref1 \"shortcut\")",
                    "Test XRef: [](xref:xref1)",
                    "Test XRef auto link: <xref:xref1>",
                    "Test link: [link text](test1.md)",
                    "<p>",
                    "test",
                });
            new BuildCommand(new BuildCommandOptions
            {
                Content = new List<string> { conceptualFile1, conceptualFile2 },
                OutputFolder = Path.Combine(Environment.CurrentDirectory, _outputFolder),
                Templates = new List<string> { Path.Combine(_templateFolder, "default") }
            }).Exec(null);

            var file = Path.Combine(_outputFolder, Path.ChangeExtension(conceptualFile1, ".html"));
            Assert.True(File.Exists(file));
            Assert.Equal<string>(
                new string[]
                {
                    "",
                    "<h1 id=\"hello-test1\">Hello Test1</h1>",
                    "<p>Test XRef: <a class=\"xref\" href=\"test2.html#xref2\">Hello World</a>",
                    "Test XRef: @unknown_xref",
                    "Test link: <a href=\"test2.html\">link text</a></p>",
                    "<p><p>",
                    "test</p>",
                    ""
                },
                File.ReadAllLines(file));

            file = Path.Combine(_outputFolder, Path.ChangeExtension(conceptualFile2, ".html"));
            Assert.True(File.Exists(file));
            Assert.Equal<string>(
                new string[]
                {
                    "",
                    "<h1 id=\"hello-world\">Hello World</h1>",
                    "<p>Test XRef Shortcut: <a class=\"xref\" href=\"test1.html#xref1\" title=\"shortcut\">Xref</a>",
                    "Test XRef: <a class=\"xref\" href=\"test1.html#xref1\">Hello Test1</a>",
                    "Test XRef auto link: <a class=\"xref\" href=\"test1.html#xref1\">Hello Test1</a>",
                    "Test link: <a href=\"test1.html\">link text</a></p>",
                    "<p><p>",
                    "test</p>",
                    ""
                },
                File.ReadAllLines(file));
        }
    }
}
