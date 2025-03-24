// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using FluentAssertions;

namespace Docfx.Common.Tests;

[TestClass]
public class PathUtilityTest
{
    [TestMethod]
    [DynamicData(nameof(TestData.AdditionalTests), typeof(TestData))]
    public void TestMakeRelativePath(string basePath, string targetPath, string expected)
    {
        // Act
        var result = PathUtility.MakeRelativePath(basePath, targetPath);

        // Assert
        result.Should().Be(expected);
    }

    [TestMethod]
    [DynamicData(nameof(TestData.EscapedPaths), typeof(TestData))]
    public void TestMakeRelativePathWithEncodedPath(string inputPath)
    {
        // Arrange
        string basePath = "./";
        var expected = inputPath;

        // Act
        var result = PathUtility.MakeRelativePath(basePath, inputPath);

        // Assert
        result.Should().Be(expected);
    }

    private static class TestData
    {
        public static IEnumerable<(string, string, string)> AdditionalTests =>
        [
            ( "/a/b/d",          "/a/b/file.md",            "../file.md"),            // root relative path
            ( "~/a/b/d",         "~/a/b/file.md",           "../file.md"),            // user home directory relative path
            ( "./",              @"\\UNCPath\file.md",       "//UNCPath/file.md"),     // UNC path
            ( "./",              "file:///C:/temp/test.md", "file:/C:/temp/test.md"), // `file:` Uri path
            ( "file:///C:/temp", "file:///C:/temp/test.md", "test.md"),               // `file:` Uri relative path
            ( "/temp/dir",       "/temp/dir/subdir/",       "subdir/"),               // If target path endsWith directory separator char. resolved path should contain directory separator.
        ];

        public static IEnumerable<string> EscapedPaths =>
        [
            "EscapedHypen(%2D).md",                      // Contains escaped hypen char
            "EscapedSpace(%20)_with_NonAsciiChar(α).md", // Contains escaped space char and non-unicode char
        ];
    }
}
