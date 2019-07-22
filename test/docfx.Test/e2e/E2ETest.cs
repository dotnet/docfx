// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.TestHost;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.Docs.Build
{
    public static class E2ETest
    {
        private static readonly string[] s_errorCodesWithoutLineInfo =
        {
            "need-restore", "heading-not-found", "config-not-found", "committish-not-found", "custom-404-page", "json-syntax-error",

            // can be removed
            "moniker-config-missing",

            // should have, maybe sometimes not
            "download-failed", "locale-invalid",

            // show multiple errors with line info
            "publish-url-conflict", "output-path-conflict", "uid-conflict", "xref-property-conflict", "redirection-conflict",
            "redirected-id-conflict", "circular-reference", "moniker-overlapping", "empty-monikers"
        };

        private static readonly ConcurrentDictionary<string, (int ordinal, string spec)> s_mockRepos = new ConcurrentDictionary<string, (int ordinal, string spec)>();

        public static readonly TheoryData<string> Specs = FindTestSpecs();

        private static readonly AsyncLocal<IReadOnlyDictionary<string, string>> t_mockedRepos = new AsyncLocal<IReadOnlyDictionary<string, string>>();
        private static readonly AsyncLocal<string> t_cachePath = new AsyncLocal<string>();

        static E2ETest()
        {
            AppData.GetCachePath = () => t_cachePath.Value;
            GitUtility.GitRemoteProxy = remote =>
            {
                var mockedRepos = t_mockedRepos.Value;
                if (mockedRepos != null && mockedRepos.TryGetValue(remote, out var mockedLocation))
                {
                    return mockedLocation;
                }
                return remote;
            };
        }

        [Theory]
        [MemberData(nameof(Specs))]
        public static async Task Run(string name)
        {
            var (docsetPath, spec, mockedRepos, cachePath) = CreateDocset(name);
            if (spec is null)
            {
                return;
            }

            try
            {
                t_mockedRepos.Value = mockedRepos;
                t_cachePath.Value = cachePath;

                var osMatches = string.IsNullOrEmpty(spec.OS) || spec.OS.Split(',').Any(
                    os => RuntimeInformation.IsOSPlatform(OSPlatform.Create(os.Trim().ToUpperInvariant())));

                if (osMatches)
                {
                    if (spec.Watch)
                    {
                        await RunWatchCore(docsetPath, spec);
                    }
                    else
                    {
                        await RunCore(docsetPath, spec);
                    }
                }
                else
                {
                    await Assert.ThrowsAnyAsync<XunitException>(() => RunCore(docsetPath, spec));
                }
            }
            finally
            {
                t_mockedRepos.Value = null;
                t_cachePath.Value = null;
            }
        }

        private static async Task RunCore(string docsetPath, E2ESpec spec)
        {
            var dir = new DirectoryInfo(docsetPath);
            var inputFiles = dir.GetFiles("*", SearchOption.AllDirectories).ToDictionary(file => file.FullName, file => file.LastWriteTime);

            bool failed = false;
            foreach (var command in spec.Commands)
            {
                if (await Docfx.Run(command.Split(" ").Concat(new[] { docsetPath }).ToArray()) != 0)
                {
                    failed = true;
                    break;
                }
            }

            // Verify output
            var docsetOutputPath = Path.Combine(docsetPath, "_site");
            Assert.True(Directory.Exists(docsetOutputPath), $"{docsetOutputPath} does not exist");

            var outputs = Directory.GetFiles(docsetOutputPath, "*", SearchOption.AllDirectories);
            var outputFileNames = outputs.Select(file => file.Substring(docsetOutputPath.Length + 1).Replace('\\', '/')).ToList();

            // Show .errors.log content if actual output has errors or warnings.
            if (failed && outputFileNames.Contains(".errors.log"))
            {
                Console.WriteLine($"{Path.GetFileName(docsetPath)}: {File.ReadAllText(Path.Combine(docsetOutputPath, ".errors.log"))}");
            }

            // These files output mostly contains empty content which e2e tests are not intrested in
            // we can just skip the verification for them
            foreach (var skippableItem in spec.SkippableOutputs)
            {
                if (!spec.Outputs.ContainsKey(skippableItem))
                {
                    outputFileNames.Remove(skippableItem);
                }
            }

            // Verify output
            Assert.Equal(spec.Outputs.Keys.Where(k => !k.StartsWith("~/")).OrderBy(_ => _), outputFileNames.OrderBy(_ => _));

            foreach (var (filename, content) in spec.Outputs)
            {
                if (filename.StartsWith("~/"))
                {
                    VerifyFile(Path.GetFullPath(Path.Combine(docsetPath, filename.Substring(2))), content);
                    continue;
                }
                VerifyFile(Path.GetFullPath(Path.Combine(docsetOutputPath, filename)), content);
            }

            VerifyNoChangesOnInputFiles(inputFiles, spec.SkippableInputs, docsetPath);
        }

        private static async Task RunWatchCore(string docsetPath, E2ESpec spec)
        {
            var server = new TestServer(Watch.CreateWebServer(docsetPath, new CommandLineOptions()));

            foreach (var (request, response) in spec.Http)
            {
                var responseContext = await server.SendAsync(requestContext => requestContext.Request.Path = "/" + request);
                var body = new StreamReader(responseContext.Response.Body).ReadToEnd();
                var actualResponse = new JObject
                {
                    ["status"] = responseContext.Response.StatusCode,
                    ["body"] = body,
                };
                TestUtility.VerifyJsonContainEquals(response, actualResponse);
            }

            // Verify no output in output directory
            Assert.False(Directory.Exists(Path.Combine(docsetPath, "_site")));
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
                    if (string.IsNullOrEmpty(header) || header.Contains("[skip]", StringComparison.OrdinalIgnoreCase))
                    {
                        i++;
                        continue;
                    }
#if DEBUG
                    var only = header.Contains("[only]", StringComparison.OrdinalIgnoreCase);
#else
                    var only = false;
#endif
                    if (Path.GetFileNameWithoutExtension(file).Contains("localization"))
                    {
                        var spec = YamlUtility.Deserialize<E2ESpec>(yaml, null);
                        if (spec.Commands != null && spec.Commands.Any(c => c != null && c.Contains("--locale"))
                            && spec.Repos.Count() > 1 && !spec.Inputs.Any() && string.IsNullOrEmpty(spec.Repo) && !header.Contains("[from loc]"))
                        {
                            specNames.Add(($"{Path.GetFileNameWithoutExtension(file)}/{i:D2}. [from loc] {header}", only));
                        }
                    }
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

        private static (string docsetPath, E2ESpec spec, IReadOnlyDictionary<string, string> mockedRepos, string cachePath)
            CreateDocset(string specName)
        {
            var match = Regex.Match(specName, "^(.+?)/(\\d+). (\\[from loc\\] )?(.*)");
            var specPath = match.Groups[1].Value + ".yml";
            var ordinal = int.Parse(match.Groups[2].Value);
            var sections = File.ReadAllText(Path.Combine("specs", specPath)).Split("\n---", StringSplitOptions.RemoveEmptyEntries);
            var yaml = sections[ordinal].Trim('\r', '\n', '-');
            var fromLoc = !string.IsNullOrEmpty(match.Groups[3].Value);
            Assert.True(yaml.StartsWith($"# {match.Groups[4].Value}") || yaml.StartsWith($"# {match.Groups[3].Value}{match.Groups[4].Value}"));

            var yamlHash = HashUtility.GetMd5Hash(yaml).Substring(0, 5);
            var name = ToSafePathString(specName).Substring(0, Math.Min(30, specName.Length)) + "-" + yamlHash;

            var spec = YamlUtility.Deserialize<E2ESpec>(yaml, null);

            var emptyEnvName = spec.Environments.FirstOrDefault(env => string.IsNullOrEmpty(Environment.GetEnvironmentVariable(env)));
            if (!string.IsNullOrEmpty(emptyEnvName))
            {
                Log.Write($"{specName} is skipped due to empty environment value: {emptyEnvName}");
                return default;
            }

            var inputFolder = Path.Combine("specs-drop", name);
            var inputFolderCreatedFlag = Path.Combine("specs-flags", name);
            if (fromLoc)
            {
                spec.Commands = new[] { "restore", "build" };
            }
            var mockedRepos = MockGitRepos(specPath, ordinal, name, spec);

            if (!File.Exists(inputFolderCreatedFlag))
            {
                var inputRepo = spec.Repo
                    ?? spec.Repos.Select(r => r.Key).FirstOrDefault(r => !fromLoc || LocalizationUtility.TryGetContributionBranch(UrlUtility.SplitGitUrl(r).refspec, out _))
                    ?? spec.Repos.Select(r => r.Key).FirstOrDefault(r => !fromLoc || LocalizationUtility.TryRemoveLocale(UrlUtility.SplitGitUrl(r).remote.Split(new char[] { '\\', '/' }).Last(), out _, out _));
                if (!string.IsNullOrEmpty(inputRepo))
                {
                    try
                    {
                        t_mockedRepos.Value = mockedRepos;

                        var (remote, refspec, _) = UrlUtility.SplitGitUrl(inputRepo);
                        GitUtility.InitFetchCheckout(inputFolder, remote, refspec);
                        Process.Start(new ProcessStartInfo("git", "submodule update --init") { WorkingDirectory = inputFolder }).WaitForExit();
                    }
                    finally
                    {
                        t_mockedRepos.Value = null;
                    }
                }

                CreateFiles(spec.Inputs, inputFolder, spec.Environments);

                PathUtility.CreateDirectoryFromFilePath(inputFolderCreatedFlag);
                File.Create(inputFolderCreatedFlag);
            }

            var cachePath = spec.Cache.Count > 0 ? Path.Combine("specs-cache", Guid.NewGuid().ToString("N")) : null;
            if (cachePath != null)
            {
                CreateFiles(spec.Cache, cachePath);
            }

            var docset = Path.Combine(inputFolder, spec.Cwd ?? string.Empty);

            if (GitUtility.IsRepo(docset) && !spec.Inputs.Any() && spec.Outputs.Any(k => k.Key.StartsWith("~/")))
            {
                Process.Start(new ProcessStartInfo("git", "clean -fdx") { WorkingDirectory = docset }).WaitForExit();
            }

            if (Directory.Exists(Path.Combine(docset, "_site")))
            {
                Directory.Delete(Path.Combine(docset, "_site"), recursive: true);
            }

            return (docset, spec, mockedRepos, cachePath);
        }

        private static void CreateFiles(Dictionary<string, string> files, string targetFolder, string[] environmentVariables = null)
        {
            foreach (var (file, content) in files)
            {
                var mutableContent = content;
                var filePath = Path.Combine(targetFolder, file);
                PathUtility.CreateDirectoryFromFilePath(filePath);
                if (Path.GetFileNameWithoutExtension(file) == "docfx")
                {
                    mutableContent = ParsePreservedKeys(mutableContent, environmentVariables);
                }
                File.WriteAllText(filePath, mutableContent);
            }
        }

        private static string ParsePreservedKeys(string content, string[] environmentVariables = null)
        {
            environmentVariables = environmentVariables ?? Array.Empty<string>();
            content = content ?? "";
            foreach (var env in environmentVariables)
            {
                content = content.Replace($"{{{env}}}", Environment.GetEnvironmentVariable(env));
            }
            return content.Replace("{APP_BASE_PATH}", AppContext.BaseDirectory);
        }

        private static string ToSafePathString(string value)
        {
            value = value.Replace('/', ' ').Replace('\\', ' ');
            foreach (var ch in Path.GetInvalidPathChars())
            {
                value = value.Replace(ch.ToString(), "");
            }
            foreach (var ch in Path.GetInvalidFileNameChars())
            {
                value = value.Replace(ch.ToString(), "");
            }
            return value;
        }

        private static IReadOnlyDictionary<string, string> MockGitRepos(string file, int ordinal, string name, E2ESpec spec)
        {
            var result = new ConcurrentDictionary<string, string>();
            var repos =
                from pair in spec.Repos
                let info = UrlUtility.SplitGitUrl(pair.Key)
                group (info.refspec, pair.Value)
                by info.remote;

            Parallel.ForEach(repos, repoInfo =>
            {
                var remote = repoInfo.Key;
                var repoPath = Path.Combine("repos", name, ToSafePathString(remote));
                result[remote] = Path.GetFullPath(repoPath);
                if (Directory.Exists(repoPath))
                {
                    return;
                }

                using (var repo = new LibGit2Sharp.Repository(LibGit2Sharp.Repository.Init(repoPath, isBare: true)))
                {
                    foreach (var (branch, commits) in repoInfo)
                    {
                        s_mockRepos.AddOrUpdate($"{remote}#{branch}", (ordinal, file), (key, oldValue) =>
                        {
                            Debug.Assert(oldValue.ordinal == ordinal && oldValue.spec == file, $"test {oldValue} and {ordinal} are using the same mock repo remote: {remote}#{branch}, '{file}'");
                            return oldValue;
                        });

                        var lastCommit = default(LibGit2Sharp.Commit);
                        foreach (var commit in commits.Reverse())
                        {
                            var author = new LibGit2Sharp.Signature(commit.Author, commit.Email, commit.Time);
                            var tree = new LibGit2Sharp.TreeDefinition();
                            var commitIndex = 0;

                            foreach (var (path, content) in commit.Files)
                            {
                                var mutableContent = content;
                                if (Path.GetFileNameWithoutExtension(path) == "docfx")
                                {
                                    mutableContent = ParsePreservedKeys(mutableContent);
                                }
                                var blob = repo.ObjectDatabase.CreateBlob(new MemoryStream(Encoding.UTF8.GetBytes(mutableContent?.Replace("\r", "") ?? "")));
                                tree.Add(path, blob, LibGit2Sharp.Mode.NonExecutableFile);
                            }

                            var currentCommit = repo.ObjectDatabase.CreateCommit(
                                author,
                                author, $"Commit {commitIndex++}",
                                repo.ObjectDatabase.CreateTree(tree),
                                lastCommit != null ? new[] { lastCommit } : Array.Empty<LibGit2Sharp.Commit>(),
                                prettifyMessage: false);

                            lastCommit = currentCommit;
                        }

                        repo.Branches.Add(branch, lastCommit);
                    }
                }
            });
            return result;
        }

        private static void VerifyFile(string file, string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                return;
            }

            string[] actual, expected;
            switch (Path.GetExtension(file.ToLowerInvariant()))
            {
                case ".txt":
                    expected = content.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).OrderBy(_ => _).ToArray();
                    actual = File.ReadAllLines(file).OrderBy(_ => _).ToArray();
                    Assert.Equal(expected.Length, actual.Length);
                    for (var i = 0; i < expected.Length; i++)
                    {
                        TestUtility.VerifyJsonContainEquals(JToken.Parse(expected[i]), JToken.Parse(actual[i]));
                    }
                    break;

                case ".json":
                    TestUtility.VerifyJsonContainEquals(
                        // Test expectation can use YAML for readability
                        content.StartsWith("{") ? JToken.Parse(content) : YamlUtility.Parse(content, null).Item2,
                        JToken.Parse(File.ReadAllText(file)));
                    break;

                case ".log":
                    expected = content.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).OrderBy(_ => _).ToArray();
                    actual = File.ReadAllLines(file).OrderBy(_ => _).ToArray();
                    if (expected.Any(str => str.Contains("*")))
                    {
                        Assert.Matches("^" + Regex.Escape(string.Join("\n", expected)).Replace("\\*", ".*") + "$", string.Join("\n", actual));
                    }
                    else
                    {
                        Assert.Equal(string.Join("\n", expected), string.Join("\n", actual));
                    }
                    VerifyLogsHasLineInfo(actual);
                    break;

                case ".html":
                    Assert.Equal(
                        TestUtility.NormalizeHtml(content),
                        TestUtility.NormalizeHtml(File.ReadAllText(file)));
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

        private static void VerifyLogsHasLineInfo(string[] logs)
        {
            if (logs.Length > 0 && logs[0].StartsWith("["))
            {
                foreach (var log in Array.ConvertAll(logs, JArray.Parse))
                {
                    if (!s_errorCodesWithoutLineInfo.Contains(log[1].ToString()) && log.Count < 5)
                    {
                        Assert.True(false, $"Error code {log[1].ToString()} must have line info");
                    }
                }
            }
        }

        private static void VerifyNoChangesOnInputFiles(Dictionary<string, DateTime> inputFiles, string[] skippableInputs, string docsetPath)
        {
            var dir = new DirectoryInfo(docsetPath);
            var gitdir = Path.GetFullPath(Path.Combine(docsetPath, ".git/"));
            var currentFiles = dir.GetFiles("*", SearchOption.AllDirectories)
                                .Where(file => !skippableInputs.Contains(file.Name))
                                .Select(file => file.FullName)
                                .Except(dir.GetFiles($"_site/*", SearchOption.AllDirectories)
                                .Select(file => file.FullName))
                                .ToList();

            foreach (var (file, lastModifyTime) in inputFiles)
            {
                if (file.StartsWith(gitdir))
                {
                    currentFiles.Remove(file);
                    continue;
                }

                var currentFile = new FileInfo(file);
                if (skippableInputs.Contains(currentFile.Name))
                {
                    continue;
                }
                Assert.True(currentFile.Exists, $"Input file {currentFile.Name} has been deleted");
                Assert.True(lastModifyTime == currentFile.LastWriteTime, $"Input file {currentFile.Name} has been changed");
                currentFiles.Remove(file);
            }

            Assert.True(
                currentFiles.Count == 0,
                $"New files {string.Join(",", currentFiles.Count > 3 ? currentFiles.SkipLast(currentFiles.Count - 3) : currentFiles)} has been generated in input folder");
        }
    }
}
