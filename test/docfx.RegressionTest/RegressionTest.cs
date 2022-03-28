// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Reflection;
using System.Text;
using CommandLine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Polly;

namespace Microsoft.Docs.Build;

internal static class RegressionTest
{
    private const string TestDataRepositoryUrl = "https://dev.azure.com/ceapex/Engineering/_git/docfx.TestData";
    private const string TestDiskRoot = "D:/";

    private static readonly string s_testDataRoot = Path.Join(TestDiskRoot, "docfx.TestData");
    private static readonly string? s_githubToken = Environment.GetEnvironmentVariable("DOCS_GITHUB_TOKEN");
    private static readonly string? s_azureDevopsToken = Environment.GetEnvironmentVariable("AZURE_DEVOPS_TOKEN");
    private static readonly string? s_buildReason = Environment.GetEnvironmentVariable("BUILD_REASON");
    private static readonly string? s_microsoftGraphClientCertificate = Environment.GetEnvironmentVariable("MICROSOFT_GRAPH_CLIENT_CERTIFICATE");
    private static readonly string s_gitCmdAuth = GetGitCommandLineAuthorization();
    private static readonly bool s_isPullRequest = s_buildReason == null || s_buildReason == "PullRequest";
    private static readonly string s_commitString = typeof(Docfx).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? throw new InvalidOperationException();

    private static string s_testName = "";
    private static string s_repository = "";

    private static int Main(string[] args)
    {
        return Parser.Default.ParseArguments<Options>(args).MapResult(
            Run,
            _ =>
            {
                SendPullRequestComments(new() { CrashMessage = "argument exception" });
                return -9999;
            });
    }

    private static int Run(Options opts)
    {
        try
        {
            s_repository = opts.Repository;

            s_testName = opts.DryRun
                ? $"dryrun.{Path.GetFileName(opts.Repository)}"
                : opts.OutputType.Equals("html", StringComparison.OrdinalIgnoreCase)
                    ? $"htmlTest.{Path.GetFileName(opts.Repository)}"
                    : Path.GetFileName(opts.Repository);

            var workingFolder = Path.Combine(s_testDataRoot, $"regression-test.{s_testName}");

            EnsureTestData(opts, workingFolder);
            if (opts.WarmUp)
            {
                var (_, outputPath, repositoryPath, docfxConfig) = Prepare(opts, workingFolder);
                RestoreDependency(repositoryPath, docfxConfig, outputPath);
            }
            else
            {
                Test(opts, workingFolder);
            }
        }
        catch (Exception ex)
        {
            SendPullRequestComments(new() { CrashMessage = ex.ToString() });
            throw;
        }
        return 0;
    }

