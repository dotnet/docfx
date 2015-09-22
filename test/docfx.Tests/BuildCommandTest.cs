// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Tests
{
    using EntityModel;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using Xunit;

    public class BuildCommandTest
    {
        /// <summary>
        /// As similar to run docfx.exe directly: search for docfx.json in current directory
        /// </summary>
        [Fact]
        [Trait("Related", "docfx")]
        public void TestGetConfig_WithNoInputAndDocfxJsonExists()
        {
            const string documentsBaseDir = "documents";
            const string outputBaseDir = "output";
            if (Directory.Exists(documentsBaseDir))
            {
                Directory.Delete(documentsBaseDir, true);
            }
            if (Directory.Exists(outputBaseDir))
            {
                Directory.Delete(outputBaseDir, true);
            }
            Directory.CreateDirectory(documentsBaseDir);
            Directory.CreateDirectory(outputBaseDir);
            var conceptualFile = Path.Combine(documentsBaseDir, "test.md");
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
                    "Test link: [link text](test/test.md)",
                    "<p>",
                    "test",
                });

           ParseResult result = new BuildCommand(new BuildCommandOptions
            {
                Content = new List<string> { conceptualFile },
                OutputFolder = Path.Combine(Environment.CurrentDirectory, outputBaseDir),
            }).Exec(null);

            Assert.Equal(ResultLevel.Success, result.ResultLevel);
            Assert.True(File.Exists(Path.Combine(outputBaseDir, Path.ChangeExtension(conceptualFile, ".yml"))));
            var model = YamlUtility.Deserialize<Dictionary<string, object>>(Path.Combine(outputBaseDir, Path.ChangeExtension(conceptualFile, ".yml")));
            Assert.Equal(
                "<h1 id=\"hello-world\">Hello World</h1>\n" +
                "<p>Test XRef: <xref href=\"XRef1\"></xref>\n" +
                "Test link: <a href=\"~/documents/test/test.md\">link text</a></p>\n" +
                "<p><p>\n" +
                "test</p>\n",
                model["conceptual"]);
            Assert.Equal("Conceptual", model["type"]);
            Assert.Equal("b", model["a"]);
            Assert.IsType<Dictionary<object, object>>(model["b"]);
            Assert.Equal("e", ((Dictionary<object, object>)model["b"])["c"]);

            Directory.Delete(documentsBaseDir, true);
            Directory.Delete(outputBaseDir, true);
        }
    }
}
