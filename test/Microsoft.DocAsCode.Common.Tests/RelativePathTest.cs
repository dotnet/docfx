// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common.Tests
{
    using System;

    using Xunit;

    using Microsoft.DocAsCode.Common;

    [Trait("Owner", "vwxyzh")]
    [Trait("Related", "RelativePath")]
    public class RelativePathTest
    {
        [Fact]
        public void TestRelativePathWithBasicScenarios()
        {
            {
                var s = "../a/b.txt";
                var r = (RelativePath)s;
                Assert.NotNull(r);
                Assert.Equal(1, r.ParentDirectoryCount);
                Assert.Equal(s, r.ToString());
                Assert.Equal(s, r);
            }
            {
                var s = "a.txt";
                var r = (RelativePath)s;
                Assert.NotNull(r);
                Assert.Equal(0, r.ParentDirectoryCount);
                Assert.Equal(s, r.ToString());
                Assert.Equal(s, r);
            }
            {
                var s = "a.dir/";
                var r = (RelativePath)s;
                Assert.NotNull(r);
                Assert.Equal(0, r.ParentDirectoryCount);
                Assert.Equal(s, r.ToString());
                Assert.Equal(s, r);
            }
            {
                var s = @"a\b\.\d\..\\c.dir\";
                var r = (RelativePath)s;
                Assert.NotNull(r);
                Assert.Equal(0, r.ParentDirectoryCount);
                Assert.Equal("a/b/c.dir/", r.ToString());
                Assert.Equal("a/b/c.dir/", r);
            }
            {
                var s = "";
                var r = (RelativePath)s;
                Assert.NotNull(r);
                Assert.Equal(0, r.ParentDirectoryCount);
                Assert.Equal(s, r.ToString());
                Assert.Equal(s, r);
                Assert.Same(RelativePath.Empty, r);
            }
            {
                var s = ".";
                var r = (RelativePath)s;
                Assert.NotNull(r);
                Assert.Equal(0, r.ParentDirectoryCount);
                Assert.Equal(string.Empty, r.ToString());
                Assert.Equal(string.Empty, r);
                Assert.Same(RelativePath.Empty, r);
            }
            {
                var s = "a/../";
                var r = (RelativePath)s;
                Assert.NotNull(r);
                Assert.Equal(0, r.ParentDirectoryCount);
                Assert.Equal(string.Empty, r.ToString());
                Assert.Equal(string.Empty, r);
                Assert.Same(RelativePath.Empty, r);
            }
        }

        [Theory]
        [InlineData("d/e.txt", "a/b/c/", "a/b/c/d/e.txt")]
        [InlineData("../d/e.txt", "a/b/c/", "a/b/d/e.txt")]
        [InlineData("d/e.txt", "a/b/c.txt", "a/b/d/e.txt")]
        [InlineData("../e.txt", "a/b/c.txt", "a/e.txt")]
        [InlineData("../e.txt", "../c.txt", "../../e.txt")]
        [InlineData("../a.txt", "", "../a.txt")]
        [InlineData("", "../a/b.txt", "../a/")]
        [InlineData("../", "a/", "")]
        [InlineData("", "", "")]
        [InlineData("~/a.txt", "a/b/c/", "~/a.txt")]
        [InlineData("d.txt", "~/a/b/c/", "~/a/b/c/d.txt")]
        public void TestRelativePathBasedOn(string thisPath, string basedOnPath, string expected)
        {
            var actual = ((RelativePath)thisPath).BasedOn((RelativePath)basedOnPath);
            Assert.NotNull(actual);
            Assert.Equal(expected, actual.ToString());
        }

        [Theory]
        [InlineData("a/b/c.txt", "d/e.txt", "../a/b/c.txt")]
        [InlineData("a/b/c.txt", "a/d.txt", "b/c.txt")]
        [InlineData("../../a.txt", "../b.txt", "../a.txt")]
        [InlineData("../../a.txt", "../b/c.txt", "../../a.txt")]
        [InlineData("a.txt", "../b.txt", null)]
        [InlineData("a/b.txt", "", "a/b.txt")]
        [InlineData("", "a/b.txt", "../")]
        [InlineData("a/", "a/", "")]
        [InlineData("", "", "")]
        [InlineData("~/a/b.txt", "~/a/c.txt", "b.txt")]
        [InlineData("~/a/b.txt", "a/c.txt", "~/a/b.txt")]
        public void TestRelativePathMakeRelativeTo(string thisPath, string relativeToPath, string expected)
        {
            try
            {
                var actual = ((RelativePath)thisPath).MakeRelativeTo((RelativePath)relativeToPath);
                Assert.NotNull(actual);
                Assert.Equal(expected, actual.ToString());
            }
            catch (NotSupportedException)
            {
                if (expected != null)
                {
                    throw;
                }
            }
        }

        [Theory]
        [InlineData("a/b/c", "a/b/")]
        [InlineData("~/a/b/c", "~/a/b/")]
        [InlineData("~/../a/b/c", "~/../a/b/")]
        [InlineData("~/../a/", "~/../a/")]
        [InlineData("~/../", "~/../")]
        [InlineData("~/a/../b", "~/")]
        public void TestRelativePathGetDirectoryPath(string file, string expected)
        {
            var relativePath = (RelativePath)file;
            var result = relativePath.GetDirectoryPath();
            Assert.Equal(expected, result.ToString());
        }

        [Fact]
        public void TestRelativePathGetDirectoryPathWithInvalidParentDirectoryShouldFail()
        {
            var relativePath = (RelativePath)"~/..";
            Assert.Throws<InvalidOperationException>(() => relativePath.GetDirectoryPath());
        }

        [Fact]
        public void TestRelativePathChangeFileNameWithInvalidFileNameShouldFail()
        {
            var relativePath = (RelativePath)"~/a/b/c";
            Assert.Throws<ArgumentException>(() => relativePath.ChangeFileName("d/"));
            Assert.Throws<ArgumentException>(() => relativePath.ChangeFileName(".."));
            Assert.Throws<ArgumentException>(() => relativePath.ChangeFileName("../d/"));
        }

        [Theory]
        [InlineData("a/b/c", "d", "a/b/d")]
        [InlineData("~/a/b/c", "d", "~/a/b/d")]
        [InlineData("../a/b/c", "d", "../a/b/d")]
        public void TestRelativePathChangeFileName(string file, string changedFileName, string expected)
        {
            var relativePath = (RelativePath)file;
            var result = relativePath.ChangeFileName(changedFileName);
            Assert.Equal(expected, result.ToString());
        }

        [Fact]
        public void TestRelativePathOperatorAdd()
        {
            // a/b/c/ + d/e.txt = a/b/c/d/e.txt
            {
                var r1 = (RelativePath)"a/b/c/";
                var r2 = (RelativePath)"d/e.txt";
                var r3 = r1 + r2;
                Assert.NotNull(r3);
                Assert.Equal(0, r3.ParentDirectoryCount);
                Assert.Equal("a/b/c/d/e.txt", r3.ToString());
            }
            // a/b/c/ + ../d/e.txt = a/b/d/e.txt
            {
                var r1 = (RelativePath)"a/b/c/";
                var r2 = (RelativePath)"../d/e.txt";
                var r3 = r1 + r2;
                Assert.NotNull(r3);
                Assert.Equal(0, r3.ParentDirectoryCount);
                Assert.Equal("a/b/d/e.txt", r3.ToString());
            }
            // a/b/c.txt + d/e.txt = a/b/d/e.txt
            {
                var r1 = (RelativePath)"a/b/c.txt";
                var r2 = (RelativePath)"d/e.txt";
                var r3 = r1 + r2;
                Assert.NotNull(r3);
                Assert.Equal(0, r3.ParentDirectoryCount);
                Assert.Equal("a/b/d/e.txt", r3.ToString());
            }
            // a/b/c.txt + ../e.txt = a/e.txt
            {
                var r1 = (RelativePath)"a/b/c.txt";
                var r2 = (RelativePath)"../e.txt";
                var r3 = r1 + r2;
                Assert.NotNull(r3);
                Assert.Equal(0, r3.ParentDirectoryCount);
                Assert.Equal("a/e.txt", r3.ToString());
            }
            // ../c.txt + ../e.txt = ../../e.txt
            {
                var r1 = (RelativePath)"../c.txt";
                var r2 = (RelativePath)"../e.txt";
                var r3 = r1 + r2;
                Assert.NotNull(r3);
                Assert.Equal(2, r3.ParentDirectoryCount);
                Assert.Equal("../../e.txt", r3.ToString());
            }
            // "" + ../a.txt = ../a.txt
            {
                var r1 = (RelativePath)"";
                var r2 = (RelativePath)"../a.txt";
                var r3 = r1 + r2;
                Assert.NotNull(r3);
                Assert.Equal(1, r3.ParentDirectoryCount);
                Assert.Equal("../a.txt", r3.ToString());
            }
            // ../a/b.txt + "" = ../a/
            {
                var r1 = (RelativePath)"../a/b.txt";
                var r2 = (RelativePath)"";
                var r3 = r1 + r2;
                Assert.NotNull(r3);
                Assert.Equal(1, r3.ParentDirectoryCount);
                Assert.Equal("../a/", r3.ToString());
            }
            // a/ + ../ = ""
            {
                var r1 = (RelativePath)"a/";
                var r2 = (RelativePath)"../";
                var r3 = r1 + r2;
                Assert.NotNull(r3);
                Assert.Equal(0, r3.ParentDirectoryCount);
                Assert.Equal("", r3.ToString());
                Assert.Same(RelativePath.Empty, r3);
            }
            // "" + "" = ""
            {
                var r1 = (RelativePath)"";
                var r2 = (RelativePath)"";
                var r3 = r1 + r2;
                Assert.NotNull(r3);
                Assert.Equal(0, r3.ParentDirectoryCount);
                Assert.Equal("", r3.ToString());
                Assert.Same(RelativePath.Empty, r1);
                Assert.Same(RelativePath.Empty, r2);
                Assert.Same(RelativePath.Empty, r3);
            }
        }

        [Fact]
        public void TestRelativePathOperatorSub()
        {
            // a/b/c.txt - d/e.txt = ../a/b/c.txt
            {
                var r1 = (RelativePath)"a/b/c.txt";
                var r2 = (RelativePath)"d/e.txt";
                var r3 = r1 - r2;
                Assert.NotNull(r3);
                Assert.Equal(1, r3.ParentDirectoryCount);
                Assert.Equal("../a/b/c.txt", r3.ToString());
            }
            // a/b/c.txt - a/d.txt = b/c.txt
            {
                var r1 = (RelativePath)"a/b/c.txt";
                var r2 = (RelativePath)"a/d.txt";
                var r3 = r1 - r2;
                Assert.NotNull(r3);
                Assert.Equal(0, r3.ParentDirectoryCount);
                Assert.Equal("b/c.txt", r3.ToString());
            }
            // ../../a.txt - ../b.txt = ../a.txt
            {
                var r1 = (RelativePath)"../../a.txt";
                var r2 = (RelativePath)"../b.txt";
                var r3 = r1 - r2;
                Assert.NotNull(r3);
                Assert.Equal(1, r3.ParentDirectoryCount);
                Assert.Equal("../a.txt", r3.ToString());
            }
            // ../../a.txt - ../b/c.txt = ../../a.txt
            {
                var r1 = (RelativePath)"../../a.txt";
                var r2 = (RelativePath)"../b/c.txt";
                var r3 = r1 - r2;
                Assert.NotNull(r3);
                Assert.Equal(2, r3.ParentDirectoryCount);
                Assert.Equal("../../a.txt", r3.ToString());
            }
            // a.txt - ../b.txt = Oop...
            {
                var r1 = (RelativePath)"a.txt";
                var r2 = (RelativePath)"../b.txt";
                Assert.Throws<NotSupportedException>(() => r1 - r2);
            }
            // a/b.txt - "" = a/b.txt
            {
                var r1 = (RelativePath)"a/b.txt";
                var r2 = (RelativePath)"";
                var r3 = r1 - r2;
                Assert.NotNull(r3);
                Assert.Equal(0, r3.ParentDirectoryCount);
                Assert.Equal("a/b.txt", r3.ToString());
            }
            // "" - a/b.txt = ../
            {
                var r1 = (RelativePath)"";
                var r2 = (RelativePath)"a/b.txt";
                var r3 = r1 - r2;
                Assert.NotNull(r3);
                Assert.Equal(1, r3.ParentDirectoryCount);
                Assert.Equal("../", r3.ToString());
            }
            // "a/" - "a/" = ""
            {
                var r1 = (RelativePath)"a/";
                var r2 = (RelativePath)"a/";
                var r3 = r1 - r2;
                Assert.NotNull(r3);
                Assert.Equal(0, r3.ParentDirectoryCount);
                Assert.Equal("", r3.ToString());
                Assert.Same(RelativePath.Empty, r3);
            }
            // "" - "" = ""
            {
                var r1 = (RelativePath)"";
                var r2 = (RelativePath)"";
                var r3 = r1 - r2;
                Assert.NotNull(r3);
                Assert.Equal(0, r3.ParentDirectoryCount);
                Assert.Equal("", r3.ToString());
                Assert.Same(RelativePath.Empty, r1);
                Assert.Same(RelativePath.Empty, r2);
                Assert.Same(RelativePath.Empty, r3);
            }
        }

        [Fact]
        public void TestRelativePathRebase()
        {
            // a/b/c.txt rebase from x/y.txt to d/e.txt = ../x/a/b/c.txt
            {
                var r1 = (RelativePath)"a/b/c.txt";
                var from = (RelativePath)"x/y.txt";
                var to = (RelativePath)"d/e.txt";
                var r2 = r1.Rebase(from, to);
                Assert.NotNull(r2);
                Assert.Equal(1, r2.ParentDirectoryCount);
                Assert.Equal("../x/a/b/c.txt", r2.ToString());
            }
        }

        [Fact]
        public void TestRelativePathFromWorkingFolder()
        {
            {
                var s = "~/";
                var r = (RelativePath)s;
                Assert.NotNull(r);
                Assert.True(r.IsFromWorkingFolder());
                Assert.Equal(0, r.ParentDirectoryCount);
                Assert.Equal(s, r.ToString());
                Assert.Equal(s, r);
                Assert.Same(RelativePath.WorkingFolder, r);
            }
            {
                var s = "~/../a.txt";
                var r = (RelativePath)s;
                Assert.NotNull(r);
                Assert.True(r.IsFromWorkingFolder());
                Assert.Equal(1, r.ParentDirectoryCount);
                Assert.Equal(s, r.ToString());
                Assert.Equal(s, r);
            }
            {
                var s = "~/a.dir/";
                var r = (RelativePath)s;
                Assert.NotNull(r);
                Assert.True(r.IsFromWorkingFolder());
                Assert.Equal(0, r.ParentDirectoryCount);
                Assert.Equal(s, r.ToString());
                Assert.Equal(s, r);
            }
        }

        [Theory]
        [InlineData("a/b/c", "a/b/c")]
        [InlineData("../a/b/c", "../a/b/c")]
        [InlineData("a/b/c d", "a/b/c%20d")]
        [InlineData("../a+b/c/d", "../a%2Bb/c/d")]
        [InlineData("a%3fb", "a%253fb")]
        public void TestUrlEncode(string path, string expected)
        {
            Assert.Equal(expected, ((RelativePath)path).UrlEncode());
        }

        [Theory]
        [InlineData("a/b/c", "a/b/c")]
        [InlineData("../a/b/c", "../a/b/c")]
        [InlineData("a/b/c%20d", "a/b/c d")]
        [InlineData("../a%2Bb/c/d", "../a+b/c/d")]
        [InlineData("a%253fb", "a%3fb")]
        [InlineData("a%2fb", "a%2fb")]
        [InlineData("%2A%2F%3A%3C%3E%3F%5C%7C", "%2A%2F%3A%3C%3E%3F%5C%7C")] //*/:<>?\|
        [InlineData("%2a%2f%3a%3c%3e%3f%5c%7c", "%2a%2f%3a%3c%3e%3f%5c%7c")]
        public void TestUrlDecode(string path, string expected)
        {
            Assert.Equal(expected, ((RelativePath)path).UrlDecode());
        }

        [Theory]
        [InlineData("a/b/c", "a/b/", true)]
        [InlineData("~/a/b/c", "~/a/b/", true)]
        [InlineData("a/b/c", "~/a/b/", false)]
        [InlineData("~/a/b/c", "a/b/", false)]
        [InlineData("a/b", "a/b", false)]
        [InlineData("a/b/", "a/b", false)]
        [InlineData("a/b", "a/b/", false)]
        [InlineData("a/b/", "a/b/", true)]
        [InlineData("a/b/c", "a/b/c", false)]
        [InlineData("a/b/c", "a/b/c/d", false)]
        [InlineData("a/b/c", "a/b/d", false)]
        [InlineData("a/../b/c", "b/", true)]
        [InlineData("../a/b", "../a", false)]
        [InlineData("../a/b", "../", false)]
        [InlineData("../a/b", "../../a", false)]
        [InlineData("../../", "../", false)]
        [InlineData("../", "../../", false)]
        [InlineData("~/a/b", "~/../", false)]
        public void TestStartsWith(string source, string dest, bool isStarstsWith)
        {
            Assert.Equal(isStarstsWith, ((RelativePath)source).InDirectory((RelativePath)dest));
        }
    }
}
