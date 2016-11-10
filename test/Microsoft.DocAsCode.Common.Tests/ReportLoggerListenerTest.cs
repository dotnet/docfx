// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common.Tests
{
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Tests.Common;

    using Xunit;

    [Collection("docfx STA")]
    public class ReportLoggerListenerTest : TestBase
    {
        private string _workingDirectory;
        private string _repoRoot;

        public ReportLoggerListenerTest()
        {
            _workingDirectory = GetRandomFolder();
            _repoRoot = Path.GetFullPath(GetRandomFolder());
        }

        [Fact]
        public void TestFilePath()
        {
            // - RepoRoot/
            //   |- A.ps1
            //   |- B/
            //      |- Root
            //         |- C.cs
            //         |- docfx.json
            //         |- D/
            //            |- E.md
            var logFilePath = Path.Combine(_workingDirectory, "report.txt");
            var files = new string[]
            {
                "A.ps1",
                "B/Root/C.cs",
                "B/Root/docfx.json",
                "B/Root/D/E.md",
            };
            CreateFilesOrFolders(_repoRoot, files);
            var listener = new ReportLogListener(logFilePath, _repoRoot, Path.Combine(_repoRoot, "B/Root/"));
            Logger.RegisterListener(listener);
            using (new LoggerPhaseScope("ReportLoggerListenerTest"))
            {
                Logger.LogInfo("Test file path1", file: "~/C.cs");
                Logger.LogInfo("Test file path2", file: "D/E.md");
            }
            Logger.UnregisterListener(listener);
            var lines = File.ReadAllLines(logFilePath);
            var reportItems = (from line in lines
                               select line.FromJsonString<ReportLogListener.ReportItem>()).ToList();
            var item1 = reportItems.SingleOrDefault(r => r.Message == "Test file path1");
            Assert.NotNull(item1);
            Assert.Equal(item1.File, "B/Root/C.cs");
            var item2 = reportItems.SingleOrDefault(r => r.Message == "Test file path2");
            Assert.NotNull(item2);
            Assert.Equal(item2.File, "B/Root/D/E.md");
        }

        private static void CreateFilesOrFolders(string cwd, params string[] items)
        {
            if (string.IsNullOrEmpty(cwd)) cwd = ".";
            foreach (var i in items)
            {
                var item = cwd + "/" + i;
                if (item.EndsWith("/"))
                {
                    Directory.CreateDirectory(item);
                }
                else
                {
                    var dir = Path.GetDirectoryName(item);
                    if (dir != string.Empty) Directory.CreateDirectory(dir);
                    File.WriteAllText(item, string.Empty);
                }
            }
        }
    }
}
