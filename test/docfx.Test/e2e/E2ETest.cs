// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.Docs.Build
{
    public static class E2ETest
    {
        public static readonly TheoryData<string, int> Specs = FindTestSpecs();

        [Theory]
        [MemberData(nameof(Specs))]
        public static async Task Run(string name, int ordinal)
        {
            var (docsetPath, spec) = await CreateDocset(name, ordinal);

            if (!string.IsNullOrEmpty(spec.OS) &&
                !spec.OS.Split(',').Any(os => RuntimeInformation.IsOSPlatform(OSPlatform.Create(os.Trim()))))
            {
                return;
            }

            await Program.Run(new[] { "restore", docsetPath });
            await Program.Run(new[] { "build", docsetPath });

            // Verify restored files
            foreach (var (file, content) in spec.Restores)
            {
                var restoredFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".docfx", "git", file);
                Assert.True(File.Exists(restoredFile));
                Assert.Equal(content, File.ReadAllText(restoredFile).Trim());
            }

            // Verify output
            var docsetOutputPath = Path.Combine(docsetPath, "_site");
            Assert.True(Directory.Exists(docsetPath));

            var outputs = Directory.GetFiles(docsetOutputPath, "*", SearchOption.AllDirectories);
            var outputFileNames = outputs.Select(file => file.Substring(docsetOutputPath.Length + 1).Replace('\\', '/')).ToList();

            Assert.Equal(spec.Outputs.Keys.OrderBy(_ => _), outputFileNames.OrderBy(_ => _));

            foreach (var (filename, content) in spec.Outputs)
            {
                VerifyFile(Path.GetFullPath(Path.Combine(docsetOutputPath, filename)), content);
            }
        }

        private static TheoryData<string, int> FindTestSpecs()
        {
            var result = new TheoryData<string, int>();

            Parallel.ForEach(
                Directory.EnumerateFiles("specs", "*.yml", SearchOption.AllDirectories),
                file =>
                {
                    var i = 0;
                    foreach (var header in FindTestSpecHeadersInFile(file))
                    {
                        if (string.IsNullOrEmpty(header))
                        {
                            i++;
                            continue;
                        }

                        var name = $"{i + 1:D2}. {header}";
                        var folder = Path.Combine(
                            file.Replace("\\", "/").Replace($"specs/", "").Replace(".yml", ""),
                            name).Replace("\\", "/");

                        result.Add(folder, i++);
                    }
                });

            return result;
        }

        private static async Task<(string docsetPath, E2ESpec spec)> CreateDocset(string specName, int ordinal)
        {
            var i = specName.LastIndexOf('/');
            var specPath = specName.Substring(0, i) + ".yml";
            var sections = File.ReadAllText(Path.Combine("specs", specPath)).Split("\n---", StringSplitOptions.RemoveEmptyEntries);
            var yaml = sections[ordinal].Trim('\r', '\n', '-');
            var spec = YamlUtility.Deserialize<E2ESpec>(yaml);
            var docsetPath = Path.Combine("specs.drop", specName.Replace("<", "").Replace(">", ""));

            if (Directory.Exists(docsetPath))
            {
                // Directory.Delete(recursive: true) does not delete hidden files. .git folder is hidden.
                DeleteDirectory(docsetPath);
            }

            if (!string.IsNullOrEmpty(spec.Repo))
            {
                var (_, remote, refspec) = Restore.GetGitRestoreInfo(spec.Repo);
                await GitUtility.Clone(Path.GetDirectoryName(docsetPath), remote, Path.GetFileName(docsetPath), refspec);
                return (docsetPath, spec);
            }

            foreach (var (file, content) in spec.Inputs)
            {
                var filePath = Path.Combine(docsetPath, file);
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                File.WriteAllText(filePath, content);
            }

            return (docsetPath, spec);
        }

        private static void DeleteDirectory(string targetDir)
        {
            string[] files = Directory.GetFiles(targetDir);
            string[] dirs = Directory.GetDirectories(targetDir);

            foreach (string file in files)
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }

            foreach (string dir in dirs)
            {
                DeleteDirectory(dir);
            }

            Directory.Delete(targetDir, false);
        }

        private static IEnumerable<string> FindTestSpecHeadersInFile(string path)
        {
            var sections = File.ReadAllText(path).Split("\n---", StringSplitOptions.RemoveEmptyEntries);

            foreach (var section in sections)
            {
                var yaml = section.Trim('\r', '\n', '-');
                var header = YamlUtility.ReadHeader(yaml) ?? "";

                foreach (var ch in Path.GetInvalidPathChars())
                {
                    header = header.Replace(ch, ' ');
                }

                yield return header.Replace('/', ' ').Replace('\\', ' ');
            }
        }

        private static void VerifyFile(string file, string content)
        {
            switch (Path.GetExtension(file.ToLower()))
            {
                case ".json":
                case ".manifest":
                    TestHelper.VerifyJsonContainEquals(
                        JToken.Parse(content ?? "{}"),
                        JToken.Parse(File.ReadAllText(file)));
                    break;
                case ".log":
                    var expected = content.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).OrderBy(_ => _);
                    var actual = File.ReadAllLines(file).OrderBy(_ => _);
                    Assert.Equal(string.Join("\n", expected), string.Join("\n", actual));
                    break;
                default:
                    Assert.Equal(
                        content?.Trim() ?? "",
                        File.ReadAllText(file).Trim(),
                        ignoreCase: false,
                        ignoreLineEndingDifferences: true,
                        ignoreWhiteSpaceDifferences: true);
                    break;
            }
        }
    }
}
