// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Yunit;

namespace Microsoft.Docs.Build
{
    public static class DocfxTest
    {
        private static readonly JsonDiff s_jsonDiff = CreateJsonDiff();

        private static readonly AsyncLocal<IReadOnlyDictionary<string, string>> t_repos = new AsyncLocal<IReadOnlyDictionary<string, string>>();
        private static readonly AsyncLocal<string> t_appDataPath = new AsyncLocal<string>();

        static DocfxTest()
        {
            TestQuirks.AppDataPath = () => t_appDataPath.Value;

            TestQuirks.GitRemoteProxy = remote =>
            {
                var mockedRepos = t_repos.Value;
                if (mockedRepos != null && mockedRepos.TryGetValue(remote, out var mockedLocation))
                {
                    return mockedLocation;
                }
                return remote;
            };
        }

        public static IEnumerable<string> ExpandTest(DocfxTestSpec spec)
        {
            yield return "";

            if (!spec.NoDryRun && spec.Outputs.ContainsKey(".errors.log"))
            {
                yield return "DryRun";
            }
        }

        [YamlTest("~/docs/specs/**/*.yml", ExpandTest = nameof(ExpandTest))]
        [MarkdownTest("~/docs/designs/**/*.md", ExpandTest = nameof(ExpandTest))]
        public static void Run(TestData test, DocfxTestSpec spec)
        {
            if (!OsMatches(spec.OS))
            {
                throw new TestSkippedException("OS not supported");
            }

            var (docsetPath, appDataPath, outputPath, repos) = CreateDocset(test, spec);

            try
            {
                t_repos.Value = repos;
                t_appDataPath.Value = appDataPath;
                RunCore(docsetPath, outputPath, test, spec);
            }
            catch (Exception exception)
            {
                while (exception is AggregateException ae && ae.InnerException != null)
                {
                    exception = ae.InnerException;
                }
                ExceptionDispatchInfo.Capture(exception).Throw();
            }
            finally
            {
                t_repos.Value = null;
                t_appDataPath.Value = null;
            }
        }

        private static (string docsetPath, string appDataPath, string outputPath, Dictionary<string, string> repos)
            CreateDocset(TestData test, DocfxTestSpec spec)
        {
            var testName = $"{Path.GetFileName(test.FilePath)}-{test.Ordinal:D2}-{HashUtility.GetMd5HashShort(test.Content)}";
            var basePath = Path.GetFullPath(Path.Combine(spec.Temp ? Path.GetTempPath() : "docfx-tests", testName));
            var outputPath = Path.GetFullPath(Path.Combine(basePath, "outputs"));
            var markerPath = Path.Combine(basePath, "marker");
            var appDataPath = Path.Combine(basePath, "appdata");
            var cachePath = Path.Combine(appDataPath, "cache");
            var statePath = Path.Combine(appDataPath, "state");

            var variables = new Dictionary<string, string>
            {
                { "APP_BASE_PATH", AppContext.BaseDirectory },
                { "OUTPUT_PATH", outputPath },
                { "CACHE_PATH", cachePath },
                { "STATE_PATH", statePath },
                { "DOCS_GITHUB_TOKEN", Environment.GetEnvironmentVariable("DOCS_GITHUB_TOKEN") },
                { "DOCS_OPS_TOKEN", Environment.GetEnvironmentVariable("DOCS_OPS_TOKEN") },
                { "MICROSOFT_GRAPH_CLIENT_SECRET", Environment.GetEnvironmentVariable("MICROSOFT_GRAPH_CLIENT_SECRET") },
            };

            var missingVariables = spec.Environments.Where(env => !variables.TryGetValue(env, out var value) || string.IsNullOrEmpty(value));
            if (missingVariables.Any())
            {
                throw new TestSkippedException($"Missing variable {string.Join(',', missingVariables)}");
            }

            Directory.CreateDirectory(basePath);

            var repos = spec.Repos
                .Select(repo => new PackagePath(repo.Key).Url)
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
                    var packageUrl = new PackagePath(url);
                    TestUtility.CreateGitRepository(repos[packageUrl.Url], commits, packageUrl.Url, packageUrl.Branch, variables);
                }

                TestUtility.CreateFiles(docsetPath, spec.Inputs, variables);
                TestUtility.CreateFiles(cachePath, spec.Cache, variables);
                TestUtility.CreateFiles(statePath, spec.State, variables);

                File.WriteAllText(markerPath, "");
            }

