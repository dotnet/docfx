// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Plugins;
using Docfx.Tests.Common;

namespace Docfx.Build.Engine.Tests;

[DoNotParallelize]
[TestClass]
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

    [TestMethod]
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
        }.Process(manifest, _outputFolder);

        var actual = File.ReadAllText(Path.Combine(_outputFolder, "a.html"));
        Assert.AreEqual("<p id='b1'>section<a href='http://bing.com#top'>Microsoft Bing</a></p>", actual);
    }
}
