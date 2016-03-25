// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Glob.Tests
{
    using System.IO;
    using System.Linq;

    using Xunit;

    using Microsoft.DocAsCode.Glob;
    using Microsoft.DocAsCode.Tests.Common;

    public class GlobFileTest : TestBase
    {
        private string _workingDirectory;

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
            Assert.Equal(0, result.Length);
            result = FileGlob.GetFiles(
                _workingDirectory,
                new string[] { "**" },
                new string[] { "**.md" }).ToArray();
            Assert.Equal(6, result.Length);
            result = FileGlob.GetFiles(
                 _workingDirectory,
                 new string[] { "**.md" },
                 new string[] { "**{J,L}/**" }).ToArray();
            Assert.Equal(1, result.Length);
            result = FileGlob.GetFiles(
                 _workingDirectory,
                 new string[] { "**.md", "**.csproj" },
                 new string[] { "**J/**", "**/M/**" }).ToArray();
            Assert.Equal(1, result.Length);
            result = FileGlob.GetFiles(
                 _workingDirectory + "/Root",
                 new string[] { "[EJ]/*.{md,cs,csproj}" },
                 new string[] { "**.cs" }).ToArray();
            Assert.Equal(2, result.Length);
        }

        private static void CreateFilesOrFolders(string cwd, params string[] items)
        {
            if (string.IsNullOrEmpty(cwd)) cwd = ".";
            foreach(var i in items)
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
}
