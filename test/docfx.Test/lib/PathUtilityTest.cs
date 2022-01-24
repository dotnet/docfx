// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using Xunit;

namespace Microsoft.Docs.Build;

public static class PathUtilityTest
{
    [Theory]
    [InlineData(".", "")]
    [InlineData("./", "")]
    [InlineData("../", "../")]
    [InlineData("..", "../")]
    [InlineData("a", "a")]
    [InlineData("a.b", "a.b")]
    [InlineData("\\a", "/a")]
    [InlineData("a\\", "a/")]
    [InlineData("a/b/c/../d", "a/b/d")]
    [InlineData("a/b/c/../d/", "a/b/d/")]
    [InlineData("a/../.b", ".b")]
    public static void Normalize(string path, string expected)
        => Assert.Equal(expected, PathUtility.Normalize(path));

    [Theory]
    [InlineData(".", "")]
    [InlineData("a", "a")]
    [InlineData("a.b", "a.b")]
    [InlineData("\\a", "/a")]
    [InlineData("a\\b./c/d../e", "a/b./c/d../e")]
    [InlineData("a\\b\\./c/d/../e", "a/b/c/e")]
    [InlineData("/a\\b\\./c/d/../e", "/a/b/c/e")]
    [InlineData("a\\\\b\\./c/d/../e", "a/b/c/e")]
    [InlineData("a\\b\\.///c/d/../e", "a/b/c/e")]
    [InlineData("a.b//c", "a.b/c")]
    [InlineData("ab//c", "ab/c")]
    [InlineData("a/", "a")]
    public static void NormalizeFile(string path, string expected)
        => Assert.Equal(expected, PathUtility.NormalizeFile(path));

    [Theory]
    [InlineData("", "./")]
    [InlineData(".", "./")]
    [InlineData(".\\", "./")]
    [InlineData("a", "a/")]
    [InlineData("\\a", "/a/")]
    [InlineData("a\\b\\./c/d/../e", "a/b/c/e/")]
    [InlineData("/a\\b\\./c/d/../e", "/a/b/c/e/")]
    [InlineData("a/", "a/")]
    public static void NormalizeFolder(string path, string expected)
        => Assert.Equal(expected, PathUtility.NormalizeFolder(path));

    [Theory]
    [InlineData("a", "b", "b")]
    [InlineData("a/b", "a/c", "c")]
    [InlineData("a/b", "c/d", "../c/d")]
    [InlineData("a/b", "a", "../a")]
    [InlineData("a/b/c", "a", "..")]
    [InlineData("a/b", "a/b", "b")]
    public static void GetRelativePathToFile(string relativeTo, string path, string expected)
        => Assert.Equal(expected, PathUtility.GetRelativePathToFile(relativeTo, path).Replace("\\", "/"));

    [Theory]
    [InlineData("", "./")]
    [InlineData("a", "./")]
    [InlineData("a/b", "../")]
    [InlineData("a/b/c", "../../")]
    public static void GetRelativePathToRoot(string path, string expected)
        => Assert.Equal(expected, PathUtility.GetRelativePathToRoot(path));

    [Theory]
    [InlineData("", "", true, "")]
    [InlineData("a", "a", true, "")]
    [InlineData("a/b", "a/b", true, "")]
    [InlineData("a/b", "a/", true, "b")]
    [InlineData("a", "./", true, "a")]
    [InlineData("a/b", "./", true, "a/b")]
    [InlineData("a/b", "c/", false, "")]
    [InlineData("a/b", "c", false, "")]
    [InlineData("a", "a/b", false, "")]
    [InlineData("a/b/c", "a", true, "b/c")]
    [InlineData("ab/c", "a", false, "")]
    [InlineData("a", "/", false, "")]
    public static void PathMatch(string file, string matcher, bool expectedMatch, string expectedRemainingPath)
    {
        var matches = new PathString(file).StartsWithPath(new PathString(matcher), out var remaining);
        Assert.Equal(expectedMatch, matches);
        Assert.Equal(new PathString(expectedRemainingPath), remaining);
    }

    [Theory]
    [InlineData("", "", true)]
    [InlineData("", ".", true)]
    [InlineData("a", "a", true)]
    [InlineData("a", "a\\", true)]
    [InlineData("a/", "a\\", true)]
    [InlineData("a/", "a", true)]
    [InlineData("a", "b", false)]
    [InlineData("/a", "a", false)]
    [InlineData("a", "", false)]
    [InlineData("a/b", "a/", false)]
    public static void FolderEquals(string a, string b, bool equals)
    {
        Assert.Equal(equals, new PathString(a).FolderEquals(new PathString(b)));
    }

    [Theory]
    [InlineData("", "", ".")]
    [InlineData("a", "", "a")]
    [InlineData("", "a", "a")]
    [InlineData("a", "b", "a/b")]
    [InlineData("a/", "b", "a/b")]
    [InlineData("a/", "/b", "/b")]
    [InlineData("a/", "./b", "a/b")]
    [InlineData("a/", "../b", "b")]
    [InlineData("a", "../b", "b")]
    [InlineData("a", "../b/", "b/")]
    [InlineData("a", "../../../b", "../../b")]
    [InlineData("a", "c:/b", "c:/b")]
    public static void PathConcatTest(string a, string b, string match)
    {
        Assert.Equal(match, new PathString(a).Concat(new PathString(b)));
    }

    [Theory]
    [InlineData("file1.md", false)]
    [InlineData("path-test/2/file1.md", true)]
    public static void CreateDirectoryFromFilePath(string filePath, bool isDirectoryCreated)
    {
        if (Directory.Exists(filePath))
        {
            Directory.Delete(filePath);
        }

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(filePath)) ?? ".");
        Assert.Equal(Directory.Exists(Path.GetDirectoryName(filePath)), isDirectoryCreated);
    }

    [Fact]
    public static void PathDoesNotThrowForInvalidChar()
    {
        var str = new string(Path.GetInvalidFileNameChars().Concat(Path.GetInvalidPathChars()).ToArray());
        Path.GetFileName(str);
        Path.GetDirectoryName(str);
        Path.IsPathRooted(str);
    }

    [Fact]
    public static void IsCaseSensitive()
    {
        Assert.Equal(RuntimeInformation.IsOSPlatform(OSPlatform.Linux), PathUtility.IsCaseSensitive);
    }
}
