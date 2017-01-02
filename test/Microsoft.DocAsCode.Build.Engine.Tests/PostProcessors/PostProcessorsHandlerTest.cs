// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine.Tests
{
    using System.Collections.Generic;
    using System.IO;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Tests.Common;

    using Xunit;

    [Trait("Owner", "jehuan")]
    [Collection("docfx STA")]
    public class PostProcessorsHandlerTest : TestBase
    {
        private readonly PostProcessorsHandler _postProcessorsHandler = new PostProcessorsHandler();
        private readonly List<PostProcessor> _postProcessors = new List<PostProcessor>
        {
            new PostProcessor
            {
                ContractName = typeof (AppendStringPostProcessor).Name,
                Processor = new AppendStringPostProcessor()
            }
        };

        [Fact]
        public void TestBasicScenario()
        {
            var manifest = JsonUtility.Deserialize<Manifest>("PostProcessors/Data/manifest_basic.json");
            var outputFolder = GetRandomFolder();
            CreateFile("index.html", "content", outputFolder);
            CreateFile("index.mta.json", "metadata", outputFolder);
            _postProcessorsHandler.Handle(_postProcessors, manifest, outputFolder);
            Assert.Equal($"content{AppendStringPostProcessor.AppendString}", File.ReadAllText(Path.Combine(outputFolder, "index.html")));
            Assert.Equal("metadata", File.ReadAllText(Path.Combine(outputFolder, "index.mta.json")));
        }
    }
}
