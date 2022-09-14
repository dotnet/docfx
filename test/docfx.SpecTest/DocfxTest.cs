// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Yunit;

namespace Microsoft.Docs.Build;

public static class DocfxTest
{
    private static readonly JsonDiff s_jsonDiff = CreateJsonDiff();
    private static readonly ConcurrentDictionary<string, object> s_locks = new();
    private static readonly AsyncLocal<IReadOnlyDictionary<string, string>> s_repos = new();
    private static readonly AsyncLocal<IReadOnlyDictionary<string, string>> s_remoteFiles = new();
    private static readonly AsyncLocal<string> s_appDataPath = new();
    private static readonly AsyncLocal<DocsEnvironment?> s_buildEnvironment = new();

    static DocfxTest()
    {
        TestQuirks.BuildEnvironment = () => s_buildEnvironment.Value;

        TestQuirks.AppDataPath = () => s_appDataPath.Value;

        TestQuirks.GitRemoteProxy = remote =>
        {
            var mockedRepos = s_repos.Value;
            if (mockedRepos != null && mockedRepos.TryGetValue(remote, out var mockedLocation))
            {
                return mockedLocation;
            }
            return remote;
        };

        TestQuirks.HttpProxy = remote =>
        {
            var mockedRemoteFiles = s_remoteFiles.Value;
            if (mockedRemoteFiles != null && mockedRemoteFiles.TryGetValue(remote, out var mockedContent))
            {
                return mockedContent;
            }
            return null;
        };

        TestQuirks.OpsGetAccessTokenProxy = url =>
        {
            if (url == null)
            {
                return Environment.GetEnvironmentVariable("DOCS_GITHUB_TOKEN") ?? string.Empty;
            }
            var mockedRemoteFiles = s_remoteFiles.Value;
            if (mockedRemoteFiles != null && mockedRemoteFiles.Values.Contains(url))
            {
                return string.Empty;
            }
            return null;
        };
    }

    public static IEnumerable<string> ExpandTest(DocfxTestSpec spec)
    {
        yield return "";

        var hasError = spec.Outputs.ContainsKey(".errors.log");
        if (hasError && !spec.DryRunOnly && !spec.NoDryRun)
        {
            yield return "DryRun";
        }

        if (hasError && !spec.NoSingleFile && !spec.BuildFiles.Any() && spec.Inputs.Keys.Count(file => IsContentFile(file)) > 1)
        {
            yield return "SingleFile";
        }

        if (InputContainsText(spec, "outputType: pageJson"))
        {
            yield return "ContinueBuild";
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
            var (docsetPath, appDataPath, outputPath, repos, package) = CreateDocset(test, spec);

            try
            {
                s_buildEnvironment.Value = Enum.TryParse(spec.BuildEnvironment, out DocsEnvironment env) ? env : DocsEnvironment.PPE;
                s_repos.Value = repos;
                s_remoteFiles.Value = spec.Http;
                s_appDataPath.Value = appDataPath;
                RunCore(docsetPath, outputPath, test, spec, package);
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
                s_buildEnvironment.Value = null;
                s_repos.Value = null;
                s_remoteFiles.Value = null;
                s_appDataPath.Value = null;
            }
        }
    }

    private static bool InputContainsText(DocfxTestSpec spec, string text)
    {
        if (Contains(spec.Inputs, text))
        {
            return true;
        }

        return spec.Repos.Values
            .SelectMany(item => item)
            .Any(item => Contains(item.Files, text));

        static bool Contains(IDictionary<string, string> dict, string text)
            => dict.Values.Any(value => value is string str && str.Contains(text, StringComparison.OrdinalIgnoreCase));
    }

