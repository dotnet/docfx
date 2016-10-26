// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine.Tests
{
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;

    using Microsoft.DocAsCode.Build.Common;
    using Microsoft.DocAsCode.Plugins;

    using Xunit;

    public class RemoveDebugInfoTest
    {
        [Fact]
        public void TestBasicFeature()
        {
            var outputFolder = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "RemoveDebugInfo");
            Directory.CreateDirectory(outputFolder);
            Manifest manifest = new Manifest
            {
                SourceBasePath = outputFolder,
                Files = new List<ManifestItem>
                {
                    new ManifestItem { SourceRelativePath = "a.md", OutputFiles = new Dictionary<string, OutputFileInfo> { { ".html", new OutputFileInfo { RelativePath = "a.html" } } } },
                }
            };

            File.WriteAllText(Path.Combine(outputFolder, "a.html"), @"<p id='b1' sourceFile='a.md' sourceStartLineNumber='1' sourceEndLineNumber='2'>section<a sourcefile=""a.md"" href='http://bing.com#top'>Microsoft Bing</a></p>");

            new HtmlPostProcessor
            {
                Handlers = { new RemoveDebugInfo() }
            }.Process(manifest, outputFolder);

            var actual = File.ReadAllText(Path.Combine(outputFolder, "a.html"));
            Assert.Equal("<p id='b1'>section<a href='http://bing.com#top'>Microsoft Bing</a></p>", actual);
            Directory.Delete(outputFolder, true);
        }
    }
}
