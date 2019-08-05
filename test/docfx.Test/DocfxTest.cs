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
using Microsoft.AspNetCore.TestHost;
using Microsoft.DocAsTest;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.Docs.Build
{
    public static class DocfxTest
    {
        private static readonly Dictionary<string, string> s_variables = new Dictionary<string, string>
        {
            { "APP_BASE_PATH", AppContext.BaseDirectory },
            { "DOCS_GITHUB_TOKEN", Environment.GetEnvironmentVariable("DOCS_GITHUB_TOKEN") },
            { "MICROSOFT_GRAPH_CLIENT_SECRET", Environment.GetEnvironmentVariable("MICROSOFT_GRAPH_CLIENT_SECRET") },
        };

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

        private static readonly AsyncLocal<IReadOnlyDictionary<string, string>> t_repos = new AsyncLocal<IReadOnlyDictionary<string, string>>();
        private static readonly AsyncLocal<string> t_cachePath = new AsyncLocal<string>();

        static DocfxTest()
        {
            Environment.SetEnvironmentVariable("DOCFX_APPDATA_PATH", Path.GetFullPath("appdata"));
            Environment.SetEnvironmentVariable("DOCFX_GLOBAL_CONFIG_PATH", Path.GetFullPath("docfx.test.yml"));

            Log.ForceVerbose = true;

            AppData.GetCachePath = () => t_cachePath.Value;
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

        [YamlTest("~/docs/specs")]
        public static async Task Run(TestData test, DocfxTestSpec spec)
        {
            var (docsetPath, cachePath, outputPath, repos) = CreateDocset(test, spec);

            try
            {
                t_repos.Value = repos;
                t_cachePath.Value = cachePath;

                if (OsMatches(spec.OS))
                {
                    await RunCore(docsetPath, outputPath, spec);
                }
                else
                {
                    await Assert.ThrowsAnyAsync<XunitException>(() => RunCore(docsetPath, outputPath, spec));
                }
            }
            finally
            {
                t_repos.Value = null;
                t_cachePath.Value = null;
            }
        }

        private static (string docsetPath, string cachePath, string outputPath, Dictionary<string, string> repos)
            CreateDocset(TestData test, DocfxTestSpec spec)
        {
            var missingVariables = spec.Environments.Where(env => !s_variables.TryGetValue(env, out var value) || string.IsNullOrEmpty(value));
            if (missingVariables.Any())
            {
                throw new TestSkippedException($"Missing variable {string.Join(',', missingVariables)}");
            }

            var testName = $"{Path.GetFileName(test.FilePath)}-{test.Ordinal:D2}-{HashUtility.GetMd5HashShort(test.Content)}";
            var basePath = Path.GetFullPath(Path.Combine("docfx-test", testName));

            Directory.CreateDirectory(basePath);

            var repos = spec.Repos
                .Select(repo => UrlUtility.SplitGitUrl(repo.Key).remote)
                .Distinct()
                .Select((remote, index) => (remote, index))
                .ToDictionary(
                    remote => remote.remote,
                    remote => Path.Combine(basePath, "repos", remote.index.ToString()));

            var docsetPath = repos.Select(item => item.Value).FirstOrDefault() ?? Path.Combine(basePath, "inputs");
            var outputPath = Path.GetFullPath(Path.Combine(basePath, "outputs"));
            var cachePath = Path.Combine(basePath, "cache");
            var markerPath = Path.Combine(basePath, "marker");

            if (!File.Exists(markerPath))
            {
                foreach (var (url, commits) in spec.Repos.Reverse())
                {
                    var (remote, branch, _) = UrlUtility.SplitGitUrl(url);
                    TestUtility.CreateGitRepository(repos[remote], commits, remote, branch, s_variables);
                }

                TestUtility.CreateFiles(docsetPath, spec.Inputs, s_variables);
                TestUtility.CreateFiles(cachePath, spec.Cache, s_variables);

                File.WriteAllText(markerPath, "");
            }
            
            return (docsetPath, cachePath, outputPath, repos);
        }

        private async static Task RunCore(string docsetPath, string outputPath, DocfxTestSpec spec)
        {
            if (spec.Watch)
            {
                await RunWatch(docsetPath, spec);
                return;
            }

            await RunBuild(docsetPath, outputPath, spec, spec.Locale);

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

            var options = $"{(spec.Legacy ? "--legacy" : "")} {(locale != null ? $"--locale {locale}" : "")}"
                .Split(' ', StringSplitOptions.RemoveEmptyEntries).ToArray();

            if (spec.Restore)
            {
                await Docfx.Run(new[] { "restore", docsetPath, "--output", outputPath }.Concat(options).ToArray());
            }
            if (spec.Build)
            {
                await Docfx.Run(new[] { "build", docsetPath, "--output", outputPath }.Concat(options).ToArray());
            }

            // Verify output
            Assert.True(Directory.Exists(outputPath), $"{outputPath} does not exist");

            var outputs = Directory.GetFiles(outputPath, "*", SearchOption.AllDirectories);
            var outputFileNames = outputs.Select(file => file.Substring(outputPath.Length + 1).Replace('\\', '/')).ToList();

            // Show .errors.log content if actual output has errors or warnings.
            if (outputFileNames.Contains(".errors.log"))
            {
                Console.WriteLine($"{Path.GetFileName(docsetPath)}: {File.ReadAllText(Path.Combine(outputPath, ".errors.log"))}");
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
                VerifyFile(Path.GetFullPath(Path.Combine(outputPath, filename)), content);
            }
        }

        private static async Task RunWatch(string docsetPath, DocfxTestSpec spec)
        {
            using (var server = new TestServer(Watch.CreateWebServer(docsetPath, new CommandLineOptions())))
            {
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
                    TestUtility.AssertHtmlEquals(content, File.ReadAllText(file));
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

        private static bool OsMatches(string os)
        {
            return string.IsNullOrEmpty(os) ||
                os.Split(',').Any(
                    item => RuntimeInformation.IsOSPlatform(OSPlatform.Create(item.Trim().ToUpperInvariant())));
        }
    }
}
