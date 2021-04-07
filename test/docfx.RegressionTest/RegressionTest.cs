// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommandLine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal static class RegressionTest
    {
        private const string TestDataRepositoryUrl = "https://dev.azure.com/ceapex/Engineering/_git/docfx.TestData";
        private const string TestDiskRoot = "D:/";

        private static readonly string s_testDataRoot = Path.Join(TestDiskRoot, "docfx.TestData");
        private static readonly string? s_githubToken = Environment.GetEnvironmentVariable("DOCS_GITHUB_TOKEN");
        private static readonly string? s_azureDevopsToken = Environment.GetEnvironmentVariable("AZURE_DEVOPS_TOKEN");
        private static readonly string? s_buildReason = Environment.GetEnvironmentVariable("BUILD_REASON");
        private static readonly string s_gitCmdAuth = GetGitCommandLineAuthorization();
        private static readonly bool s_isPullRequest = s_buildReason == null || s_buildReason == "PullRequest";
        private static readonly string s_commitString = typeof(Docfx).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? throw new InvalidOperationException();

        private static string s_repositoryName = "";
        private static string s_repository = "";

        private static int Main(string[] args)
        {
            if (args.Length >= 1 && args[0].Equals("warm-up"))
            {
                Console.WriteLine($"warm up starting...");

                var opt = string.Join(" ", args[1..]);

                if (string.IsNullOrEmpty(opt))
                {
                    throw new InvalidDataException();
                }

                try
                {
                    var option = Parser.Default.ParseArguments<Options>($"{opt}".Split()).MapResult(WarmUpAgents, _ => { return -9999; });
                }
                catch
                {
                    Console.WriteLine($"Clone failed: https:{opt}");
                }

                return 0;
            }
            else
            {
                Console.WriteLine("run regression test starting...");
                return Parser.Default.ParseArguments<Options>(args).MapResult(
                    Run,
                    _ =>
                    {
                        SendPullRequestComments(new() { CrashMessage = "regression-test argument exception" });
                        return -9999;
                    });
            }
        }

        private static int WarmUpAgents(Options opts)
        {
            try
            {
                s_repository = opts.Repository;
                s_repositoryName = $"{(opts.DryRun ? "dryrun." : "")}{Path.GetFileName(opts.Repository)}";
                var workingFolder = Path.Combine(s_testDataRoot, $"regression-test.{s_repositoryName}");

                Console.WriteLine($"Downloading {s_repository} with branch {opts.Branch}");
                EnsureTestData(opts, workingFolder);
                Console.WriteLine($"{s_repository} with branch {opts.Branch} is finished!");
            }
            catch
            {
                throw;
            }

            return 0;
        }

        private static int Run(Options opts)
        {
            try
            {
                s_repository = opts.Repository;
                s_repositoryName = $"{(opts.DryRun ? "dryrun." : "")}{Path.GetFileName(opts.Repository)}";
                var workingFolder = Path.Combine(s_testDataRoot, $"regression-test.{s_repositoryName}");

                var remoteBranch = EnsureTestData(opts, workingFolder);
                Test(opts, workingFolder, remoteBranch);
            }
            catch (Exception ex)
            {
                SendPullRequestComments(new() { CrashMessage = ex.ToString() });
                throw;
            }
            return 0;
        }

        private static (string baseLinePath, string outputPath, string repositoryPath, string docfxConfig) Prepare(Options opts, string workingFolder, string remoteBranch)
        {
            var repositoryPath = Path.Combine(workingFolder, s_repositoryName);
            var cachePath = Path.Combine(workingFolder, "cache");
            var statePath = Path.Combine(workingFolder, "state");

            Console.BackgroundColor = ConsoleColor.DarkMagenta;
            Console.WriteLine($"Testing {s_repositoryName}");
            Console.ResetColor();

            var baseLinePath = Path.Combine(workingFolder, "output");
            Directory.CreateDirectory(baseLinePath);
            var outputPath = s_isPullRequest ? Path.Combine(workingFolder, ".temp") : baseLinePath;

            // Set Env for Build
            Environment.SetEnvironmentVariable("DOCFX_REPOSITORY_URL", opts.Repository);
            Environment.SetEnvironmentVariable("DOCFX_REPOSITORY_BRANCH", remoteBranch);
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
                    http,
                    maxFileWarnings = 10000,
                    maxFileSuggestions = 10000,
                    maxFileInfos = 10000,
                    updateTimeAsCommitBuildTime = true,
                    githubToken = s_githubToken,
                    githubUserCacheExpirationInHours = s_isPullRequest ? 24 * 365 : 24 * 30,
                });

                if (opts.OutputHtml)
                {
                    docfxConfig["outputType"] = "html";
                    docfxConfig["urlType"] = "ugly";
                    docfxConfig["template"] = "https://github.com/Microsoft/templates.docs.msft.pdf#master";
                }
                else
                {
                    docfxConfig["outputType"] = "pageJson";
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

        private static bool Test(Options opts, string workingFolder, string remoteBranch)
        {
            var testResult = new RegressionTestResult();
            var (baseLinePath, outputPath, repositoryPath, docfxConfig) = Prepare(opts, workingFolder, remoteBranch);

            Clean(outputPath);
            Build(testResult, repositoryPath, outputPath, opts, docfxConfig);

            Compare(testResult, opts, workingFolder, outputPath, baseLinePath);
            Console.BackgroundColor = ConsoleColor.DarkMagenta;
            Console.WriteLine($"Test Pass {workingFolder}");
            Console.ResetColor();

            PushChanges(testResult, workingFolder);
            return true;
        }

        private static string EnsureTestData(Options opts, string workingFolder, int retryCount = 0)
        {
            if (!Directory.Exists(workingFolder))
            {
                try
                {
                    Directory.CreateDirectory(workingFolder);
                    Exec("git", $"init", cwd: workingFolder);
                    Exec("git", $"remote add origin {TestDataRepositoryUrl}", cwd: workingFolder);
                    Exec("git", $"{s_gitCmdAuth} fetch origin --progress template", cwd: workingFolder, secrets: s_gitCmdAuth);
                }
                catch
                {
                    if (retryCount++ < 3)
                    {
                        _ = Task.Delay(5000);
                        EnsureTestData(opts, workingFolder, retryCount);
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }
                }
            }

            var remoteBranch = string.IsNullOrEmpty(opts.Branch)
                ? GetRemoteDefaultBranch(opts.Repository, workingFolder)
                : opts.Branch;

            try
            {
                Exec("git", $"{s_gitCmdAuth} fetch origin --progress --prune {s_repositoryName}", cwd: workingFolder, secrets: s_gitCmdAuth, redirectStandardError: true);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains($"couldn't find remote ref {s_repositoryName}"))
                {
                    // A new repo is added for the first time
                    Exec("git", $"checkout -B {s_repositoryName} origin/template", cwd: workingFolder);
                    Exec("git", $"clean -xdff", cwd: workingFolder);
                    Exec("git", $"{s_gitCmdAuth} submodule add -f --branch {remoteBranch} {opts.Repository} {s_repositoryName}", cwd: workingFolder, secrets: s_gitCmdAuth);
                    return remoteBranch;
                }
                throw;
            }

            Exec("git", $"checkout --force origin/{s_repositoryName}", cwd: workingFolder);
            Exec("git", $"clean -xdff", cwd: workingFolder);

            var submoduleUpdateFlags = s_isPullRequest ? "" : "--remote";
            Exec("git", $"{s_gitCmdAuth} submodule set-branch -b {remoteBranch} {s_repositoryName}", cwd: workingFolder, secrets: s_gitCmdAuth);
            Exec("git", $"{s_gitCmdAuth} submodule sync {s_repositoryName}", cwd: workingFolder, secrets: s_gitCmdAuth);
            Exec("git", $"{s_gitCmdAuth} submodule update {submoduleUpdateFlags} --init --progress --force {s_repositoryName}", cwd: workingFolder, secrets: s_gitCmdAuth);
            Exec("git", $"clean -xdf", cwd: Path.Combine(workingFolder, s_repositoryName));
            return remoteBranch;
        }

        private static string GetRemoteDefaultBranch(string repositoryUrl, string workingDirectory)
        {
            var remoteInfo = ProcessUtility.Execute("git", $"{s_gitCmdAuth} remote show {repositoryUrl}", workingDirectory, secret: s_gitCmdAuth);
            var match = Regex.Match(remoteInfo, "^([\\s\\S]*)\\sHEAD branch: (.*)$");
            if (match.Success)
            {
                return match.Groups[2].Value;
            }
            throw new InvalidOperationException("Default remote branch not found!");
        }

        private static void Clean(string outputPath)
        {
            if (Directory.Exists(outputPath))
            {
                Directory.Delete(outputPath, recursive: true);
                Directory.CreateDirectory(outputPath);
            }
        }

        private static void Build(RegressionTestResult testResult, string repositoryPath, string outputPath, Options opts, string docfxConfig)
        {
            var dryRunOption = opts.DryRun ? "--dry-run" : "";
            var templateOption = opts.PublicTemplate ? "--template https://static.docs.com/ui/latest" : "";
            var noDrySyncOption = opts.NoDrySync ? "--no-dry-sync" : "";
            var logOption = $"--log \"{Path.Combine(outputPath, ".errors.log")}\"";
            var diagnosticPort = $"docfx-regression-test-{Guid.NewGuid()}.sock";
            var traceFile = Path.Combine(s_testDataRoot, $".temp/{s_repositoryName}.nettrace");

            Exec(
                Path.Combine(AppContext.BaseDirectory, "docfx.exe"),
                arguments: $"restore {logOption} {templateOption} --verbose --stdin",
                stdin: docfxConfig,
                cwd: repositoryPath);

            var profiler = opts.Profile
                ? Process.Start("dotnet-trace", $"collect --providers Microsoft-DotNETCore-SampleProfiler --diagnostic-port {diagnosticPort} --output \"{traceFile}\"")
                : null;

            testResult.BuildTime = Exec(
                Path.Combine(AppContext.BaseDirectory, "docfx.exe"),
                arguments: $"build -o \"{outputPath}\" {logOption} {templateOption} {dryRunOption} {noDrySyncOption} --verbose --no-restore --stdin",
                stdin: docfxConfig,
                cwd: repositoryPath,
                allowExitCodes: new[] { 0, 1 },
                env: profiler != null ? new() { ["DOTNET_DiagnosticPorts"] = diagnosticPort } : null);

            if (profiler != null)
            {
                profiler.WaitForExit();

                Console.WriteLine($"##vso[artifact.upload artifactname=trace;]{traceFile}");

                var speedScopeFile = Path.Combine(s_testDataRoot, $".temp/{s_repositoryName}.speedscope.json");
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

            if (testResult.BuildTime.TotalSeconds > opts.Timeout && s_isPullRequest)
            {
                Console.WriteLine($"##vso[task.complete result=Failed]Test failed, build timeout. Repo: {s_repositoryName}; Expected Runtime: {opts.Timeout}s");
                Console.WriteLine($"Test failed, build timeout. Repo: {s_repositoryName}; Expected Runtime: {opts.Timeout}s");
            }

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

                var diffFile = Path.Combine(s_testDataRoot, $".temp/{s_repositoryName}.patch");

                Directory.CreateDirectory(Path.GetDirectoryName(diffFile) ?? ".");
                var (diff, totalLines) = PipeOutputToFile(process.StandardOutput, diffFile, maxLines: 100000);
                process.WaitForExit();

                testResult.Succeeded = process.ExitCode == 0;
                testResult.Timeout = opts.Timeout;
                testResult.Diff = diff;
                testResult.MoreLines = totalLines;

                watch.Stop();
                Console.WriteLine($"'git diff' done in '{watch.Elapsed}'");

                if (process.ExitCode == 0)
                {
                    return;
                }

                Console.WriteLine($"##vso[artifact.upload artifactname=diff;]{diffFile}");
                Console.WriteLine($"##vso[task.complete result=Failed]Test failed, see the logs under /Summary/Build artifacts for details");
            }
            else
            {
                Exec("git", "-c core.autocrlf=input -c core.safecrlf=false add -A", cwd: workingFolder);
                Exec("git", $"-c user.name=\"docfx-impact-ci\" -c user.email=\"docfx-impact-ci@microsoft.com\" commit -m \"**DISABLE_SECRET_SCANNING** {s_repositoryName}: {s_commitString}\"", cwd: workingFolder, ignoreError: true);
            }
        }

        private static void PushChanges(RegressionTestResult testResult, string workingFolder)
        {
            if (s_isPullRequest)
            {
                SendPullRequestComments(testResult);
            }
            else
            {
                Exec("git", $"{s_gitCmdAuth} push origin HEAD:{s_repositoryName}", cwd: workingFolder, secrets: s_gitCmdAuth);
            }
        }

        private static TimeSpan Exec(
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

            var stderr = redirectStandardError ? process.StandardError.ReadToEnd() : default;
            process.WaitForExit();

            if (!allowExitCodes.Contains(process.ExitCode) && !ignoreError)
            {
                throw new InvalidOperationException(
                    $"'\"{fileName}\" {sanitizedArguments}' failed in directory '{cwd}' with exit code {process.ExitCode}, message: \n {stderr}");
            }

            stopwatch.Stop();
            Console.WriteLine($"'{fileName} {sanitizedArguments}' done in '{stopwatch.Elapsed}'");
            return stopwatch.Elapsed;
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
            body.Append($"<a href='{s_repository}'>{s_repositoryName}</a>");
            body.Append($"({testResult.BuildTime}");

            if (isTimeout)
            {
                body.Append($" | exceed {testResult.Timeout}s");
            }
            if (testResult.MoreLines > 0)
            {
                body.Append($", {testResult.MoreLines} more diff");
            }

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
    }
}
