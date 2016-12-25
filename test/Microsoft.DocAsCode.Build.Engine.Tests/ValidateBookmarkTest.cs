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
    using Microsoft.DocAsCode.Tests.Common;

    using Xunit;

    [Collection("docfx STA")]
    public class ValidateBookmarkTest : TestBase
    {
        private readonly string _outputFolder;
        private LoggerListener _listener = new LoggerListener("validate_bookmark.ValidateBookmark");

        public ValidateBookmarkTest()
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
                Files = new List<ManifestItem>
                {
                    new ManifestItem { SourceRelativePath = "a.md", OutputFiles = new Dictionary<string, OutputFileInfo> { { ".html", new OutputFileInfo { RelativePath = "a.html" } } } },
                    new ManifestItem { SourceRelativePath = "b.md", OutputFiles = new Dictionary<string, OutputFileInfo> { { ".html", new OutputFileInfo { RelativePath = "b.html" } } } },
                    new ManifestItem { SourceRelativePath = "c.md", OutputFiles = new Dictionary<string, OutputFileInfo> { { ".html", new OutputFileInfo { RelativePath = "c.html" } } } },
                }
            };

            File.WriteAllText(Path.Combine(_outputFolder, "a.html"), @"<a href='http://bing.com#top'>Microsoft Bing</a> <p id='b1'>section</p><a href='#b1'/>");
            File.WriteAllText(Path.Combine(_outputFolder, "b.html"), @"<a href='a.html#b1' sourceFile='b.md' sourceStartLineNumber='1'>bookmark existed</a><a href='a.html#b2' data-raw-source='[link with source info](a.md#b2)' sourceFile='b.md' sourceStartLineNumber='1'>link with source info</a> <a href='a.html#b3' data-raw-source='[link in token file](a.md#b3)' sourceFile='token.md' sourceStartLineNumber='1'>link in token file</a><a href='a.html#b4'>link without source info</a>");
            File.WriteAllText(Path.Combine(_outputFolder, "c.html"), @"<a href='illegal_path_%3Cillegal character%3E.html#b1'>Test illegal link path</a>");

            Logger.RegisterListener(_listener);
            using (new LoggerPhaseScope("validate_bookmark"))
            {
                new HtmlPostProcessor
                {
                    Handlers = { new ValidateBookmark() }
                }.Process(manifest, _outputFolder);
            }
            Logger.UnregisterListener(_listener);
            var logs = _listener.Items;
            Console.WriteLine(string.Concat(logs.Select(l => Tuple.Create(l.Message, l.File))));
            Assert.Equal(3, logs.Count);
            var expected = new[]
            {
                Tuple.Create(@"Illegal link: `[link with source info](a.md#b2)` -- missing bookmark. The file a.md doesn't contain a bookmark named b2.", "b.md"),
                Tuple.Create(@"Illegal link: `[link in token file](a.md#b3)` -- missing bookmark. The file a.md doesn't contain a bookmark named b3.", "token.md"),
                Tuple.Create(@"Illegal link: `<a href=""a.md#b4"">link without source info</a>` -- missing bookmark. The file a.md doesn't contain a bookmark named b4.", "b.md"),
            };
            var actual = logs.Select(l => Tuple.Create(l.Message, l.File)).ToList();
            Assert.True(!expected.Except(actual).Any() && expected.Length == actual.Count);
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
