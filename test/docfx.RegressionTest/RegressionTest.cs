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

        private static (bool succeeded, TimeSpan buildTime, int? timeout, string diff, int moreLines) s_testResult;
        private static string s_repositoryName = "";
        private static string s_repository = "";

        private static int Main(string[] args)
        {
            return Parser.Default.ParseArguments<Options>(args).MapResult(
                Run,
                _ =>
                {
                    SendPullRequestComments("regression-test argument exception");
                    return -9999;
                });
        }

        private static int Run(Options opts)
        {
            try
            {
                s_repository = opts.Repository;
                s_repositoryName = $"{(opts.DryRun ? "dryrun." : "")}{Path.GetFileName(opts.Repository)}";
                var workingFolder = Path.Combine(s_testDataRoot, $"regression-test.{s_repositoryName}");

                EnsureTestData(opts, workingFolder);
                Test(opts, workingFolder);
                PushChanges(workingFolder);
            }
            catch (Exception ex)
            {
                SendPullRequestComments(ex.ToString());
                throw;
            }
            return 0;
        }

        private static (string baseLinePath, string outputPath, string repositoryPath, string docfxConfig) Prepare(Options opts, string workingFolder)
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
            Environment.SetEnvironmentVariable("DOCFX_REPOSITORY_BRANCH", opts.Branch);
            Environment.SetEnvironmentVariable("DOCFX_LOCALE", opts.Locale);
            Environment.SetEnvironmentVariable("DOCFX_STATE_PATH", statePath);
            Environment.SetEnvironmentVariable("DOCFX_CACHE_PATH", cachePath);

            return (baseLinePath, outputPath, repositoryPath, GetDocfxConfig(opts));

            static string GetDocfxConfig(Options opts)
            {
                var docfxConfig = JObject.FromObject(new
                {
                    http = new Dictionary<string, object>
                    {
                        ["https://github.com"] = new { headers = ToAuthHeader(s_githubToken) },
                        ["https://dev.azure.com"] = new { headers = ToAuthHeader(s_azureDevopsToken) },
                    },
                    maxFileWarnings = 1000,
                    maxFileSuggestions = 1000,
                    maxFileInfos = 1000,
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
                    docfxConfig["metadataSchema"] = new JArray()
                    {
                        Path.Combine(AppContext.BaseDirectory, "data/schemas/OpsMetadata.json"),
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
            var (baseLinePath, outputPath, repositoryPath, docfxConfig) = Prepare(opts, workingFolder);

            Clean(outputPath);
            var buildTime = Build(repositoryPath, outputPath, opts, docfxConfig);

            Compare(opts, workingFolder, outputPath, baseLinePath, buildTime);
            Console.BackgroundColor = ConsoleColor.DarkMagenta;
            Console.WriteLine($"Test Pass {workingFolder}");
            Console.ResetColor();
            return true;
        }

        private static void EnsureTestData(Options opts, string workingFolder)
        {
            if (!Directory.Exists(workingFolder))
            {
                Directory.CreateDirectory(workingFolder);
                Exec("git", $"init", cwd: workingFolder);
                Exec("git", $"remote add origin {TestDataRepositoryUrl}", cwd: workingFolder);
                Exec("git", $"{s_gitCmdAuth} fetch origin --progress template", cwd: workingFolder, secrets: s_gitCmdAuth);
            }

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
                    Exec("git", $"{s_gitCmdAuth} -c core.longpaths=true submodule add -f --branch {opts.Branch} {opts.Repository} {s_repositoryName}", cwd: workingFolder, secrets: s_gitCmdAuth);
                    return;
                }
                throw;
            }

            Exec("git", $"-c core.longpaths=true checkout --force origin/{s_repositoryName}", cwd: workingFolder);
            Exec("git", $"clean -xdff", cwd: workingFolder);

            var submoduleUpdateFlags = s_isPullRequest ? "" : "--remote";
            Exec("git", $"{s_gitCmdAuth} submodule set-branch -b {opts.Branch} {s_repositoryName}", cwd: workingFolder, secrets: s_gitCmdAuth);
            Exec("git", $"{s_gitCmdAuth} submodule sync {s_repositoryName}", cwd: workingFolder, secrets: s_gitCmdAuth);
            Exec("git", $"{s_gitCmdAuth} -c core.longpaths=true submodule update {submoduleUpdateFlags} --init --progress --force {s_repositoryName}", cwd: workingFolder, secrets: s_gitCmdAuth);
            Exec("git", $"clean -xdf", cwd: Path.Combine(workingFolder, s_repositoryName));
        }

        private static void Clean(string outputPath)
        {
            if (Directory.Exists(outputPath))
            {
                Directory.Delete(outputPath, recursive: true);
                Directory.CreateDirectory(outputPath);
            }
        }

        private static TimeSpan Build(string repositoryPath, string outputPath, Options opts, string docfxConfig)
        {
            var dryRunOption = opts.DryRun ? "--dry-run" : "";
            var templateOption = opts.PublicTemplate ? "--template https://static.docs.com/ui/latest" : "";
            var noDrySyncOption = opts.NoDrySync ? "--no-dry-sync" : "";
            var logOption = $"--log \"{Path.Combine(outputPath, ".errors.log")}\"";

            Exec(
                Path.Combine(AppContext.BaseDirectory, "docfx.exe"),
                arguments: $"restore {logOption} {templateOption} --verbose --stdin",
                stdin: docfxConfig,
                cwd: repositoryPath,
                allowExitCodes: new int[] { 0 });

            return Exec(
                Path.Combine(AppContext.BaseDirectory, "docfx.exe"),
                arguments: $"build -o \"{outputPath}\" {logOption} {templateOption} {dryRunOption} {noDrySyncOption} --verbose --no-restore --stdin",
                stdin: docfxConfig,
                cwd: repositoryPath);
        }

        private static void Compare(Options opts, string workingFolder, string outputPath, string existingOutputPath, TimeSpan buildTime)
        {
            // For temporary normalize: use 'NormalizeJsonFiles' for output files
            Normalizer.Normalize(outputPath, NormalizeStage.PrettifyJsonFiles | NormalizeStage.PrettifyLogFiles, errorLevel: opts.ErrorLevel);

            if (buildTime.TotalSeconds > opts.Timeout && s_isPullRequest)
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
                    Arguments = $"--no-pager -c core.autocrlf=input -c core.safecrlf=false -c core.longpaths=true diff --no-index --ignore-all-space --ignore-blank-lines --ignore-cr-at-eol --exit-code \"{existingOutputPath}\" \"{outputPath}\"",
                    WorkingDirectory = TestDiskRoot, // starting `git diff` from root makes it faster
                    RedirectStandardOutput = true,
                }) ?? throw new InvalidOperationException();

                var diffFile = Path.Combine(s_testDataRoot, $".temp/{s_repositoryName}.patch");

                Directory.CreateDirectory(Path.GetDirectoryName(diffFile) ?? ".");
                var (diff, totalLines) = PipeOutputToFile(process.StandardOutput, diffFile, maxLines: 100000);
                process.WaitForExit();

                s_testResult = (process.ExitCode == 0, buildTime, opts.Timeout, diff, totalLines);
                watch.Stop();
                Console.WriteLine($"'git diff' done in '{watch.Elapsed}'");

                if (process.ExitCode == 0)
                {
                    return;
                }

                Console.WriteLine($"##vso[artifact.upload artifactname=diff;]{diffFile}", diffFile);
                Console.WriteLine($"##vso[task.complete result=Failed]Test failed, see the logs under /Summary/Build artifacts for details");
            }
            else
            {
                Exec("git", "-c core.autocrlf=input -c core.safecrlf=false -c core.longpaths=true add -A", cwd: workingFolder);
                Exec("git", $"-c user.name=\"docfx-impact-ci\" -c user.email=\"docfx-impact-ci@microsoft.com\" commit -m \"**DISABLE_SECRET_SCANNING** {s_repositoryName}: {s_commitString}\"", cwd: workingFolder, ignoreError: true);
            }
        }

        private static void PushChanges(string workingFolder)
        {
            if (s_isPullRequest)
            {
                SendPullRequestComments();
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
            bool ignoreError = false,
            bool redirectStandardError = false,
            int[]? allowExitCodes = null,
            params string[] secrets)
        {
            var stopwatch = Stopwatch.StartNew();
            var sanitizedArguments = secrets.Aggregate(arguments, (arg, secret) => string.IsNullOrEmpty(secret) ? arg : arg.Replace(secret, "***"));
            allowExitCodes ??= new int[] { 0, 1 };

            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine($"{fileName} {sanitizedArguments}");
            Console.ResetColor();

            var process = Process.Start(new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                WorkingDirectory = cwd ?? ".",
                UseShellExecute = false,
                RedirectStandardError = redirectStandardError,
                RedirectStandardInput = !string.IsNullOrEmpty(stdin),
            }) ?? throw new InvalidOperationException();

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
            var azureReposBasicAuth = $"-c http.https://dev.azure.com.extraheader=\"AUTHORIZATION: basic {BasicAuth(s_azureDevopsToken)}\"";
            var githubBasicAuth = $"-c http.https://github.com.extraheader=\"AUTHORIZATION: basic {BasicAuth(s_githubToken)}\"";
            return $"{azureReposBasicAuth} {githubBasicAuth}";
        }

        private static string BasicAuth(string? token)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes($"user:{token}"));
        }

        private static void SendPullRequestComments(string? crashedMessage = null)
        {
            var isTimeout = s_testResult.buildTime.TotalSeconds > s_testResult.timeout;
            if (s_testResult.succeeded && !isTimeout && crashedMessage == null)
            {
                return;
            }

            var statusIcon = crashedMessage != null
                             ? "🚗🌳💥🤕🚑"
                             : isTimeout
                               ? "🧭"
                               : "⚠";

            var summary = $"{statusIcon}" +
                          $"<a href='{s_repository}'>{s_repositoryName}</a>" +
                          (crashedMessage != null
                          ? ""
                          : $"({s_testResult.buildTime}{(isTimeout ? $" | exceed {s_testResult.timeout}s" : "")}" +
                            $"{(s_testResult.succeeded ? "" : $", {s_testResult.moreLines} more diff")}" +
                            $")");
            var body = $"<details><summary>{summary}</summary>\n\n```diff\n{crashedMessage ?? s_testResult.diff}\n```\n\n</details>";

            if (int.TryParse(Environment.GetEnvironmentVariable("PULL_REQUEST_NUMBER") ?? "", out var prNumber))
            {
                SendGitHubPullRequestComments(prNumber, body).GetAwaiter().GetResult();
            }
        }

        private static async Task SendGitHubPullRequestComments(int prNumber, string body)
        {
            using var http = new HttpClient();
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
