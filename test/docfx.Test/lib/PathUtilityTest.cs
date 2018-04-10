// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Xunit;

namespace Microsoft.Docs.Test
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
        public static void NormalizeFolder(string path, string expected)
            => Assert.Equal(expected, PathUtility.NormalizeFolder(path));

        [Theory]
        [InlineData("a", "b", "b")]
        [InlineData("a/b", "a/c", "c")]
        [InlineData("a/b", "c/d", "../c/d")]
        public static void GetRelativePathToFile(string relativeTo, string path, string expected)
            => Assert.Equal(expected, PathUtility.GetRelativePathToFile(relativeTo, path).Replace("\\", "/", StringComparison.Ordinal));
    }
}
