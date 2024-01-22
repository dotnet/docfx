// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Tests.Common;
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;

namespace Docfx.Glob.Tests;

public class GlobFileTest : TestBase
{
    private readonly string _workingDirectory;

    public GlobFileTest()
    {
        _workingDirectory = GetRandomFolder();
    }

    [Fact]
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
            new string[] { "**.md" },
            null).ToArray();
        Assert.Equal(3, result.Length);
        result = FileGlob.GetFiles(
            _workingDirectory,
            null,
            new string[] { "**.md" }).ToArray();
        Assert.Empty(result);
        result = FileGlob.GetFiles(
            _workingDirectory,
            new string[] { "**" },
            new string[] { "**.md" }).ToArray();
        Assert.Equal(6, result.Length);
        result = FileGlob.GetFiles(
             _workingDirectory,
             new string[] { "**.md" },
             new string[] { "**{J,L}/**" }).ToArray();
        Assert.Single(result);
        result = FileGlob.GetFiles(
             _workingDirectory,
             new string[] { "**.md", "**.csproj" },
             new string[] { "**J/**", "**/M/**" }).ToArray();
        Assert.Single(result);
        result = FileGlob.GetFiles(
             _workingDirectory + "/Root",
             new string[] { "[EJ]/*.{md,cs,csproj}" },
             new string[] { "**.cs" }).ToArray();
        Assert.Equal(2, result.Length);
    }

    [Fact]
    public void TestGlobGetFilesWithDotStartedDirectoryFiles()
    {
        // - .Hidden/
        //   |- A.cs
        //   |- B.cs
        //   |- .Nested/
        //   |    |- C.cs
        //   |- D/
        //   |  |- E.cs
        // - .NotHidden/
        //   |- F.cs
        //   |- G.cs
        //   |- .HiddenFile.cs
        var files = new string[]
        {
            ".Hidden/A.cs",
            ".Hidden/B.cs",
            ".Hidden/.Nested/C.cs",
            ".Hidden/D/E.cs",
            "NotHidden/F.cs",
            "NotHidden/G.cs",
            "NotHidden/.HiddenFile.cs",
        };
        CreateFilesOrFolders(_workingDirectory, files);

        var totalFileCount = files.Length;

        using (var scope = new AssertionScope())
        {
            // Test wildcard pattern with AllowDotMatch option.
            var result = FileGlob.GetFiles(
                _workingDirectory,
                patterns: new[] { "**.cs" },
                excludePatterns: null,
                options: GlobMatcher.DefaultOptions | GlobMatcherOptions.AllowDotMatch
            ).ToArray();
            result.Should().HaveCount(totalFileCount); // All files are included when using AllowDotMatch option.

            // Test wildcard pattern with default options.
            result = FileGlob.GetFiles(
                _workingDirectory,
                patterns: new[] { "**.cs" },
                excludePatterns: null
                ).ToArray();
            result.Should().HaveCount(2); // Dot started directories/files are excluded when using DefaultOptions.

            // Test explicitly specified wildcard pattern with default options.
            result = FileGlob.GetFiles(
                _workingDirectory,
                patterns: new[] { "**.cs", ".Hidden/**.cs" },
                excludePatterns: null
            ).ToArray();
            result.Should().HaveCount(5); // Explicitly specified `.Hidden/**.cs` files are included. But `.Hidden/.Nested/**` files are excluded.

            // Test `excludePatterns` with default option.
            result = FileGlob.GetFiles(
                _workingDirectory,
                patterns: new[] { "**.cs", ".Hidden/**.cs" },
                excludePatterns: new[] { ".Hidden/D/E.cs" }
            ).ToArray();
            result.Should().HaveCount(4); // `.Hidden/D/E.cs` file is excluded.

            // Test `excludePatterns` with AllowDotMatch option.
            result = FileGlob.GetFiles(
                _workingDirectory,
                patterns: new string[] { "**.cs", },
                excludePatterns: new string[] { "**/.Nested/C.cs" },
                options: GlobMatcher.DefaultOptions | GlobMatcherOptions.AllowDotMatch
            ).ToArray();
            result.Should().HaveCount(totalFileCount - 1); // `.Hidden/.Nested/C.cs` file is excluded.
        }
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
