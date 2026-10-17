// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Docfx.Plugins;
using Docfx.Tests.Common;
using HtmlAgilityPack;

using Xunit;

namespace Docfx.Build.Engine.Tests;

[Collection("docfx STA")]
public class RemoveDebugInfoTest : TestBase
{
    private readonly string _outputFolder;

    public RemoveDebugInfoTest()
    {
        _outputFolder = GetRandomFolder();
        EnvironmentContext.SetBaseDirectory(_outputFolder);
        EnvironmentContext.SetOutputDirectory(_outputFolder);
    }

    public override void Dispose()
    {
        EnvironmentContext.Clean();
        base.Dispose();
    }

    [Fact]
    public void TestBasicFeature()
    {
        Manifest manifest = new()
        {
            SourceBasePath = _outputFolder,
            Files =
            {
                new ManifestItem { SourceRelativePath = "a.md", Output = { { ".html", new OutputFileInfo { RelativePath = "a.html" } } } },
            }
        };

        File.WriteAllText(Path.Combine(_outputFolder, "a.html"), @"<p id='b1' sourceFile='a.md' sourceStartLineNumber='1' sourceEndLineNumber='2'>section<a sourcefile=""a.md"" href='http://bing.com#top'>Microsoft Bing</a></p>");

        new HtmlPostProcessor
        {
            Handlers = { new RemoveDebugInfo() }
        }.Process(manifest, _outputFolder, TestContext.Current.CancellationToken);

        var actual = File.ReadAllText(Path.Combine(_outputFolder, "a.html"));
        Assert.Equal("<p id='b1'>section<a href='http://bing.com#top'>Microsoft Bing</a></p>", actual);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("1", true)]
    [InlineData("0", false)]
    [InlineData("true", true)]
    [InlineData("TRUE", true)]
    [InlineData(" true ", true)]
    [InlineData("false", false)]
    [InlineData("FALSE", false)]
    [InlineData("invalid", false)]
    public void KeepDebugInfoEnvironmentVariableControlsOutput(string value, bool keepDebugInfo)
    {
        const string name = "DOCFX_KEEP_DEBUG_INFO";
        var original = Environment.GetEnvironmentVariable(name);
        try
        {
            Environment.SetEnvironmentVariable(name, value);
            Manifest manifest = new()
            {
                SourceBasePath = _outputFolder,
                Files =
                {
                    new ManifestItem { SourceRelativePath = "a.md", Output = { { ".html", new OutputFileInfo { RelativePath = "a.html" } } } },
                }
            };
            var outputFile = Path.Combine(_outputFolder, "a.html");
            File.WriteAllText(outputFile, "<p sourceFile='a.md'>content</p>");

            var processor = new HtmlPostProcessor();
            processor.PrepareMetadata(ImmutableDictionary<string, object>.Empty);
            processor.Process(manifest, _outputFolder, TestContext.Current.CancellationToken);

            var document = new HtmlDocument();
            document.Load(outputFile);
            Assert.Equal(keepDebugInfo ? "a.md" : null, document.DocumentNode.SelectSingleNode("p").GetAttributeValue("sourceFile", null));
        }
        finally
        {
            Environment.SetEnvironmentVariable(name, original);
        }
    }
}
