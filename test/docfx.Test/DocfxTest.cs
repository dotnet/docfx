// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DocAsTest;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.Docs.Build
{
    public static class DocfxTest
    {
        private static readonly JsonDiff s_jsonDiff = CreateJsonDiff();

        private static readonly AsyncLocal<IReadOnlyDictionary<string, string>> t_repos = new AsyncLocal<IReadOnlyDictionary<string, string>>();
        private static readonly AsyncLocal<string> t_cachePath = new AsyncLocal<string>();
        private static readonly AsyncLocal<string> t_statePath = new AsyncLocal<string>();

        static DocfxTest()
        {
            AppData.GetCachePath = () => t_cachePath.Value;
            AppData.GetStatePath = () => t_statePath.Value;
            GitUtility.GitRemoteProxy = remote =>
            {
                var mockedRepos = t_repos.Value;
                if (mockedRepos != null && mockedRepos.TryGetValue(remote, out var mockedLocation))
                {
                    return mockedLocation;
                }
                return remote;
            };
        }

        [YamlTest("~/docs/specs/**/*.yml")]
        [MarkdownTest("~/docs/designs/**/*.md")]
        public static async Task Run(TestData test, DocfxTestSpec spec)
        {
            var (docsetPath, cachePath, statePath, outputPath, repos) = CreateDocset(test, spec);

            try
            {
                t_repos.Value = repos;
                t_cachePath.Value = cachePath;
                t_statePath.Value = statePath;

                if (OsMatches(spec.OS))
                {
                    await RunCore(test, docsetPath, outputPath, spec);
                }
                else
                {
                    await Assert.ThrowsAnyAsync<Exception>(() => RunCore(test, docsetPath, outputPath, spec));
                }
            }
            finally
            {
                t_repos.Value = null;
                t_cachePath.Value = null;
                t_statePath.Value = null;
            }
        }

        private static (string docsetPath, string cachePath, string statePath, string outputPath, Dictionary<string, string> repos)
            CreateDocset(TestData test, DocfxTestSpec spec)
        {
            var testName = $"{Path.GetFileName(test.FilePath)}-{test.Ordinal:D2}-{HashUtility.GetMd5HashShort(test.Content)}";
            var basePath = Path.GetFullPath(Path.Combine("docfx-test", testName));
            var outputPath = Path.GetFullPath(Path.Combine(basePath, "outputs/"));
            var cachePath = Path.Combine(basePath, "cache/");
            var statePath = Path.Combine(basePath, "state/");
            var markerPath = Path.Combine(basePath, "marker");

            var variables = new Dictionary<string, string>
            {
                { "APP_BASE_PATH", AppContext.BaseDirectory },
                { "OUTPUT_PATH", outputPath },
                { "CACHE_PATH", cachePath },
                { "STATE_PATH", statePath },
                { "DOCS_GITHUB_TOKEN", Environment.GetEnvironmentVariable("DOCS_GITHUB_TOKEN") },
                { "MICROSOFT_GRAPH_CLIENT_SECRET", Environment.GetEnvironmentVariable("MICROSOFT_GRAPH_CLIENT_SECRET") },
            };

            var missingVariables = spec.Environments.Where(env => !variables.TryGetValue(env, out var value) || string.IsNullOrEmpty(value));
            if (missingVariables.Any())
            {
                throw new TestSkippedException($"Missing variable {string.Join(',', missingVariables)}");
            }

            Directory.CreateDirectory(basePath);

            var repos = spec.Repos
                .Select(repo => new PackageUrl(repo.Key).Url)
                .Distinct()
                .Select((remote, index) => (remote, index))
                .ToDictionary(
                    remote => remote.remote,
                    remote => Path.Combine(basePath, "repos", remote.index.ToString()));

            var docsetPath = repos.Select(item => item.Value).FirstOrDefault() ?? Path.Combine(basePath, "inputs");

            if (!File.Exists(markerPath))
            {
                foreach (var (url, commits) in spec.Repos.Reverse())
                {
                    var packageUrl = new PackageUrl(url);
                    TestUtility.CreateGitRepository(repos[packageUrl.Url], commits, packageUrl.Url, packageUrl.Branch, variables);
                }

                TestUtility.CreateFiles(docsetPath, spec.Inputs, variables);
                TestUtility.CreateFiles(cachePath, spec.Cache, variables);
                TestUtility.CreateFiles(statePath, spec.State, variables);

                File.WriteAllText(markerPath, "");
            }

            return (docsetPath, cachePath, statePath, outputPath, repos);
        }

        private async static Task RunCore(TestData test, string docsetPath, string outputPath, DocfxTestSpec spec)
        {
            if (spec.Watch)
            {
                throw new TestSkippedException("Skip watch tests");
            }

            if (!test.Summary.Contains("[from loc]", StringComparison.OrdinalIgnoreCase))
            {
                await RunBuild(docsetPath, outputPath, spec, spec.Locale);
            }

            // Verify build from localization docset also work
            if (spec.Locale != null)
            {
                var locDocsetPath = t_repos.Value.FirstOrDefault(
                    repo => repo.Key.EndsWith($".{spec.Locale}") || repo.Key.EndsWith(".loc")).Value;
                if (locDocsetPath != null)
                {
                    await RunBuild(locDocsetPath, outputPath, spec, locale: null);
                }
            }
        }

        private static async Task RunBuild(string docsetPath, string outputPath, DocfxTestSpec spec, string locale)
        {
            Directory.CreateDirectory(outputPath);
            Directory.Delete(outputPath, recursive: true);
            Directory.CreateDirectory(outputPath);

            docsetPath = Path.Combine(docsetPath, spec.Cwd ?? "");

            using (TestUtility.EnsureFilesNotChanged(docsetPath))
            {
                var options = $"{(spec.Legacy ? "--legacy" : "")} {(locale != null ? $"--locale {locale}" : "")}"
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Concat(new[] { "--output", outputPath })
                    .ToArray();

                if (spec.Restore)
                {
                    Assert.Equal(0, await Docfx.Run(new[] { "restore", docsetPath }.Concat(options).ToArray()));
                }
                if (spec.Build)
                {
                    Assert.Equal(0, await Docfx.Run(new[] { "build", docsetPath }.Concat(options).ToArray()));
                }
            }

            VerifyOutput(outputPath, spec);
        }

        private static void VerifyOutput(string outputPath, DocfxTestSpec spec)
        {
            var expectedOutputs = JObject.FromObject(spec.Outputs);

            // Ensure no .errors.log file if there is no error
            if (!spec.Outputs.ContainsKey(".errors.log"))
            {
                expectedOutputs[".errors.log"] = JValue.CreateUndefined();
            }

            var actualOutputs = Directory
                .GetFiles(outputPath, "*", SearchOption.AllDirectories)
                .ToDictionary(file => file.Substring(outputPath.Length).Replace('\\', '/'), File.ReadAllText);

            s_jsonDiff.Verify(expectedOutputs, actualOutputs);
        }

        private static JsonDiff CreateJsonDiff()
        {
            return new JsonDiffBuilder()
                .UseAdditionalProperties()
                .UseNegate()
                .UseRegex()
                .UseWildcard()
                .UseIgnoreNull(IsOutputFile)
                .UseJson()
                .UseHtml(IsHtml)
                .Use(IsHtml, RemoveDataLinkType)
                .UseLogFile()
                .Build();
        }

        private static bool IsOutputFile(JToken expected, JToken actual, string name)
        {
            // TODO: this will also match first property inside JSON file.
            return expected.Parent?.Parent == expected.Root;
        }

        private static bool IsHtml(JToken expected, JToken actual, string name)
        {
            if (name.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
                return true;

            if (expected is JValue value && value.Value is string str &&
                str.Trim() is string html && html.StartsWith('<') && html.EndsWith('>'))
            {
                return true;
            }

            return false;
        }

        private static (JToken expected, JToken actual) RemoveDataLinkType(JToken expected, JToken actual, string name, JsonDiff diff)
        {
            // Compare data-linktype only if the expectation contains data-linktype
            var expectedHtml = expected.Value<string>();
            var actualHtml = actual.Value<string>();
            if (string.IsNullOrEmpty(expectedHtml) || string.IsNullOrEmpty(expectedHtml))
            {
                return (expectedHtml, actualHtml);
            }
            if (!expectedHtml.Contains("data-linktype"))
            {
                actualHtml = Regex.Replace(actualHtml, " data-linktype=\".*?\"", "");
            }
            return (expectedHtml, actualHtml);
        }

        private static JsonDiffBuilder UseLogFile(this JsonDiffBuilder builder)
        {
            return builder.Use(
                (expected, actual, name) => name.EndsWith(".txt") || name.EndsWith(".log"),
                (expected, actual, name, jsonDiff) =>
                {
                    if (expected.Type != JTokenType.String || actual.Type != JTokenType.String)
                    {
                        return (expected, actual);
                    }

                    var expectedLines = expected.Value<string>()
                        .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                        .OrderBy(_ => _)
                        .ToArray();

                    var actualLines = actual.Value<string>()
                        .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                        .OrderBy(_ => _)
                        .ToArray();

                    for (var i = 0; i < Math.Min(expectedLines.Length, actualLines.Length); i++)
                    {
                        var (e, a) = jsonDiff.Normalize(
                            JToken.Parse(expectedLines[i]),
                            JToken.Parse(actualLines[i]));

                        expectedLines[i] = e.ToString(Formatting.Indented);
                        actualLines[i] = a.ToString(Formatting.Indented);
                    }

                    return (string.Join('\n', expectedLines), string.Join('\n', actualLines));
                });
        }

        private static bool OsMatches(string os)
        {
            return string.IsNullOrEmpty(os) ||
                os.Split(',').Any(
                    item => RuntimeInformation.IsOSPlatform(OSPlatform.Create(item.Trim().ToUpperInvariant())));
        }
    }
}
