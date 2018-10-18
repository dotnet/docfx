// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.Docs.Build
{
    public static class E2ETest
    {
        public static readonly TheoryData<string> Specs = FindTestSpecs();

        [Theory]
        [MemberData(nameof(Specs))]
        public static async Task Run(string name)
        {
            var (docsetPath, spec) = await CreateDocset(name);
            if (spec == null)
            {
                return;
            }

            var osMatches = string.IsNullOrEmpty(spec.OS) || spec.OS.Split(',').Any(
                os => RuntimeInformation.IsOSPlatform(OSPlatform.Create(os.Trim().ToUpperInvariant())));

            if (osMatches)
            {
                await RunCore(docsetPath, spec);
            }
            else
            {
                await Assert.ThrowsAnyAsync<XunitException>(() => RunCore(docsetPath, spec));
            }
        }

        private static async Task RunCore(string docsetPath, E2ESpec spec)
        {
            foreach (var command in spec.Commands)
            {
                if (await Program.Run(command.Split(" ").Concat(new[] { docsetPath }).ToArray()) != 0)
                {
                    break;
                }
            }

            // Verify output
            var docsetOutputPath = Path.Combine(docsetPath, "_site");
            Assert.True(Directory.Exists(docsetOutputPath));

            var outputs = Directory.GetFiles(docsetOutputPath, "*", SearchOption.AllDirectories);
            var outputFileNames = outputs.Select(file => file.Substring(docsetOutputPath.Length + 1).Replace('\\', '/')).ToList();

            // Show build.log content if actual output has errors or warnings.
            if (!spec.Outputs.Keys.Contains("build.log") && outputFileNames.Contains("build.log"))
            {
                Assert.True(false, File.ReadAllText(Path.Combine(docsetOutputPath, "build.log")));
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
            Assert.Equal(spec.Outputs.Keys.OrderBy(_ => _), outputFileNames.OrderBy(_ => _));

            foreach (var (filename, content) in spec.Outputs)
            {
                VerifyFile(Path.GetFullPath(Path.Combine(docsetOutputPath, filename)), content);
            }
        }

        private static TheoryData<string> FindTestSpecs()
        {
            var specNames = new List<(string, bool only)>();
            foreach (var file in Directory.EnumerateFiles("specs", "*.yml", SearchOption.AllDirectories))
            {
                var i = 0;
                var content = File.ReadAllText(file);
                var sections = content.Split("\n---", StringSplitOptions.RemoveEmptyEntries);

                foreach (var section in sections)
                {
                    var yaml = section.Trim('\r', '\n', '-');
                    var header = YamlUtility.ReadHeader(yaml) ?? "";
                    if (string.IsNullOrEmpty(header))
                    {
                        i++;
                        continue;
                    }
#if DEBUG
                    var only = header.Contains("[ONLY]", StringComparison.OrdinalIgnoreCase);
#else
                    var only = false;
#endif
                    specNames.Add(($"{Path.GetFileNameWithoutExtension(file)}/{i++:D2}. {header}", only));
                }
            }

            var hasOnly = specNames.Any(spec => spec.only);
            var result = new TheoryData<string>();
            foreach (var (name, only) in specNames)
            {
                if (!hasOnly || only)
                {
                    result.Add(name);
                }
            }
            return result;
        }

        private static async Task<(string docsetPath, E2ESpec spec)> CreateDocset(string specName)
        {
            var match = Regex.Match(specName, "^(.+?)/(\\d+). (.*)");
            var specPath = match.Groups[1].Value + ".yml";
            var ordinal = int.Parse(match.Groups[2].Value);
            var sections = File.ReadAllText(Path.Combine("specs", specPath)).Split("\n---", StringSplitOptions.RemoveEmptyEntries);
            var yaml = sections[ordinal].Trim('\r', '\n', '-');

            Assert.StartsWith($"# {match.Groups[3].Value}", yaml);

            var (_, spec) = YamlUtility.Deserialize<E2ESpec>(yaml, false);
            var docsetPath = Path.Combine("specs-drop", ToSafePathString(specName));

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
            }

            var skip = spec.Environments.Any(env => string.IsNullOrEmpty(Environment.GetEnvironmentVariable(env)));;
            if (skip)
            {
                return default;
            }

            var replaceEnvironments =
                spec.Environments.Length > 0 &&
                !spec.Environments.Any(env => string.IsNullOrEmpty(Environment.GetEnvironmentVariable(env)));

            foreach (var (file, content) in spec.Inputs)
            {
                var mutableContent = content;
                var filePath = Path.Combine(docsetPath, file);
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                if (replaceEnvironments && Path.GetFileNameWithoutExtension(file) == "docfx")
                {
                    foreach (var env in spec.Environments)
                    {
                        mutableContent = content.Replace($"{{{env}}}", Environment.GetEnvironmentVariable(env));
                    }
                }
                File.WriteAllText(filePath, mutableContent);
            }

            return (docsetPath, spec);
        }

        private static string ToSafePathString(string value)
        {
            foreach (var ch in Path.GetInvalidPathChars())
            {
                value = value.Replace(ch, ' ');
            }

            return value.Replace('/', ' ').Replace('\\', ' ').Replace("<", "").Replace(">", "");
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

        private static void VerifyFile(string file, string content)
        {
            switch (Path.GetExtension(file.ToLowerInvariant()))
            {
                case ".json":
                case ".manifest":
                    TestUtility.VerifyJsonContainEquals(
                        JToken.Parse(content ?? "{}"),
                        JToken.Parse(File.ReadAllText(file)));
                    break;
                case ".log":
                    var expected = content.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).OrderBy(_ => _).ToArray();
                    var actual = File.ReadAllLines(file).OrderBy(_ => _).ToArray();
                    // TODO: Configure github token in CI to get rid of github rate limit,
                    // then we could remove the wildcard match
                    if (expected.Any(str => str.Contains("*")))
                    {
                        Assert.Matches("^" + Regex.Escape(string.Join("\n", expected)).Replace("\\*", ".*") + "$", string.Join("\n", actual));
                    }
                    else
                    {
                        Assert.Equal(string.Join("\n", expected), string.Join("\n", actual));
                    }
                    break;
                case ".html":
                    if (!string.IsNullOrEmpty(content))
                    {
                        Assert.Equal(
                            TestUtility.NormalizeHtml(content),
                            TestUtility.NormalizeHtml(File.ReadAllText(file)));
                    }
                    break;
                default:
                    if (!string.IsNullOrEmpty(content))
                    {
                        Assert.Equal(
                            content?.Trim() ?? "",
                            File.ReadAllText(file).Trim(),
                            ignoreCase: false,
                            ignoreLineEndingDifferences: true,
                            ignoreWhiteSpaceDifferences: true);
                    }
                    break;
            }
        }
    }
}