    private static (string docsetPath, string appDataPath, string outputPath, Dictionary<string, string> repos, Package package)
        CreateDocset(TestData test, DocfxTestSpec spec)
    {
        var testName = $"{Path.GetFileName(test.FilePath)}-{test.Ordinal:D2}-{HashUtility.GetSha256HashShort(test.Content)}";
        var basePath = Path.GetFullPath(Path.Combine(spec.Temp ? Path.GetTempPath() : "docfx-tests", testName));
        var outputPath = Path.GetFullPath(Path.Combine(basePath, "outputs"));
        var markerPath = Path.Combine(basePath, "marker");
        var appDataPath = Path.Combine(basePath, "appdata");
        var cachePath = Path.Combine(appDataPath, "cache");
        var statePath = Path.Combine(appDataPath, "state");

        Directory.CreateDirectory(basePath);

        var repos = spec.Repos
            .Select(repo => new PackagePath(repo.Key).Url)
            .Distinct()
            .Select((remote, index) => (remote, index))
            .ToDictionary(
                remote => remote.remote,
                remote => Path.Combine(basePath, "repos", remote.index.ToString()));

        var docsetPath = repos.Select(item => item.Value).FirstOrDefault() ?? Path.Combine(basePath, "inputs");

        var variables = new Dictionary<string, string>
            {
                { "APP_BASE_PATH", AppContext.BaseDirectory },
                { "OUTPUT_PATH", outputPath },
                { "CACHE_PATH", cachePath },
                { "STATE_PATH", statePath },
                { "DOCS_GITHUB_TOKEN", Environment.GetEnvironmentVariable("DOCS_GITHUB_TOKEN") },
                { "DOCS_OPS_TOKEN", Environment.GetEnvironmentVariable("DOCS_OPS_TOKEN") },
                { "MICROSOFT_GRAPH_CLIENT_CERTIFICATE", Environment.GetEnvironmentVariable("MICROSOFT_GRAPH_CLIENT_CERTIFICATE") },
                { "GIT_TOKEN_HTTP_AUTH_SSO_DISABLED", Environment.GetEnvironmentVariable("GIT_TOKEN_HTTP_AUTH_SSO_DISABLED") },
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
            if (spec.Repos.Count == 0 && !GitUtility.IsGitRepository(docsetPath))
            {
                GitUtility.Init(docsetPath);
            }
            if (package is LocalPackage)
            {
                TestUtility.CreateFiles(docsetPath, spec.Inputs, variables);
            }

            File.WriteAllText(markerPath, "");
        }

        return (docsetPath, appDataPath, outputPath, repos, package);
    }

    private static void RunCore(string docsetPath, string outputPath, TestData test, DocfxTestSpec spec, Package package)
    {
        var singleFile = test.Matrix.Contains("SingleFile");
        var dryRun = spec.DryRunOnly || test.Matrix.Contains("DryRun") || singleFile;
        var isContinue = test.Matrix.Contains("ContinueBuild");

        if (spec.LanguageServer.Count != 0)
        {
            RunLanguageServer(docsetPath, spec, package).GetAwaiter().GetResult();
        }
        else if (spec.Locale != null)
        {
            // always build from localization docset for localization tests
            // https://dev.azure.com/ceapex/Engineering/_build/results?buildId=97101&view=logs&j=133bd042-0fac-58b5-e6e7-01018e6dc4d4&t=b907bda6-23f1-5af4-47fe-b951a88dbb9a&l=10898
            var locDocsetPath = s_repos.Value.FirstOrDefault(
                repo => repo.Key.EndsWith($".{spec.Locale}") || repo.Key.EndsWith(".loc")).Value;

            if (locDocsetPath != null)
            {
                RunBuild(locDocsetPath, outputPath, dryRun, singleFile, isContinue, spec, package.CreateSubPackage(locDocsetPath));
            }
        }
        else
        {
            RunBuild(docsetPath, outputPath, dryRun, singleFile, isContinue, spec, package);
        }
    }

    private static async Task RunLanguageServer(string docsetPath, DocfxTestSpec spec, Package package)
    {
        await using var client = new LanguageServerTestClient(docsetPath, package, spec.NoCache);

        foreach (var command in spec.LanguageServer)
        {
            await client.ProcessCommand(command);
        }
    }

