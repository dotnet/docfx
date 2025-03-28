// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Tests.Common;

namespace Docfx.Glob.Tests;

[TestClass]
public class GlobFileTest : TestBase
{
    private readonly string _workingDirectory;

    public GlobFileTest()
    {
        _workingDirectory = GetRandomFolder();
    }

    [TestMethod]
    public void TestGlobGetFilesShouldAbleToGetFiles()
    {
        // - Root/
        //   |- A.cs
        //   |- B.cs
        //   |- C/
        //   |  |- D.cs
        //   |- E/
        //   |  |- F.cs
        //   |  |- G.csproj
        //   |  |- H/
        //   |  |   |- I.jpg
        //   |- J/
        //   |  |- K.md
        //   |- M/
        //      |- N.md
        //      |- L/
        //         |- O.md
        // - .Hidden/
        var files = new string[]
        {
            "Root/A.cs",
            "Root/B.cs",
            "Root/C/D.cs",
            "Root/E/F.cs",
            "Root/E/G.csproj",
            "Root/E/H/I.jpg",
            "Root/J/K.md",
            "Root/M/N.md",
            "Root/M/L/O.md",
            ".Hidden/",
        };
        CreateFilesOrFolders(_workingDirectory, files);
        var result = FileGlob.GetFiles(
            _workingDirectory,
            ["**.md"],
            null).ToArray();
        Assert.AreEqual(3, result.Length);
        result = FileGlob.GetFiles(
            _workingDirectory,
            null,
            ["**.md"]).ToArray();
        Assert.IsEmpty(result);
        result = FileGlob.GetFiles(
            _workingDirectory,
            ["**"],
            ["**.md"]).ToArray();
        Assert.AreEqual(6, result.Length);
        result = FileGlob.GetFiles(
             _workingDirectory,
             ["**.md"],
             ["**{J,L}/**"]).ToArray();
        Assert.ContainsSingle(result);
        result = FileGlob.GetFiles(
             _workingDirectory,
             ["**.md", "**.csproj"],
             ["**J/**", "**/M/**"]).ToArray();
        Assert.ContainsSingle(result);
        result = FileGlob.GetFiles(
             _workingDirectory + "/Root",
             ["[EJ]/*.{md,cs,csproj}"],
             ["**.cs"]).ToArray();
        Assert.AreEqual(2, result.Length);
    }

    private static void CreateFilesOrFolders(string cwd, params string[] items)
    {
        if (string.IsNullOrEmpty(cwd)) cwd = ".";
        foreach (var i in items)
        {
            var item = cwd + "/" + i;
            if (item.EndsWith('/'))
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
