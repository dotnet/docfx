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
        [InlineData("", false, true)]
        [InlineData(null, false, true)]
        [InlineData("path-test/1/dir", false, true)]
        [InlineData("path-test/2/dir", true, false)]
        public static void CreateDirectoryIfNotEmpty(string path, bool createFileWithSameName, bool expected)
        {
            if (Directory.Exists(path))
                Directory.Delete(path);
            if (File.Exists(path))
                File.Delete(path);
            if (createFileWithSameName)
            {
                PathUtility.CreateDirectoryIfNotEmpty(Path.GetDirectoryName(path));
                File.Create(path);
            }
            Assert.Equal(PathUtility.CreateDirectoryIfNotEmpty(path), expected);
            if (!string.IsNullOrEmpty(path))
                Assert.Equal(Directory.Exists(path), expected);
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