    private static void RunBuild(string docsetPath, string outputPath, bool dryRun, bool singleFile, bool isContinue, DocfxTestSpec spec, Package package)
    {
        var randomOutputPath = Path.ChangeExtension(outputPath, $".{Guid.NewGuid()}");

        docsetPath = Path.Combine(docsetPath, spec.Cwd ?? "");

        using (TestUtility.EnsureFilesNotChanged(docsetPath, spec.NoInputCheck))
        {
            if (singleFile)
            {
                RunSingleFileBuild(docsetPath, randomOutputPath, spec, package);
            }
            else if (isContinue)
            {
                var randomJsonOutputPath = Path.ChangeExtension(outputPath, $".intermediate.{Guid.NewGuid()}");
                var jsonCommandLine = new[]
                {
                    "build", docsetPath,
                    "--output", randomJsonOutputPath,
                    "--log", Path.Combine(randomJsonOutputPath, ".errors.log"),
                    "--output-type", "json",
                    spec.NoRestore ? "--no-restore" : null,
                    spec.NoCache ? "--no-cache" : null,
                    spec.NoDrySync ? "--no-dry-sync" : null,
                };

                Docfx.Run(jsonCommandLine.Where(arg => arg != null).ToArray(), package);

                var continueCommandLine = new[]
                {
                    "build", randomJsonOutputPath,
                    "--output", randomOutputPath,
                    "--log", Path.Combine(randomOutputPath, ".errors.log"),
                    "--continue",
                    "--locale", spec.Locale ?? "en-us",
                    "--template", GetTemplatePath(package, s_repos.Value),
                    "--output-type", "pageJson",
                    spec.NoRestore ? "--no-restore" : null,
                    spec.NoCache ? "--no-cache" : null,
                    spec.NoDrySync ? "--no-dry-sync" : null,
                };
                RemoveUnnecessaryFilesForContinue(randomJsonOutputPath);
                Docfx.Run(continueCommandLine.Where(arg => arg != null).ToArray(), package);

                if (Directory.Exists(randomJsonOutputPath))
                {
                    Directory.Delete(randomJsonOutputPath, recursive: true);
                }
            }
            else
            {
                var commandLine = new[]
                {
                    "build", docsetPath,
                    "--output", randomOutputPath,
                    "--log", Path.Combine(randomOutputPath, ".errors.log"),
                    dryRun ? "--dry-run" : null,
                    spec.NoRestore ? "--no-restore" : null,
                    spec.NoCache ? "--no-cache" : null,
                    spec.NoDrySync ? "--no-dry-sync" : null,
                }.Concat(spec.BuildFiles.SelectMany(file => new[] { "--file", Path.Combine(docsetPath, file) }));

                Docfx.Run(commandLine.Where(arg => arg != null).ToArray(), package);
            }
        }

        // Ensure --dry-run doesn't produce artifacts, but produces the same error log as normal build
        var outputs = dryRun && spec.Outputs.TryGetValue(".errors.log", out var errors)
            ? new Dictionary<string, string> { [".errors.log"] = errors }
            : isContinue
              ? (from kvp in spec.Outputs
                 where !Path.GetFileName(kvp.Key).StartsWith(".") && IsRequiredOutput(kvp.Key) && kvp.Key.EndsWith(".json") && !kvp.Key.EndsWith("hierarchy.json")
                 select kvp).ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
              : spec.Outputs;

        VerifyOutput(randomOutputPath, outputs);

        if (Directory.Exists(randomOutputPath))
        {
            Directory.Delete(randomOutputPath, recursive: true);
        }
    }

    private static string GetTemplatePath(Package package, IReadOnlyDictionary<string, string> repos)
    {
        // The location of Templates has 3 options:
        // 1. _themes folder under the "docset" folder
        // 2. A Template repo folder under the "repos" folder of the spec test.
        // 3. If no template specified, then provide a placeholder or the ApplyTemplates of Docfx will throw an exception.
        if (Directory.Exists(Path.Combine(package.BasePath, "_themes")))
        {
            return Path.Combine(package.BasePath, "_themes");
        }

        foreach (var (url, path) in repos)
        {
            if (url.Contains("theme") && Directory.GetDirectories(path).Contains(Path.Combine(path, "ContentTemplate")))
            {
                return path;
            }
        }

        return "_themesPlaceholder";
    }

    private static void RemoveUnnecessaryFilesForContinue(string path)
    {
        // TODO: no need to clean-up if glob more strictly
        foreach (var filePath in Directory.GetFiles(path, "*.*", SearchOption.AllDirectories))
        {
            if (Path.GetFileName(filePath).StartsWith(".") || !IsRequiredOutput(filePath) || !filePath.EndsWith(".json"))
            {
                File.Delete(filePath);
            }
        }
    }

    private static void RunSingleFileBuild(string docsetPath, string outputPath, DocfxTestSpec spec, Package package)
    {
        // Single file build builds each content file and verifies the union of
        // each file build result is the same as expected output. Implies dryRun.
        var commandLine = new CommandLineOptions
        {
            Directory = docsetPath,
            Output = outputPath,
            DryRun = true,
            NoRestore = spec.NoRestore,
            NoDrySync = spec.NoDrySync,
        };

        var errors = new ErrorList();
        var builder = new Builder(commandLine, package);

        foreach (var (file, _) in spec.Inputs)
        {
            if (IsContentFile(file))
            {
                builder.Build(errors, new Progress<string>(), new[] { file });
            }
        }

        Directory.CreateDirectory(outputPath);

        File.WriteAllLines(
            Path.Combine(outputPath, ".errors.log"),
            errors.ToArray().Select(error => error with { MessageArguments = null }).Distinct().Select(error => error.ToString()));
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

    private static bool IsContentFile(string path)
    {
        var name = Path.GetFileNameWithoutExtension(path);

        if (name.Equals("docfx", PathUtility.PathComparison) ||
            name.Equals("redirections", PathUtility.PathComparison) ||
            name.StartsWith("."))
        {
            return false;
        }

        return path.EndsWith(".md", PathUtility.PathComparison) ||
               path.EndsWith(".json", PathUtility.PathComparison) ||
               path.EndsWith(".yml", PathUtility.PathComparison);
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
