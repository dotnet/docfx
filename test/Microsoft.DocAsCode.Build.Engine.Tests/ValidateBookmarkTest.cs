// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    using Microsoft.DocAsCode.Build.Common;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;

    using Xunit;

    public class ValidateBookmarkTest
    {
        private static ValidateBookmark _validator = new ValidateBookmark();
        private LoggerListener _listener = new LoggerListener("validate_bookmark");

        [Fact]
        public void TestBasicFeature()
        {
            var outputFolder = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "validate_bookmark");
            Directory.CreateDirectory(outputFolder);
            Manifest manifest = new Manifest
            {
                SourceBasePath = outputFolder,
                Files = new List<ManifestItem>
                {
                    new ManifestItem { SourceRelativePath = "a.md", OutputFiles = new Dictionary<string, OutputFileInfo> { { ".html", new OutputFileInfo { RelativePath = "a.html" } } } },
                    new ManifestItem { SourceRelativePath = "b.md", OutputFiles = new Dictionary<string, OutputFileInfo> { { ".html", new OutputFileInfo { RelativePath = "b.html" } } } },
                }
            };

            File.WriteAllText(Path.Combine(outputFolder, "a.html"), @"<a href='http://bing.com#top'>Microsoft Bing</a> <p id='b1'>section</p><a href='#b1'/>");
            File.WriteAllText(Path.Combine(outputFolder, "b.html"), @"<a href='a.html#b1'>bookmark existed</a> <a href='a.html#b2'>bookmark nonexisted</a>");

            Logger.RegisterListener(_listener);
            using (new LoggerPhaseScope("validate_bookmark"))
            {
                _validator.Process(manifest, outputFolder);
            }
            var logs = _listener.Items;
            Console.WriteLine(string.Concat(logs.Select(l => l.Message)));
            Assert.Equal(1, logs.Count);
            Assert.Equal(
                @"Output file b.html which is built from src file b.md contains illegal link a.html#b2: the file a.html which is built from src a.md doesn't contain a bookmark named b2.",
                logs[0].Message);
            Logger.UnregisterListener(_listener);
        }

        private class LoggerListener : ILoggerListener
        {
            public string Phase { get; }

            public List<ILogItem> Items { get; } = new List<ILogItem>();

            public LoggerListener(string phase)
            {
                Phase = phase;
            }

            public void Dispose()
            {
            }

            public void Flush()
            {
            }

            public void WriteLine(ILogItem item)
            {
                if (item.Phase == Phase)
                {
                    Items.Add(item);
                }
            }
        }
    }
}
