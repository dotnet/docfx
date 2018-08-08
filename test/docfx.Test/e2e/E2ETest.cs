// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Sdk;

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
            var osMatches = string.IsNullOrEmpty(spec.OS) || spec.OS.Split(',').Any(
                os => RuntimeInformation.IsOSPlatform(OSPlatform.Create(os.Trim().ToUpperInvariant())));

            if (osMatches)
            {
                await RunCore(docsetPath, spec);
            }
            else
            {
                await Assert.ThrowsAnyAsync<AssertActualExpectedException>(() => RunCore(docsetPath, spec));
            }
        }

        private static async Task RunCore(string docsetPath, E2ESpec spec)
        {
            foreach (var command in spec.Commands)
            {
                await Program.Run(command.Split(" ").Concat(new[] { docsetPath }).ToArray());
            }

            // Verify restored files
            foreach (var (file, content) in spec.Restores)
            {
                var restoredFile = Directory.EnumerateFiles(AppData.AppDataDir, file, SearchOption.TopDirectoryOnly).FirstOrDefault();
                Assert.NotNull(restoredFile);
                Assert.True(File.Exists(restoredFile));
                VerifyFile(restoredFile, content);
            }

            // Verify output
            var docsetOutputPath = Path.Combine(docsetPath, "_site");
            Assert.True(Directory.Exists(docsetOutputPath));

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
            foreach (var file in Directory.EnumerateFiles("specs", "*.yml", SearchOption.AllDirectories))
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
            }
            return result;
        }

        private static async Task<(string docsetPath, E2ESpec spec)> CreateDocset(string specName, int ordinal)
        {
            var i = specName.LastIndexOf('/');
            var specPath = specName.Substring(0, i) + ".yml";
            var sections = File.ReadAllText(Path.Combine("specs", specPath)).Split("\n---", StringSplitOptions.RemoveEmptyEntries);
            var yaml = sections[ordinal].Trim('\r', '\n', '-');
            var (_, spec) = YamlUtility.Deserialize<E2ESpec>(yaml, false);
            var docsetPath = Path.Combine("specs-drop", specName.Replace("<", "").Replace(">", ""));

            if (Directory.Exists(docsetPath))
            {
                // Directory.Delete(recursive: true) does not delete hidden files. .git folder is hidden.
                DeleteDirectory(docsetPath);
            }

            if (!string.IsNullOrEmpty(spec.Repo))
            {
                var (remote, refspec) = GitUtility.GetGitRemoteInfo(spec.Repo);
                await GitUtility.Clone(Path.GetDirectoryName(docsetPath), remote, Path.GetFileName(docsetPath), refspec);
                Process.Start(new ProcessStartInfo("git", "submodule update --init") { WorkingDirectory = docsetPath }).WaitForExit();
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
                    var expected = content.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).OrderBy(_ => _).ToList();
                    var actual = File.ReadAllLines(file).OrderBy(_ => _).ToList();
                    TestHelper.VerifyLogEquals(expected, actual);
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