    private static (string baseLinePath, string outputPath, string repositoryPath, string docfxConfig) Prepare(Options opts, string workingFolder)
    {
        var repositoryPath = Path.Combine(workingFolder, s_testName);
        var cachePath = Path.Combine(workingFolder, "cache");
        var statePath = Path.Combine(workingFolder, "state");

        Console.BackgroundColor = ConsoleColor.DarkMagenta;
        Console.WriteLine($"Testing {s_testName}");
        Console.ResetColor();

        var baseLinePath = Path.Combine(workingFolder, "output");
        Directory.CreateDirectory(baseLinePath);
        var outputPath = s_isPullRequest ? Path.Combine(workingFolder, ".temp") : baseLinePath;

        // Set Env for Build
        Environment.SetEnvironmentVariable("DOCFX_REPOSITORY_URL", opts.Repository);
        Environment.SetEnvironmentVariable("DOCFX_REPOSITORY_BRANCH", opts.Branch);
        Environment.SetEnvironmentVariable("DOCFX_LOCALE", opts.Locale);
        Environment.SetEnvironmentVariable("DOCFX_STATE_PATH", statePath);
        Environment.SetEnvironmentVariable("DOCFX_CACHE_PATH", cachePath);

        return (baseLinePath, outputPath, repositoryPath, GetDocfxConfig(opts));

        static string GetDocfxConfig(Options opts)
        {
            var http = new Dictionary<string, object>();
            if (!string.IsNullOrEmpty(s_githubToken))
            {
                http["https://github.com"] = new { headers = ToAuthHeader(s_githubToken) };
            }

            if (!string.IsNullOrEmpty(s_azureDevopsToken))
            {
                http["https://dev.azure.com"] = new { headers = ToAuthHeader(s_azureDevopsToken) };
            }

            var docfxConfig = JObject.FromObject(new
            {
                outputType = opts.OutputType,
                maxFileWarnings = 10000,
                maxFileSuggestions = 10000,
                maxFileInfos = 10000,
                updateTimeAsCommitBuildTime = true,
                githubUserCacheExpirationInHours = s_isPullRequest ? 24 * 365 : 24 * 30,
                microsoftGraphClientId = "b799e059-9dd8-4839-a39c-96f7531e55e2",
                secrets = new
                {
                    http,
                    githubToken = s_githubToken,
                    microsoftGraphClientCertificate = s_microsoftGraphClientCertificate,
                },
            });

            if (!string.IsNullOrEmpty(opts.Template))
            {
                docfxConfig["template"] = opts.Template;
            }

            if (opts.OutputType == "html")
            {
                docfxConfig["urlType"] = "ugly";
            }
            else
            {
                docfxConfig["selfContained"] = false;
            }

            if (opts.RegressionRules)
            {
                docfxConfig["markdownValidationRules"] = "https://ops/regressionallcontentrules/";
                docfxConfig["buildValidationRules"] = "https://ops/regressionallbuildrules/";
                docfxConfig["sandboxEnabledModuleList"] = "https://ops/sandboxEnabledModuleList/";
                docfxConfig["metadataSchema"] = new JArray()
                    {
                        Path.Combine(AppContext.BaseDirectory, "data/docs/metadata.json"),
                        "https://ops/regressionallmetadataschema/",
                    };
                docfxConfig["allowlists"] = "https://ops/regressionalltaxonomy-allowlists/";
            }

            return JsonConvert.SerializeObject(docfxConfig);
        }

        static Dictionary<string, string> ToAuthHeader(string? token)
        {
            return new Dictionary<string, string>
                {
                    { "authorization", $"basic {BasicAuth(token)}" },
                };
        }
    }

    private static bool Test(Options opts, string workingFolder)
    {
        var testResult = new RegressionTestResult();
        var (baseLinePath, outputPath, repositoryPath, docfxConfig) = Prepare(opts, workingFolder);

        Clean(outputPath);
        Build(testResult, repositoryPath, outputPath, opts, docfxConfig);

        Compare(testResult, opts, workingFolder, outputPath, baseLinePath);
        Console.BackgroundColor = ConsoleColor.DarkMagenta;
        Console.WriteLine($"Test {(testResult.Succeeded ? "Pass" : "Fail")} {workingFolder}");
        Console.WriteLine("Test Result Summary:");
        Console.WriteLine($"Succeeded = {testResult.Succeeded}, " +
            $"BuildTime = {testResult.BuildTime.TotalSeconds}s, " +
            $"Timeout = {testResult.Timeout}s, " +
            $"PeakMemory = {testResult.PeakMemory}, " +
            $"Diff = {(testResult.Diff?.Length > 0 ? "Yes" : "No")}" +
            $"MoreLines = {testResult.MoreLines}, " +
            $"CrashMessage = {testResult.CrashMessage}");
        Console.ResetColor();

        PushChanges(testResult, workingFolder);
        return true;
    }

    private static void EnsureTestData(Options opts, string workingFolder)
    {
        if (!Directory.Exists(workingFolder))
        {
            Directory.CreateDirectory(workingFolder);
            Exec("git", $"init", cwd: workingFolder);
            Exec("git", $"remote add origin {TestDataRepositoryUrl}", cwd: workingFolder);

            Retry(() => Exec("git", $"{s_gitCmdAuth} fetch origin --progress template", cwd: workingFolder, secrets: s_gitCmdAuth));
        }

        try
        {
            Retry(() => Exec(
                        "git",
                        $"{s_gitCmdAuth} fetch origin --progress --prune {s_testName}",
                        cwd: workingFolder,
                        secrets: s_gitCmdAuth,
                        redirectStandardError: true));
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains($"couldn't find remote ref {s_testName}"))
            {
                // A new repo is added for the first time
                Exec("git", $"checkout -B {s_testName} origin/template", cwd: workingFolder);
                Exec("git", $"clean -xdff", cwd: workingFolder);
                Exec("git", $"{s_gitCmdAuth} submodule add -f --branch {opts.Branch} {opts.Repository} {s_testName}", cwd: workingFolder, secrets: s_gitCmdAuth);
                return;
            }
            throw;
        }

        Exec("git", $"checkout --force origin/{s_testName}", cwd: workingFolder);
        Exec("git", $"clean -xdff", cwd: workingFolder);

