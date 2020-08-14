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

            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(filePath)));
            Assert.Equal(Directory.Exists(Path.GetDirectoryName(filePath)), isDirectoryCreated);
        }

        [Theory]
        [InlineData("", "d41d8cd9")]
        [InlineData("https://github.com/dotnet/docfx", "github.com+dotnet+docfx+fb64b9d2")]
        [InlineData("https://github.com/1/2/3/4/5/6/7/8/9/10/11/12/13/14", "github.com+1+2+3+11+12+13+14+b5f1b5a8")]
        [InlineData("https://github.com/crazy-crazy-crazy-crazy-long-repo.zh-cn", "github.com+crazy-cr..po.zh-cn+791b3f68")]
        [InlineData("https://a.com?b=c#d", "a.com+b=c+d+2183540f")]
        [InlineData("https://ab-c.blob.core.windows.net/a/b/c/d?sv=d&sr=e&sig=f&st=2019-05-07&se=2019-05-08&sp=r", "ab-c.blo..dows.net+a+b+c+d+9e69c8e7")]
        public static void UrlToFolderName(string url, string folderName)
        {
            var result = PathUtility.UrlToShortName(url);
            Assert.Equal(folderName, result);
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
