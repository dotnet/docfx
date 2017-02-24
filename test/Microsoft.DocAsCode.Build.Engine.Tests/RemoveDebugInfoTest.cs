// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine.Tests
{
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;

    using Microsoft.DocAsCode.Build.Common;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Tests.Common;

    using Xunit;

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
            Manifest manifest = new Manifest
            {
                SourceBasePath = _outputFolder,
                Files =
                {
                    new ManifestItem { SourceRelativePath = "a.md", OutputFiles = { { ".html", new OutputFileInfo { RelativePath = "a.html" } } } },
                }
            };

            File.WriteAllText(Path.Combine(_outputFolder, "a.html"), @"<p id='b1' sourceFile='a.md' sourceStartLineNumber='1' sourceEndLineNumber='2'>section<a sourcefile=""a.md"" href='http://bing.com#top'>Microsoft Bing</a></p>");

            new HtmlPostProcessor
            {
                Handlers = { new RemoveDebugInfo() }
            }.Process(manifest, _outputFolder);

            var actual = File.ReadAllText(Path.Combine(_outputFolder, "a.html"));
            Assert.Equal("<p id='b1'>section<a href='http://bing.com#top'>Microsoft Bing</a></p>", actual);
        }
    }
}
