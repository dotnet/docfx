// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Yunit;

namespace Microsoft.Docs.Build
{
    public static class DocfxTest
    {
        private static readonly JsonDiff s_jsonDiff = CreateJsonDiff();
        private static readonly JsonDiff s_languageServerJsonDiff = CreateLanguageServerJsonDiff();
        private static readonly ConcurrentDictionary<string, object> s_locks = new();
        private static readonly AsyncLocal<IReadOnlyDictionary<string, string>> t_repos = new();
        private static readonly AsyncLocal<IReadOnlyDictionary<string, string>> t_remoteFiles = new();
        private static readonly AsyncLocal<string> t_appDataPath = new();
        private static readonly AsyncLocal<StrongBox<int>> t_finishedBuildCount = new();

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

            TestQuirks.HttpProxy = remote =>
            {
                var mockedRemoteFiles = t_remoteFiles.Value;
                if (mockedRemoteFiles != null && mockedRemoteFiles.TryGetValue(remote, out var mockedContent))
                {
                    return mockedContent;
                }
                return null;
            };

            TestQuirks.FinishedBuildCountIncrease = () =>
            {
                t_finishedBuildCount.Value.Value++;
            };
        }

        public static IEnumerable<string> ExpandTest(DocfxTestSpec spec)
        {
            yield return "";

            if (!spec.DryRunOnly && !spec.NoDryRun && spec.Outputs.ContainsKey(".errors.log"))
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

            lock (s_locks.GetOrAdd($"{test.FilePath}-{test.Ordinal:D2}", _ => new object()))
            {
                var (docsetPath, appDataPath, outputPath, repos, variables, package) = CreateDocset(test, spec);

                try
                {
                    t_repos.Value = repos;
                    t_remoteFiles.Value = spec.Http;
                    t_appDataPath.Value = appDataPath;
                    t_finishedBuildCount.Value = new StrongBox<int>();
                    RunCore(docsetPath, outputPath, test, spec, variables, package);
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
                    t_remoteFiles.Value = null;
                    t_appDataPath.Value = null;
                    t_finishedBuildCount.Value = null;
                }
            }
        }

        private static (string docsetPath, string appDataPath, string outputPath, Dictionary<string, string> repos, Dictionary<string, string> variables, Package package)
            CreateDocset(TestData test, DocfxTestSpec spec)
        {
            var testName = $"{Path.GetFileName(test.FilePath)}-{test.Ordinal:D2}-{HashUtility.GetMd5HashShort(test.Content)}";
            var basePath = NormalizePath(Path.GetFullPath(Path.Combine(spec.Temp ? Path.GetTempPath() : "docfx-tests", testName)));
            var outputPath = NormalizePath(Path.GetFullPath(Path.Combine(basePath, "outputs")));
            var markerPath = NormalizePath(Path.Combine(basePath, "marker"));
            var appDataPath = NormalizePath(Path.Combine(basePath, "appdata"));
            var cachePath = NormalizePath(Path.Combine(appDataPath, "cache"));
            var statePath = NormalizePath(Path.Combine(appDataPath, "state"));

            Directory.CreateDirectory(basePath);

            var repos = spec.Repos
                .Select(repo => new PackagePath(repo.Key).Url)
                .Distinct()
                .Select((remote, index) => (remote, index))
                .ToDictionary(
                    remote => remote.remote,
                    remote => Path.Combine(basePath, "repos", remote.index.ToString()));

            var docsetPath = NormalizePath(repos.Select(item => item.Value).FirstOrDefault() ?? Path.Combine(basePath, "inputs"));

            var variables = new Dictionary<string, string>
            {
                { "APP_BASE_PATH", AppContext.BaseDirectory },
                { "DOCSET_PATH", docsetPath },
                { "OUTPUT_PATH", outputPath },
                { "CACHE_PATH", cachePath },
                { "STATE_PATH", statePath },
                { "DOCS_GITHUB_TOKEN", Environment.GetEnvironmentVariable("DOCS_GITHUB_TOKEN") },
                { "MICROSOFT_GRAPH_CLIENT_SECRET", Environment.GetEnvironmentVariable("MICROSOFT_GRAPH_CLIENT_SECRET") },
                { "GIT_TOKEN_HTTP_AUTH_SSO_DISABLED", Environment.GetEnvironmentVariable("GIT_TOKEN_HTTP_AUTH_SSO_DISABLED") },
                { "GIT_TOKEN_HTTP_AUTH_INSUFFICIENT_PERMISSION", Environment.GetEnvironmentVariable("GIT_TOKEN_HTTP_AUTH_INSUFFICIENT_PERMISSION") },
            };

            var missingVariables = spec.Environments.Where(env => !variables.TryGetValue(env, out var value) || string.IsNullOrEmpty(value));
            if (missingVariables.Any())
            {
                throw new TestSkippedException($"Missing variable {string.Join(',', missingVariables)}");
            }

            var package = TestUtility.CreateInputDirectoryPackage(docsetPath, spec, variables);

            if (!File.Exists(markerPath))
            {
                foreach (var (url, commits) in spec.Repos.Reverse())
                {
                    var packageUrl = new PackagePath(url);
                    TestUtility.CreateGitRepository(repos[packageUrl.Url], commits, packageUrl.Url, packageUrl.Branch, variables);
                }

                TestUtility.CreateFiles(cachePath, spec.Cache, variables);
                TestUtility.CreateFiles(statePath, spec.State, variables);
                if (package is LocalPackage)
                {
                    TestUtility.CreateFiles(docsetPath, spec.Inputs, variables);
                }

                File.WriteAllText(markerPath, "");
            }

            return (docsetPath, appDataPath, outputPath, repos, variables, package);
        }

