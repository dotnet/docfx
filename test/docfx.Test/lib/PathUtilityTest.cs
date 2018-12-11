// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Xunit;

namespace Microsoft.Docs.Build
{
    public static class PathUtilityTest
    {
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
        // Disable temporarily due to behavior differences between .NET core versions
        // [InlineData("a/b/c", "a", "../../a")]
        [InlineData("a/b", "a/b", "b")]
        public static void GetRelativePathToFile(string relativeTo, string path, string expected)
            => Assert.Equal(expected, PathUtility.GetRelativePathToFile(relativeTo, path).Replace("\\", "/"));

        [Theory]
        [InlineData("a", "a", true, true, "a")]
        [InlineData("a/b", "a/b", true, true, "a/b")]
        [InlineData("a/b", "a/", true, false, "b")]
        [InlineData("a", "./", true, false, "a")]
        [InlineData("a/b", "./", true, false, "a/b")]
        [InlineData("a/b", "c/", false, false, null)]
        [InlineData("a/b", "c", false, false, null)]
        [InlineData("a", "a/b", false, false, null)]
        [InlineData("a/b/c", "a", true, false, "b/c")]
        [InlineData("ab/c", "a", false, false, null)]
        public static void PathMatch(string file, string matcher, bool expectedMatch, bool expectedIsFileMatch, string expectedRemainingPath)
        {
            var (match, isFileMatch, remaniningPath) = PathUtility.Match(file, matcher);
            Assert.Equal(expectedMatch, match);
            Assert.Equal(expectedIsFileMatch, isFileMatch);
            Assert.Equal(expectedRemainingPath, remaniningPath);
        }

        [Theory]
        [InlineData("file1.md", false)]
        [InlineData("path-test/2/file1.md", true)]
        public static void CreateDirectoryFromFilePath(string filePath, bool isDirectoryCreated)
        {
            if (Directory.Exists(filePath))
                Directory.Delete(filePath);
            if (File.Exists(filePath))
                File.Delete(filePath);

            PathUtility.CreateDirectoryFromFilePath(filePath);
            Assert.Equal(Directory.Exists(Path.GetDirectoryName(filePath)), isDirectoryCreated);
        }

        [Theory]
        [InlineData("", "d41d8cd9")]
        [InlineData("https://github.com/dotnet/docfx", "github.com+dotnet+docfx+fb64b9d2")]
        [InlineData("https://github.com/1/2/3/4/5/6/7/8/9/10/11/12/13/14", "github.com+1+2+3+11+12+13+14+b5f1b5a8")]
        [InlineData("https://github.com/crazy-crazy-crazy-crazy-long-repo.zh-cn", "github.com+crazy-cr..po.zh-cn+791b3f68")]
        [InlineData("https://a.com?b=c#d", "a.com+b=c+d+2183540f")]
        public static void UrlToFolderName(string url, string folderName)
        {
            Assert.Equal(folderName, PathUtility.UrlToShortName(url));
        }

        [Fact]
        public static void PathDoesNotThrowForInvalidChar()
        {
            var str = new string(Path.GetInvalidFileNameChars().Concat(Path.GetInvalidPathChars()).ToArray());
            Path.GetFileName(str);
            Path.GetDirectoryName(str);
        }

        [Fact]
        public static void IsCaseSensitive()
        {
            Assert.Equal(RuntimeInformation.IsOSPlatform(OSPlatform.Linux), PathUtility.IsCaseSensitive);
        }
    }
}
