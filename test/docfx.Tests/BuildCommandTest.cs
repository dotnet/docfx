// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Tests
{
    using EntityModel;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Xunit;

    [Collection("docfx STA")]
    public class BuildCommandTest
    {
        /// <summary>
        /// As similar to run docfx.exe directly: search for docfx.json in current directory
        /// </summary>
        [Fact]
        [Trait("Related", "docfx")]
        public void TestGetConfigWithNoInputAndDocfxJsonExists()
        {
            const string documentsBaseDir = "documents";
            const string outputBaseDir = "output";
            const string templateDir = "template";
            if (Directory.Exists(documentsBaseDir))
            {
                Directory.Delete(documentsBaseDir, true);
            }
            if (Directory.Exists(outputBaseDir))
            {
                Directory.Delete(outputBaseDir, true);
            }
            if (Directory.Exists(templateDir))
            {
                Directory.Delete(templateDir, true);
            }

            // Create default template
            var defaultTemplate = @"
{{{conceptual}}}
";
            Directory.CreateDirectory(Path.Combine(templateDir, "default"));
            Directory.CreateDirectory(documentsBaseDir);
            Directory.CreateDirectory(outputBaseDir);
            File.WriteAllText(Path.Combine(templateDir, "default", "Conceptual.html.tmpl"), defaultTemplate);

            // use `/` as it will be used in glob pattern
            // In glob pattern, `/` is considered as file separator while `\` is considered as escape character
            var conceptualFile = documentsBaseDir + "/test.md";
            File.WriteAllLines(
                conceptualFile,
                new[]
                {
                    "---",
                    "a: b",
                    "b:",
                    "  c: e",
                    "---",
                    "# Hello World",
                    "Test XRef: @XRef1",
                    "Test link: [link text](../documents/test.md)",
                    "<p>",
                    "test",
                });

            new BuildCommand(new Options
            {
                CurrentSubCommand = CommandType.Build,
                BuildCommand = new BuildCommandOptions
                {
                    Content = new List<string> { conceptualFile },
                    OutputFolder = Path.Combine(Environment.CurrentDirectory, outputBaseDir),
                    Templates = new List<string> { Path.Combine(templateDir, "default") }
                }
            }
            , null).Exec(null);

            var file = Path.Combine(outputBaseDir, Path.ChangeExtension(conceptualFile, ".html"));
            Assert.True(File.Exists(file));
            // TODO: Update when XREF is implemented by @zhyan
            Assert.Equal<string>(
                new string[]
                {
                    "",
                    "<h1 id=\"hello-world\">Hello World</h1>",
                    "<p>Test XRef: <span class=\"xref\">XRef1</span>",
                    "Test link: <a href=\"test.html\">link text</a></p>",
                    "<p><p>",
                    "test</p>",
                    ""
                },
                File.ReadAllLines(file));
        }
    }
}
