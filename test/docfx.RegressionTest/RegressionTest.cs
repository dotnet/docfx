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

        private static (string name, string repository, bool succeeded, TimeSpan buildTime, int? timeout, string diff, int moreLines) s_testResult;

        private static int Main(string[] args)
        {
            return Parser.Default.ParseArguments<Options>(args).MapResult(Run, _ => -9999);
        }

        private static int Run(Options opts)
        {
            var repositoryName = opts.DryRun ? $"dryrun.{Path.GetFileName(opts.Repository)}" : Path.GetFileName(opts.Repository);
            var workingFolder = Path.Combine(s_testDataRoot, $"regression-test.{repositoryName}");

            EnsureTestData(opts, repositoryName, workingFolder);
            Test(opts, repositoryName, workingFolder);
            PushChanges(repositoryName, workingFolder);
            return 0;
        }

        private static (string baseLinePath, string outputPath, string repositoryPath, string docfxConfig) Prepare(Options opts, string repositoryName, string workingFolder)
        {
            var repositoryPath = Path.Combine(workingFolder, repositoryName);
            var cachePath = Path.Combine(workingFolder, "cache");
            var statePath = Path.Combine(workingFolder, "state");

            Console.BackgroundColor = ConsoleColor.DarkMagenta;
            Console.WriteLine($"Testing {repositoryName}");
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
            Environment.SetEnvironmentVariable("DOCFX_UPDATE_CACHE_SYNC", s_isPullRequest ? "false" : "true");

            return (baseLinePath, outputPath, repositoryPath, GetDocfxConfig(opts));

            static string GetDocfxConfig(Options opts)
            {
                // Git token for CRR restore
                var http = new Dictionary<string, object>();
                http["https://github.com"] = new { headers = ToAuthHeader(s_githubToken) };
                http["https://dev.azure.com"] = new { headers = ToAuthHeader(s_azureDevopsToken) };
                var docfxConfig = JObject.FromObject(new
                {
                    http,
                    maxWarnings = 5000,
                    maxInfos = 30000,
                    updateTimeAsCommitBuildTime = true,
                    githubToken = s_githubToken,
                    githubUserCacheExpirationInHours = s_isPullRequest ? 24 * 365 : 24 * 30,
                });

                if (opts.OutputHtml)
                {
                    docfxConfig["outputType"] = "html";
                    docfxConfig["outputUrlType"] = "ugly";
                    docfxConfig["template"] = "https://github.com/Microsoft/templates.docs.msft.pdf#master";
                }

                if (opts.RegressionMarkdownRule)
                {
                    docfxConfig["markdownValidationRules"] = "https://ops/regressionallcontentrules/";
                }

                if (opts.RegressionMetadataSchema)
                {
                    docfxConfig["metadataSchema"] = new JArray()
                    {
                        Path.Combine(AppContext.BaseDirectory, "data/schemas/OpsMetadata.json"),
                        "https://ops/regressionallmetadataschema/",
                    };
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

        private static bool Test(Options opts, string repositoryName, string workingFolder)
        {
            var (baseLinePath, outputPath, repositoryPath, docfxConfig) = Prepare(opts, repositoryName, workingFolder);

            Clean(outputPath);

            var buildTime = Build(repositoryPath, outputPath, opts, docfxConfig);
            Compare(opts, repositoryName, workingFolder, outputPath, baseLinePath, buildTime);

            Console.BackgroundColor = ConsoleColor.DarkMagenta;
            Console.WriteLine($"Test Pass {workingFolder}");
            Console.ResetColor();
            return true;
        }

        private static void EnsureTestData(Options opts, string repositoryName, string workingFolder)
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
                Exec("git", $"{s_gitCmdAuth} fetch origin --progress --prune {repositoryName}", cwd: workingFolder, secrets: s_gitCmdAuth, redirectStandardError: true);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains($"couldn't find remote ref {repositoryName}"))
                {
                    // A new repo is added for the first time
                    Exec("git", $"checkout -B {repositoryName} origin/template", cwd: workingFolder);
                    Exec("git", $"clean -xdff", cwd: workingFolder);
                    Exec("git", $"{s_gitCmdAuth} -c core.longpaths=true submodule add -f --branch {opts.Branch} {opts.Repository} {repositoryName}", cwd: workingFolder, secrets: s_gitCmdAuth);
                    return;
                }
                throw;
            }

            Exec("git", $"-c core.longpaths=true checkout --force origin/{repositoryName}", cwd: workingFolder);
            Exec("git", $"clean -xdff", cwd: workingFolder);

            var submoduleUpdateFlags = s_isPullRequest ? "" : "--remote";
            Exec("git", $"{s_gitCmdAuth} submodule set-branch -b {opts.Branch} {repositoryName}", cwd: workingFolder, secrets: s_gitCmdAuth);
            Exec("git", $"{s_gitCmdAuth} submodule sync {repositoryName}", cwd: workingFolder, secrets: s_gitCmdAuth);
            Exec("git", $"{s_gitCmdAuth} -c core.longpaths=true submodule update {submoduleUpdateFlags} --init --progress --force {repositoryName}", cwd: workingFolder, secrets: s_gitCmdAuth);
            Exec("git", $"clean -xdf", cwd: Path.Combine(workingFolder, repositoryName));
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
            var legacyOption = !opts.OutputHtml ? "--legacy" : "";
            var dryRunOption = opts.DryRun ? "--dry-run" : "";

            Exec(
                Path.Combine(AppContext.BaseDirectory, "docfx.exe"),
                arguments: $"restore {legacyOption} --verbose --stdin",
                stdin: docfxConfig,
                cwd: repositoryPath,
                allowExitCodes: new int[] { 0 });

            return Exec(
                Path.Combine(AppContext.BaseDirectory, "docfx.exe"),
                arguments: $"build -o \"{outputPath}\" {legacyOption} {dryRunOption} --verbose --no-restore --stdin",
                stdin: docfxConfig,
                cwd: repositoryPath);
        }

        private static void Compare(Options opts, string repositoryName, string workingFolder, string outputPath, string existingOutputPath, TimeSpan buildTime)
        {
            // For temporary normalize: use 'NormalizeJsonFiles' for output files
            Normalizer.Normalize(outputPath, NormalizeStage.PrettifyJsonFiles | NormalizeStage.PrettifyLogFiles, errorLevel: opts.ErrorLevel);

            if (buildTime.TotalSeconds > opts.Timeout)
            {
                Console.WriteLine($"##vso[task.complete result=Failed]Test failed, build timeout. Repo: ${repositoryName}");
                Console.WriteLine($"Test failed, build timeout: {buildTime.TotalSeconds} Repo: ${repositoryName}");
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
                });

                var diffFile = Path.Combine(s_testDataRoot, $".temp/{repositoryName}.patch");

                Directory.CreateDirectory(Path.GetDirectoryName(diffFile));
                var (diff, totalLines) = PipeOutputToFile(process.StandardOutput, diffFile, maxLines: 100000);
                process.WaitForExit();

                s_testResult = (repositoryName, opts.Repository, process.ExitCode == 0, buildTime, opts.Timeout, diff, totalLines);
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
                Exec("git", $"-c user.name=\"docfx-impact-ci\" -c user.email=\"docfx-impact-ci@microsoft.com\" commit -m \"**DISABLE_SECRET_SCANNING** {repositoryName}: {s_commitString}\"", cwd: workingFolder, ignoreError: true);
            }
        }

        private static void PushChanges(string repositoryName, string workingFolder)
        {
            if (s_isPullRequest)
            {
                SendPullRequestComments().GetAwaiter().GetResult();
            }
            else
            {
                Exec("git", $"{s_gitCmdAuth} push origin HEAD:{repositoryName}", cwd: workingFolder, secrets: s_gitCmdAuth);
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
                WorkingDirectory = cwd,
                UseShellExecute = false,
                RedirectStandardError = redirectStandardError,
                RedirectStandardInput = !string.IsNullOrEmpty(stdin),
            });
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

        private static Task SendPullRequestComments()
        {
            var isTimeout = s_testResult.buildTime.TotalSeconds > s_testResult.timeout;
            if (s_testResult.succeeded && !isTimeout)
            {
                return Task.CompletedTask;
            }

            var statusIcon = s_testResult.succeeded
                             ? !isTimeout
                               ? "✔"
                               : "🧭"
                             : "⚠";
            var summary = $"{statusIcon}" +
                          $"<a href='{s_testResult.repository}'>{s_testResult.name}</a>" +
                          $"({s_testResult.buildTime}{(isTimeout ? $" | exceed {s_testResult.timeout}s" : "")}" +
                          $"{(s_testResult.succeeded ? "" : $", {s_testResult.moreLines} more diff")}" +
                          $")";
            var body = $"<details><summary>{summary}</summary>\n\n```diff\n{s_testResult.diff}\n```\n\n</details>";

            if (int.TryParse(Environment.GetEnvironmentVariable("PULL_REQUEST_NUMBER") ?? "", out var prNumber))
            {
                return SendGitHubPullRequestComments(prNumber, body);
            }

            if (int.TryParse(Environment.GetEnvironmentVariable("PULL_REQUEST_ID") ?? "", out var prId))
            {
                return SendAzureDevOpsPullRequestComments(prId, body);
            }

            return Task.CompletedTask;
        }

        private static async Task SendGitHubPullRequestComments(int prNumber, string body)
        {
            using (var http = new HttpClient())
            {
                http.DefaultRequestHeaders.Add("User-Agent", "DocFX");
                http.DefaultRequestHeaders.Add("Authorization", $"bearer {s_githubToken}");

                var response = await http.PostAsync(
                    $"https://api.github.com/repos/dotnet/docfx/issues/{prNumber}/comments",
                    new StringContent(JsonConvert.SerializeObject(new { body }), Encoding.UTF8, "application/json"));

                response.EnsureSuccessStatusCode();
            }
        }

        private static async Task SendAzureDevOpsPullRequestComments(int prId, string content)
        {
            using (var http = new HttpClient())
            {
                http.DefaultRequestHeaders.Add("Authorization", $"basic {BasicAuth(s_azureDevopsToken)}");

                var response = await http.PostAsync(
                    $"https://dev.azure.com/ceapex/Engineering/_apis/git/repositories/Docs.Build/pullRequests/{prId}/threads/comments?api-version=5.0",
                    new StringContent(JsonConvert.SerializeObject(new { comments = new[] { new { content } } }), Encoding.UTF8, "application/json"));

                response.EnsureSuccessStatusCode();
            }
        }
    }
}