        private static void RunCore(string docsetPath, string outputPath, TestData test, DocfxTestSpec spec, Dictionary<string, string> variables, Package package)
        {
            var dryRun = spec.DryRunOnly || test.Matrix.Contains("DryRun");

            if (spec.LanguageServer.Count != 0)
            {
                RunLanguageServer(docsetPath, spec, package, variables).GetAwaiter().GetResult();
            }
            else if (spec.Locale != null)
            {
                // always build from localization docset for localization tests
                // https://dev.azure.com/ceapex/Engineering/_build/results?buildId=97101&view=logs&j=133bd042-0fac-58b5-e6e7-01018e6dc4d4&t=b907bda6-23f1-5af4-47fe-b951a88dbb9a&l=10898
                var locDocsetPath = t_repos.Value.FirstOrDefault(
                    repo => repo.Key.EndsWith($".{spec.Locale}") || repo.Key.EndsWith(".loc")).Value;

                if (locDocsetPath != null)
                {
                    RunBuild(locDocsetPath, outputPath, dryRun, spec, package.CreateSubPackage(locDocsetPath));
                }
            }
            else
            {
                RunBuild(docsetPath, outputPath, dryRun, spec, package);
            }
        }

        private static async Task RunLanguageServer(string docsetPath, DocfxTestSpec spec, Package package, Dictionary<string, string> variables)
        {
            var lspTestHost = new LanguageServerTestHost(docsetPath, variables, package);

            for (var i = 0; i < spec.LanguageServer.Count; i++)
            {
                var lspSpec = spec.LanguageServer[i];
                if (!string.IsNullOrEmpty(lspSpec.Notification))
                {
                    await lspTestHost.SendNotification(new LanguageServerNotification(lspSpec.Notification, lspSpec.Params));
                }
                else if (!string.IsNullOrEmpty(lspSpec.ExpectNotification))
                {
                    var expectedNotifications = new List<LanguageServerNotification>();
                    var expectedMethods = new HashSet<string>();
                    while (true)
                    {
                        lspSpec = spec.LanguageServer[i];
                        expectedNotifications.Add(new LanguageServerNotification(lspSpec.ExpectNotification, TestUtility.ApplyVariables(lspSpec.Params, variables)));
                        expectedMethods.Add(lspSpec.ExpectNotification);

                        if ((i + 1) >= spec.LanguageServer.Count || string.IsNullOrEmpty(spec.LanguageServer[i + 1].ExpectNotification))
                        {
                            break;
                        }

                        i++;
                    }

                    var actualNotifications = await lspTestHost.GetExpectedNotification(
                        (method) => expectedMethods.Contains(method, StringComparer.OrdinalIgnoreCase),
                        expectedNotifications.Count);

                    if (expectedNotifications.Count > 1)
                    {
                        expectedNotifications = expectedNotifications.OrderBy(item => item.Params.ToString()).ToList();
                        actualNotifications = actualNotifications.OrderBy(item => item.Params.ToString()).ToList();
                    }

                    s_languageServerJsonDiff.Verify(expectedNotifications, actualNotifications);
                }
                else if (lspSpec.ExpectNoNotificationAfterBuildTime != null)
                {
                    while (lspSpec.ExpectNoNotificationAfterBuildTime > t_finishedBuildCount.Value.Value)
                    {
                        await Task.Delay(1000);
                    }
                    var actualNotifications = await lspTestHost.GetExpectedNotification(expectedCount: 1, timeout: 100);
                    s_languageServerJsonDiff.Verify(new List<LanguageServerNotification>(), actualNotifications);
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
        }

        private static void RunBuild(string docsetPath, string outputPath, bool dryRun, DocfxTestSpec spec, Package package)
        {
            var randomOutputPath = Path.ChangeExtension(outputPath, $".{Guid.NewGuid()}");

            docsetPath = Path.Combine(docsetPath, spec.Cwd ?? "");

            using (TestUtility.EnsureFilesNotChanged(docsetPath, spec.NoInputCheck))
            {
                var commandLine = new[]
                {
                    "build", docsetPath,
                    "--output", randomOutputPath,
                    "--log", Path.Combine(randomOutputPath, ".errors.log"),
                    dryRun ? "--dry-run" : null,
                    spec.NoRestore ? "--no-restore" : null,
                    spec.NoDrySync ? "--no-dry-sync" : null,
                }.Concat(spec.BuildFiles.SelectMany(file => new[] { "--file", Path.Combine(docsetPath, file) }));

                Docfx.Run(commandLine.Where(arg => arg != null).ToArray(), package);
            }

            // Ensure --dry-run doesn't produce artifacts, but produces the same error log as normal build
            var outputs = dryRun && spec.Outputs.TryGetValue(".errors.log", out var errors)
                ? new Dictionary<string, string> { [".errors.log"] = errors }
                : spec.Outputs;

            VerifyOutput(randomOutputPath, outputs);

            if (Directory.Exists(randomOutputPath))
            {
                Directory.Delete(randomOutputPath, recursive: true);
            }
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

        private static JsonDiff CreateLanguageServerJsonDiff()
        {
            return new JsonDiffBuilder()
                .UseAdditionalProperties()
                .UseNegate()
                .UseWildcard()
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

        private static string NormalizePath(string path)
        {
            var normalizedPath = PathUtility.Normalize(path);
            if (!PathUtility.IsCaseSensitive)
            {
                normalizedPath = normalizedPath.ToLower();
            }
            return normalizedPath;
        }
    }
}