        var submoduleUpdateFlags = s_isPullRequest ? "" : "--remote";
        Exec("git", $"{s_gitCmdAuth} submodule set-branch -b {opts.Branch} {s_testName}", cwd: workingFolder, secrets: s_gitCmdAuth);
        Exec("git", $"{s_gitCmdAuth} submodule sync {s_testName}", cwd: workingFolder, secrets: s_gitCmdAuth);
        Exec("git", $"{s_gitCmdAuth} submodule update {submoduleUpdateFlags} --init --progress --force {s_testName}", cwd: workingFolder, secrets: s_gitCmdAuth);
        Exec("git", $"clean -xdf", cwd: Path.Combine(workingFolder, s_testName));
    }

    private static void Clean(string outputPath)
    {
        if (Directory.Exists(outputPath))
        {
            Directory.Delete(outputPath, recursive: true);
            Directory.CreateDirectory(outputPath);
        }
    }

    private static void RestoreDependency(string repositoryPath, string docfxConfig, string outputPath)
    {
        var logOption = $"--log \"{Path.Combine(outputPath, ".errors.log")}\"";
        Exec(
            Path.Combine(AppContext.BaseDirectory, "docfx.exe"),
            arguments: $"restore {logOption} --verbose --stdin",
            stdin: docfxConfig,
            cwd: repositoryPath);
    }

    private static void Build(RegressionTestResult testResult, string repositoryPath, string outputPath, Options opts, string docfxConfig)
    {
        var dryRunOption = opts.DryRun ? "--dry-run" : "";
        var noDrySyncOption = opts.NoDrySync ? "--no-dry-sync" : "";
        var logOption = $"--log \"{Path.Combine(outputPath, ".errors.log")}\"";
        var diagnosticPort = $"docfx-regression-test-{Guid.NewGuid()}.sock";
        var traceFile = Path.Combine(s_testDataRoot, $".temp/{s_testName}.nettrace");

        RestoreDependency(repositoryPath, docfxConfig, outputPath);

        Directory.CreateDirectory(Path.GetDirectoryName(traceFile) ?? ".");
        var profiler = opts.Profile
            ? Process.Start("dotnet-trace", $"collect --providers Microsoft-DotNETCore-SampleProfiler --diagnostic-port {diagnosticPort} --output \"{traceFile}\"")
            : null;

        (testResult.BuildTime, testResult.PeakMemory) = Exec(
            Path.Combine(AppContext.BaseDirectory, "docfx.exe"),
            arguments: $"build -o \"{outputPath}\" {logOption} {dryRunOption} {noDrySyncOption} --verbose --no-restore --stdin",
            stdin: docfxConfig,
            cwd: repositoryPath,
            allowExitCodes: new[] { 0, 1 },
            env: profiler != null ? new() { ["DOTNET_DiagnosticPorts"] = diagnosticPort } : null);

        Console.WriteLine($"docfx peak memory usage: {testResult.PeakMemory / 1000 / 1000}MB");

        if (profiler != null)
        {
            profiler.WaitForExit();

            Console.WriteLine($"##vso[artifact.upload artifactname=trace;]{traceFile}");

            var speedScopeFile = Path.Combine(s_testDataRoot, $".temp/{s_testName}.speedscope.json");
            Process.Start("dotnet-trace", $"convert --format Speedscope \"{traceFile}\"").WaitForExit();

            testResult.HotMethods = string.Join('\n', SpeedScope.FindHotMethods(speedScopeFile).Select(item => $"{item.percentage,3}% | {item.method}"));

            Console.WriteLine();
            Console.WriteLine("Performance Sampling Result");
            Console.WriteLine("---------------------------");
            Console.WriteLine(testResult.HotMethods);
        }
    }

    private static void Compare(RegressionTestResult testResult, Options opts, string workingFolder, string outputPath, string existingOutputPath)
    {
        // For temporary normalize: use 'NormalizeJsonFiles' for output files
        Normalizer.Normalize(outputPath, NormalizeStage.PrettifyJsonFiles | NormalizeStage.PrettifyLogFiles, errorLevel: opts.ErrorLevel);

        if (s_isPullRequest)
        {
            var watch = Stopwatch.StartNew();

            // For temporary normalize: uncomment below line
            // Normalizer.Normalize(existingOutputPath, NormalizeStage.NormalizeJsonFiles);
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = "git",
                Arguments = $"--no-pager -c core.longpaths=true -c core.autocrlf=input -c core.safecrlf=false diff --no-index --ignore-all-space --ignore-blank-lines --ignore-cr-at-eol --exit-code \"{existingOutputPath}\" \"{outputPath}\"",
                WorkingDirectory = TestDiskRoot, // starting `git diff` from root makes it faster
                RedirectStandardOutput = true,
            }) ?? throw new InvalidOperationException();

            var diffFile = Path.Combine(s_testDataRoot, $".temp/{s_testName}.patch");

            Directory.CreateDirectory(Path.GetDirectoryName(diffFile) ?? ".");
            var (diff, totalLines) = PipeOutputToFile(process.StandardOutput, diffFile, maxLines: 100000);
            process.WaitForExit();

            // refer to https://git-scm.com/docs/git-diff#Documentation/git-diff.txt---exit-code
            var noDiff = process.ExitCode == 0;

            testResult.Timeout = opts.Timeout;
            testResult.Diff = diff;
            testResult.MoreLines = totalLines;

            watch.Stop();
            Console.WriteLine($"'git diff' done in '{watch.Elapsed}'");

            if (!noDiff)
            {
                Console.WriteLine($"##vso[artifact.upload artifactname=diff;]{diffFile}");
                MarkTaskFailed("Test failed, see the logs under /Summary/Build artifacts for details");
            }

            var isTimeout = testResult.BuildTime.TotalSeconds > opts.Timeout;
            if (isTimeout)
            {
                MarkTaskFailed($"Test failed, build timeout. Repo: {s_testName}; Expected Runtime: {opts.Timeout}s");
                Console.WriteLine($"Test failed, build timeout. Repo: {s_testName}; Expected Runtime: {opts.Timeout}s");
            }

            testResult.Succeeded = noDiff && !isTimeout;
        }
        else
        {
            Exec("git", "-c core.autocrlf=input -c core.safecrlf=false add -A", cwd: workingFolder);
            Exec("git", $"-c user.name=\"docfx-impact-ci\" -c user.email=\"docfx-impact-ci@microsoft.com\" commit -m \"**DISABLE_SECRET_SCANNING** {s_testName}: {s_commitString}\"", cwd: workingFolder, ignoreError: true);
        }
    }

    private static void MarkTaskFailed(string comment)
    {
        Console.WriteLine($"##vso[task.complete result=Failed]{comment}");
    }

    private static void PushChanges(RegressionTestResult testResult, string workingFolder)
    {
        if (s_isPullRequest)
        {
            SendPullRequestComments(testResult);
        }
        else
        {
            Exec("git", $"{s_gitCmdAuth} push origin HEAD:{s_testName}", cwd: workingFolder, secrets: s_gitCmdAuth);
        }
    }

    private static (TimeSpan time, long peakMemory) Exec(
        string fileName,
        string arguments = "",
        string? stdin = null,
        string? cwd = null,
        Dictionary<string, string>? env = null,
        bool ignoreError = false,
        bool redirectStandardError = false,
        int[]? allowExitCodes = null,
        params string[] secrets)
    {
        var stopwatch = Stopwatch.StartNew();
        var sanitizedArguments = secrets.Aggregate(arguments, (arg, secret) => string.IsNullOrWhiteSpace(secret) ? arg : arg.Replace(secret, "***"));
        allowExitCodes ??= new int[] { 0 };

        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine($"{fileName} {sanitizedArguments}");
        Console.ResetColor();

        if (fileName == "git")
        {
            arguments = $"-c core.longpaths=true {arguments}";
        }

        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = cwd ?? ".",
            UseShellExecute = false,
            RedirectStandardError = redirectStandardError,
            RedirectStandardInput = !string.IsNullOrEmpty(stdin),
        };

        if (env != null)
        {
            foreach (var (key, value) in env)
            {
                psi.EnvironmentVariables[key] = value;
            }
        }

        var process = Process.Start(psi) ?? throw new InvalidOperationException();

        if (!string.IsNullOrEmpty(stdin))
        {
            process.StandardInput.Write(stdin);
            process.StandardInput.Close();
        }

        var memoryWatcher = WatchPeakMemoryUsage(process);
        var stderr = redirectStandardError ? process.StandardError.ReadToEnd() : default;
        process.WaitForExit();

        if (!allowExitCodes.Contains(process.ExitCode) && !ignoreError)
        {
            throw new InvalidOperationException(
                $"'\"{fileName}\" {sanitizedArguments}' failed in directory '{cwd}' with exit code {process.ExitCode}, message: \n {stderr}");
        }

        stopwatch.Stop();
        Console.WriteLine($"'{fileName} {sanitizedArguments}' done in '{stopwatch.Elapsed}'");
        return (stopwatch.Elapsed, memoryWatcher.Result);

        static async Task<long> WatchPeakMemoryUsage(Process process)
        {
            var peakWorkingSet = 0L;
            var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
            while (await timer.WaitForNextTickAsync() && !process.HasExited)
            {
                process.Refresh();
                peakWorkingSet = Math.Max(peakWorkingSet, process.PeakWorkingSet64);
            }
            return peakWorkingSet;
        }
    }

    private static (string, int) PipeOutputToFile(StreamReader reader, string path, int maxLines)
    {
        var maxSummaryLines = 200;
        var totalLines = 0;
        var result = new StringBuilder();

        using (var output = File.CreateText(path))
        {
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                if (totalLines++ < maxLines)
                {
                    output.WriteLine(line);
                    if (totalLines <= maxSummaryLines)
                    {
                        result.AppendLine(line);
                    }
                }
            }
        }

        return (result.ToString(), Math.Max(0, totalLines - maxSummaryLines));
    }

    private static string GetGitCommandLineAuthorization()
    {
        var azureReposBasicAuth = string.IsNullOrEmpty(s_azureDevopsToken)
            ? "" : $"-c http.https://dev.azure.com.extraheader=\"AUTHORIZATION: basic {BasicAuth(s_azureDevopsToken)}\"";

        var githubBasicAuth = string.IsNullOrEmpty(s_githubToken)
            ? "" : $"-c http.https://github.com.extraheader=\"AUTHORIZATION: basic {BasicAuth(s_githubToken)}\"";

        return $"{azureReposBasicAuth} {githubBasicAuth}";
    }

    private static string BasicAuth(string? token)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes($"user:{token}"));
    }

    private static void SendPullRequestComments(RegressionTestResult testResult)
    {
        var isTimeout = testResult.BuildTime.TotalSeconds > testResult.Timeout;
        if (testResult.Succeeded && !isTimeout && testResult.CrashMessage == null)
        {
            return;
        }

        var body = new StringBuilder();
        body.Append("<details><summary>");
        body.Append(testResult.CrashMessage != null ? "🚗🌳💥🤕🚑" : isTimeout ? "🧭" : "⚠");
        body.Append($"<a href='{s_repository}'>{s_testName}</a>");
        body.Append($"({testResult.BuildTime.TotalSeconds}s");

        if (isTimeout)
        {
            body.Append($" | exceed {testResult.Timeout}s");
        }
        if (testResult.MoreLines > 0)
        {
            body.Append($", {testResult.MoreLines} more diff");
        }

        body.Append($", {testResult.PeakMemory / 1000 / 1000}MB");
        body.Append(")</summary>\n\n");

        if (!string.IsNullOrEmpty(testResult.CrashMessage))
        {
            body.Append($"```\n{testResult.CrashMessage}\n\n```");
        }

        if (!string.IsNullOrEmpty(testResult.Diff))
        {
            body.Append($"```diff\n{testResult.Diff}\n\n```");
        }

        if (isTimeout && !string.IsNullOrEmpty(testResult.HotMethods))
        {
            body.Append($"```csharp\n{testResult.HotMethods}\n\n```");
        }

        body.Append("\n\n</details>");

        if (int.TryParse(Environment.GetEnvironmentVariable("PULL_REQUEST_NUMBER") ?? "", out var prNumber))
        {
            SendGitHubPullRequestComments(prNumber, body.ToString()).GetAwaiter().GetResult();
        }
    }

    private static async Task SendGitHubPullRequestComments(int prNumber, string body)
    {
        using var http = new HttpClient(new HttpClientHandler { CheckCertificateRevocationList = true });
        http.DefaultRequestHeaders.Add("User-Agent", "DocFX");
        http.DefaultRequestHeaders.Add("Authorization", $"bearer {s_githubToken}");

        using var content = new StringContent(JsonConvert.SerializeObject(new { body }), Encoding.UTF8, "application/json");
        var response = await http.PostAsync(
            new Uri($"https://api.github.com/repos/dotnet/docfx/issues/{prNumber}/comments"),
            content);

        response.EnsureSuccessStatusCode();
    }

    private static T Retry<T>(Func<T> action, int retryCount = 5)
        => Policy
        .Handle<Exception>()
        .WaitAndRetry(retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)))
        .Execute(action);
}
