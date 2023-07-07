// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Tests.Common;

using Xunit;

namespace Docfx.Common.Tests;

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
        Assert.Equal("B/Root/C.cs", item1.File);
        var item2 = reportItems.SingleOrDefault(r => r.Message == "Test file path2");
        Assert.NotNull(item2);
        Assert.Equal("B/Root/D/E.md", item2.File);
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
