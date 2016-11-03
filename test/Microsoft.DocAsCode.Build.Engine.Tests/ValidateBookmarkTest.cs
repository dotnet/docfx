﻿// Copyright (c) Microsoft. All rights reserved.
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
        private LoggerListener _listener = new LoggerListener("validate_bookmark.ValidateBookmark");

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
            File.WriteAllText(Path.Combine(outputFolder, "b.html"), @"<a href='a.html#b1' sourceFile='b.md' sourceStartLineNumber='1'>bookmark existed</a><a href='a.html#b2' sourceFile='b.md' sourceStartLineNumber='1'>link with source info</a> <a href='a.html#b3' sourceFile='token.md' sourceStartLineNumber='1'>link in token file</a><a href='a.html#b4'>link without source info</a>");

            Logger.RegisterListener(_listener);
            using (new LoggerPhaseScope("validate_bookmark"))
            {
                new HtmlPostProcessor
                {
                    Handlers = { new ValidateBookmark() }
                }.Process(manifest, outputFolder);
            }
            Logger.UnregisterListener(_listener);
            var logs = _listener.Items;
            Console.WriteLine(string.Concat(logs.Select(l => l.Message)));
            Assert.Equal(3, logs.Count);
            var expected = new[]
            {
                @"b.md contains illegal link: a.md#b2. Link text is `link with source info`. The file a.md doesn't contain a bookmark named b2.",
                @"token.md contains illegal link: a.md#b3. Link text is `link in token file`. The file a.md doesn't contain a bookmark named b3.",
                @"b.html contains illegal link: a.html#b4. Link text is `link without source info`. The file a.html doesn't contain a bookmark named b4, please check the src file b.md and src linkedTo file a.md or the template you applied."
            };
            var actual = logs.Select(l => l.Message).ToList();
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
