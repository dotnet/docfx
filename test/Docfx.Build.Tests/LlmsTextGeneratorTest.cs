// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Build.Engine;
using Docfx.Plugins;
using Docfx.Tests.Common;

using Xunit;

namespace Docfx.Build.Tests
{
    public class LlmsTextGeneratorTest : TestBase
    {
        [Fact]
        public void TestGenerateLlmsText()
        {
            // Arrange
            var manifest = new Manifest
            {
                LlmsText = new LlmsTextOptions
                {
                    Title = "Test Project",
                    Summary = "A test project for generating llms.txt",
                    Details = "This is a detailed description of the project.",
                    Sections = new Dictionary<string, List<LlmsTextLink>>
                    {
                        ["Main"] = new List<LlmsTextLink>
                        {
                            new LlmsTextLink
                            {
                                Title = "Documentation",
                                Url = "https://example.com/docs",
                                Description = "Main documentation site"
                            },
                            new LlmsTextLink
                            {
                                Title = "API Reference",
                                Url = "https://example.com/api"
                            }
                        },
                        ["Optional"] = new List<LlmsTextLink>
                        {
                            new LlmsTextLink
                            {
                                Title = "Examples",
                                Url = "https://example.com/examples",
                                Description = "Code examples"
                            }
                        }
                    }
                }
            };

            var outputFolder = Path.GetFullPath("output");
            if (Directory.Exists(outputFolder))
            {
                Directory.Delete(outputFolder, true);
            }
            Directory.CreateDirectory(outputFolder);

            // Act
            var generator = new LlmsTextGenerator();
            generator.Process(manifest, outputFolder);

            // Assert
            var llmsTextPath = Path.Combine(outputFolder, "llms.txt");
            Assert.True(File.Exists(llmsTextPath));

            var content = File.ReadAllText(llmsTextPath);
            Assert.Contains("# Test Project", content);
            Assert.Contains("> A test project for generating llms.txt", content);
            Assert.Contains("This is a detailed description of the project.", content);
            Assert.Contains("## Main", content);
            Assert.Contains("- [Documentation](https://example.com/docs): Main documentation site", content);
            Assert.Contains("- [API Reference](https://example.com/api)", content);
            Assert.Contains("## Optional", content);
            Assert.Contains("- [Examples](https://example.com/examples): Code examples", content);
        }
    }
}