            return (docsetPath, appDataPath, outputPath, repos);
        }

        private static void RunCore(string docsetPath, string outputPath, TestData test, DocfxTestSpec spec)
        {
            if (spec.Locale != null)
            {
                // always build from localization docset for localization tests
                // https://dev.azure.com/ceapex/Engineering/_build/results?buildId=97101&view=logs&j=133bd042-0fac-58b5-e6e7-01018e6dc4d4&t=b907bda6-23f1-5af4-47fe-b951a88dbb9a&l=10898
                var locDocsetPath = t_repos.Value.FirstOrDefault(
                    repo => repo.Key.EndsWith($".{spec.Locale}") || repo.Key.EndsWith(".loc")).Value;

                if (locDocsetPath != null)
                {
                    RunBuild(locDocsetPath, outputPath, test, spec);
                }
            }
            else
            {
                RunBuild(docsetPath, outputPath, test, spec);
            }
        }

        private static void RunBuild(string docsetPath, string outputPath, TestData test, DocfxTestSpec spec)
        {
            var dryRun = test.Metrix.Contains("DryRun");
            var randomOutputPath = Path.ChangeExtension(outputPath, $".{Guid.NewGuid()}");

            docsetPath = Path.Combine(docsetPath, spec.Cwd ?? "");

            using (TestUtility.EnsureFilesNotChanged(docsetPath, spec.SkipInputCheck))
            {
                var commandLine = new[]
                {
                    "build", docsetPath,
                    "--output", randomOutputPath,
                    dryRun ? "--dry-run" : null,
                    spec.Legacy ? "--legacy" : null,
                    spec.NoRestore ? "--no-restore" : null,
                };

                Docfx.Run(commandLine.Where(arg => arg != null).ToArray());
            }

            // Ensure --dry-run doesn't produce artifacts, but produces the same error log as normal build
            var outputs = dryRun
                ? new Dictionary<string, string> { [".errors.log"] = spec.Outputs[".errors.log"] }
                : spec.Outputs;

            VerifyOutput(randomOutputPath, outputs);

            Directory.Delete(randomOutputPath, recursive: true);
        }

        private static void VerifyOutput(string outputPath, Dictionary<string, string> outputs)
        {
            var expectedOutputs = JObject.FromObject(outputs);

            // Ensure no .errors.log file if there is no error
            if (!outputs.ContainsKey(".errors.log"))
            {
                expectedOutputs[".errors.log"] = JValue.CreateUndefined();
            }

            var actualOutputs = Directory.Exists(outputPath)
                ? Directory
                    .GetFiles(outputPath, "*", SearchOption.AllDirectories)
                    .ToDictionary(file => Path.GetRelativePath(outputPath, file).Replace('\\', '/'), File.ReadAllText)
                : new Dictionary<string, string>();

            s_jsonDiff.Verify(expectedOutputs, actualOutputs);
        }

        private static JsonDiff CreateJsonDiff()
        {
            var fileJsonDiff = new JsonDiffBuilder()
                .UseAdditionalProperties()
                .UseNegate()
                .UseWildcard()
                .UseHtml(IsHtml)
                .Use(IsHtml, RemoveDataLinkType)
                .Build();

            return new JsonDiffBuilder()
                .UseAdditionalProperties(null, IsRequiredOutput)
                .UseIgnoreNull()
                .UseJson(null, fileJsonDiff)
                .UseLogFile(fileJsonDiff)
                .UseHtml(IsHtml)
                .Use(IsHtml, RemoveDataLinkType)
                .Build();
        }

        private static bool IsRequiredOutput(string name)
        {
            var fileName = Path.GetFileName(name);

            return (fileName == ".errors.log" || !fileName.StartsWith("."))
                && fileName != "filemap.json"
                && fileName != "op_aggregated_file_map_info.json"
                && fileName != "full-dependent-list.txt"
                && fileName != "server-side-dependent-list.txt";
        }

        private static bool IsHtml(JToken expected, JToken actual, string name)
        {
            if (name.EndsWith(".html", StringComparison.OrdinalIgnoreCase) && name.Length > ".html".Length)
            {
                return true;
            }

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
            if (!expectedHtml.Contains("data-linktype") && !string.IsNullOrEmpty(actualHtml))
            {
                actualHtml = Regex.Replace(actualHtml, " data-linktype=\".*?\"", "");
            }
            return (expectedHtml, actualHtml);
        }

        private static JsonDiffBuilder UseLogFile(this JsonDiffBuilder builder, JsonDiff jsonDiff)
        {
            return builder.Use(
                (expected, actual, name) => name.EndsWith(".txt") || name.EndsWith(".log"),
                (expected, actual, name, _) =>
                {
                    if (expected.Type != JTokenType.String || actual.Type != JTokenType.String)
                    {
                        return (expected, actual);
                    }

                    var expectedLines = expected.Value<string>()
                        .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                        .OrderBy(item => item)
                        .ToArray();

                    var actualLines = actual.Value<string>()
                        .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                        .OrderBy(item => item)
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
