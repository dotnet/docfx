// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using CommandLine;

namespace Microsoft.Docs.Build
{
    class RegressionTest
    {
        static class IntegrationTest
        {
            static IntegrationTest()
            {
                Environment.SetEnvironmentVariable("DOCS_ENVIRONMENT", "PPE");
                Environment.SetEnvironmentVariable("DOCS_SITE_NAME", "Docs");
                Environment.SetEnvironmentVariable("DOCFX_MAX_WARNINGS", "5000");
            }

            const string TestDataRepositoryUrl = "https://dev.azure.com/ceapex/Engineering/_git/docfx.RegressionTest.TestData";
            const string TestDiskRoot = "D:/";

            static readonly string s_repositoryRoot = Environment.GetEnvironmentVariable("BUILD_SOURCESDIRECTORY") ?? FindRepositoryRoot(AppContext.BaseDirectory);
            static readonly string s_testDataRoot = Path.Join(TestDiskRoot, "docfx.RegressionTest.TestData");

            static readonly string s_githubToken = Environment.GetEnvironmentVariable("DOCS_GITHUB_TOKEN");
            static readonly string s_azureDevopsToken = Environment.GetEnvironmentVariable("AZURE_DEVOPS_TOKEN");
            static readonly string s_gitCmdAuth = GetGitCommandLineAuthorization();
            static readonly bool s_isPullRequest = Environment.GetEnvironmentVariable("BUILD_REASON") == "PullRequest";
            static readonly (string hash, string[] descriptions) s_commitString = s_isPullRequest ? default : GetCommitString();

            static (string name, string repository, bool succeeded, TimeSpan buildTime, int? timeout, string diff, int moreLines) s_testResult;

            static void Main(string[] args)
            {
                Parser.Default.ParseArguments<Options>(args)
                    .WithParsed(opts =>
                    {
                        EnsureTestData(opts.Repository, opts.Branch);
                        Test(opts);
                        PushChanges(opts.Repository);
                    });
            }

            static (string, string[]) GetCommitString()
            {
                var docfxPath = Path.Combine(s_repositoryRoot, "../docfx");
                var docfxSha = ExecOutput("git", "rev-parse --short HEAD", docfxPath);
                var docsBuildSha = ExecOutput("git", "rev-parse --short HEAD", s_repositoryRoot);
                var docfxMessage = new string(ExecOutput("git", $"show -s --format=%B HEAD", docfxPath).ToArray());
                var docsBuildMessage = new string(ExecOutput("git", $"show -s --format=%B HEAD", s_repositoryRoot).ToArray());

                return ($"{docsBuildSha}-{docfxSha}", new[] { docsBuildMessage, docfxMessage });
            }

            static bool Test(Options opts)
            {

                var (baseLinePath, outputPath, workingFolder, repositoryPath) = Prepare(opts);

                Clean(outputPath);

                var buildTime = Build(repositoryPath, outputPath);
                Prettify(outputPath);
                Compare(outputPath, opts.Repository, baseLinePath, buildTime, opts.Timeout, workingFolder);

                Console.BackgroundColor = ConsoleColor.DarkMagenta;
                Console.WriteLine($"Test Pass {workingFolder}");
                Console.ResetColor();
                return true;
            }

            private static (string baseLinePath, string outputPath, string workingFolder, string repositoryPath) Prepare(Options opts)
            {
                var repositoryName = "engineering";// Path.GetFileName(opts.Repository);
                var workingFolder = Path.Combine(s_testDataRoot, $"regression-test.{repositoryName}");
                var repositoryPath = Path.Combine(workingFolder, repositoryName);

                Console.BackgroundColor = ConsoleColor.DarkMagenta;
                Console.WriteLine($"Testing {repositoryName}");
                Console.ResetColor();

                var baseLinePath = Path.Combine(workingFolder, "output");
                Directory.CreateDirectory(baseLinePath);
                var outputPath = s_isPullRequest ? Path.Combine(workingFolder, ".temp") : baseLinePath;

                var gitToken = opts.Repository.StartsWith("https://github.com") ? s_githubToken : s_azureDevopsToken;

                // Set Env for Build
                Environment.SetEnvironmentVariable("DOCS_ENVIRONMENT", "PROD");
                Environment.SetEnvironmentVariable("DOCFX_GIT_TOKEN", gitToken);
                Environment.SetEnvironmentVariable("DOCFX_GITHUB_TOKEN", s_githubToken);
                Environment.SetEnvironmentVariable("DOCFX_REPOSITORY_URL", opts.Repository);
                Environment.SetEnvironmentVariable("DOCFX_REPOSITORY_BRANCH", opts.Branch);
                Environment.SetEnvironmentVariable("DOCFX_LOCALE", opts.Locale);

                return (baseLinePath, outputPath, workingFolder, repositoryPath);
            }

            private static void EnsureTestData(string repository, string branch)
            {
                var testRepositoryName = Path.GetFileName(repository);
                var testWorkingFolder = Path.Combine(s_testDataRoot, $"regression-test.{testRepositoryName}");


                if (!Directory.Exists(testWorkingFolder))
                {
                    Directory.CreateDirectory(testWorkingFolder);
                    Exec("git", $"init", cwd: testWorkingFolder);
                    Exec("git", $"remote add origin {TestDataRepositoryUrl}", cwd: testWorkingFolder);
                    Exec("git", $"{s_gitCmdAuth} fetch origin --progress template", cwd: testWorkingFolder, secrets: s_gitCmdAuth);
                }

                try
                {
                    Exec("git", $"{s_gitCmdAuth} fetch origin --progress --prune {testRepositoryName}", cwd: testWorkingFolder, secrets: s_gitCmdAuth, redirectStandardError: true);
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains($"couldn't find remote ref {testRepositoryName}"))
                    {
                        // A new repo is added for the first time
                        Exec("git", $"checkout -B {testRepositoryName} origin/template", cwd: testWorkingFolder);
                        Exec("git", $"clean -xdff", cwd: testWorkingFolder);
                        Exec("git", $"{s_gitCmdAuth} submodule add -f --branch {branch} {repository} {testRepositoryName}", testWorkingFolder, secrets: s_gitCmdAuth);
                        return;
                    }
                    throw ex;
                }

                Exec("git", $"checkout --force origin/{testRepositoryName}", cwd: testWorkingFolder);
                Exec("git", $"clean -xdff", cwd: testWorkingFolder);

                var submoduleUpdateFlags = s_isPullRequest ? "" : "--remote";
                Exec("git", $"{s_gitCmdAuth} submodule sync {testRepositoryName}", cwd: testWorkingFolder, secrets: s_gitCmdAuth);
                Exec("git", $"{s_gitCmdAuth} submodule update {submoduleUpdateFlags} --init --progress --force {testRepositoryName}", cwd: testWorkingFolder, secrets: s_gitCmdAuth);
                Exec("git", $"clean -xdf -e **/_cache/* -e **/_repo_cache/*", Path.Combine(testWorkingFolder, testRepositoryName));
            }

            static void Clean(string outputPath)
            {
                if (Directory.Exists(outputPath))
                {
                    Directory.Delete(outputPath, recursive: true);
                    Directory.CreateDirectory(outputPath);
                }
            }

            static TimeSpan Build(string repositoryPath, string outputPath)
            {
                return Exec(Path.Combine(AppContext.BaseDirectory, "docfx.exe"), arguments: $"build -o \"{outputPath}\" --legacy", cwd: repositoryPath);
            }

            static void Compare(string outputPath, string repository, string existingOutputPath, TimeSpan buildTime, int? timeout, string testWorkingFolder)
            {
                var testRepositoryName = Path.GetFileName(repository);
                if (s_isPullRequest)
                {
                    var watch = Stopwatch.StartNew();
                    var process = Process.Start(new ProcessStartInfo
                    {
                        FileName = "git",
                        Arguments = $"--no-pager -c core.autocrlf=input -c core.safecrlf=false diff --no-index --ignore-cr-at-eol --exit-code \"{existingOutputPath}\" \"{outputPath}\"",
                        WorkingDirectory = TestDiskRoot, // starting `git diff` from root makes it faster
                        RedirectStandardOutput = true,
                    });

                    var diffFile = Path.Combine(s_testDataRoot, $".temp/{testRepositoryName}.patch");

                    Directory.CreateDirectory(Path.GetDirectoryName(diffFile));
                    var (diff, totalLines) = PipeOutputToFile(process.StandardOutput, diffFile, maxLines: 100000);
                    process.WaitForExit();

                    s_testResult = (testRepositoryName, repository, process.ExitCode == 0, buildTime, timeout, diff, totalLines);
                    watch.Stop();
                    Console.WriteLine($"'git diff' done in '{watch.Elapsed}'");

                    if (buildTime.TotalSeconds > timeout)
                    {
                        Console.WriteLine($"##vso[task.complete result=Failed]Test failed, build timeout. Repo: ${testRepositoryName}");
                    }

                    if (process.ExitCode == 0)
                    {
                        return;
                    }

                    Console.WriteLine($"##vso[artifact.upload artifactname=diff;]{diffFile}", diffFile);
                    Console.WriteLine($"##vso[task.complete result=Failed]Test failed, see the logs under /Summary/Build artifacts for details");
                }
                else
                {
                    var commitMessageDetails = string.Join(' ', s_commitString.descriptions.Select(m => $"-m \"{m.Replace('\"', ' ')}\""));
                    Exec("git", "-c core.autocrlf=input -c core.safecrlf=false add -A", testWorkingFolder);
                    Exec("git", $"-c user.name=\"docfx-impact-ci\" -c user.email=\"docfx-impact-ci@microsoft.com\" commit -m \"**DISABLE_SECRET_SCANNING** {testRepositoryName}: {s_commitString.hash}\" {commitMessageDetails}", testWorkingFolder, ignoreError: true);
                }
            }

            static void PushChanges(string repository)
            {
                if (s_isPullRequest)
                {
                    SendPullRequestComments().GetAwaiter().GetResult();
                }
                else
                {
                    var testRepositoryName = Path.GetFileName(repository);
                    var testWorkingFolder = Path.Combine(s_testDataRoot, testRepositoryName);
                    Exec("git", $"{s_gitCmdAuth} push  origin HEAD:{testRepositoryName}", testWorkingFolder, secrets: s_gitCmdAuth);
                }

            }

            static TimeSpan Exec(string fileName, string arguments = "", string cwd = null, bool ignoreError = false, bool redirectStandardError = false, params string[] secrets)
            {
                var stopwatch = Stopwatch.StartNew();
                var sanitizedArguments = secrets.Aggregate(arguments, (arg, secret) => string.IsNullOrEmpty(secret) ? arg : arg.Replace(secret, "***"));

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
                });
                var stderr = redirectStandardError ? process.StandardError.ReadToEnd() : default;
                process.WaitForExit();

                // TODO: docs-pipeline shoud not exit 1 for content error while reporting moved to docs.build
                if (!new int[] { 0, 1 }.Contains(process.ExitCode) && !ignoreError)
                {
                    throw new InvalidOperationException(
                        $"'\"{fileName}\" {sanitizedArguments}' failed in directory '{cwd}' with exit code {process.ExitCode}, message: \n {stderr}");
                }

                stopwatch.Stop();
                Console.WriteLine($"'{fileName} {sanitizedArguments}' done in '{stopwatch.Elapsed}'");
                return stopwatch.Elapsed;
            }

            static string ExecOutput(string fileName, string arguments, string cwd = null)
            {
                var process = Process.Start(new ProcessStartInfo { FileName = fileName, Arguments = arguments, WorkingDirectory = cwd, UseShellExecute = false, RedirectStandardOutput = true });
                var result = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException(
                        $"'\"{fileName}\" {arguments}' failed in directory '{cwd}' with exit code {process.ExitCode}");
                }
                return result.Trim();
            }

            static (string, int) PipeOutputToFile(StreamReader reader, string path, int maxLines)
            {
                var maxSummaryLines = 200;
                var totalLines = 0;
                var result = new StringBuilder();

                using (var output = File.CreateText(path))
                {
                    string line;
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

            static string FindRepositoryRoot(string path)
            {
                return Directory.Exists(Path.Combine(path, ".git")) ? path : FindRepositoryRoot(Path.GetDirectoryName(path));
            }

            static string GetGitCommandLineAuthorization()
            {
                var azureReposBasicAuth = $"-c http.https://dev.azure.com.extraheader=\"AUTHORIZATION: basic {BasicAuth(s_azureDevopsToken)}\"";
                var githubBasicAuth = $"-c http.https://github.com.extraheader=\"AUTHORIZATION: basic {BasicAuth(s_githubToken)}\"";
                return $"{azureReposBasicAuth} {githubBasicAuth}";
            }

            static string BasicAuth(string token)
            {
                return Convert.ToBase64String(Encoding.UTF8.GetBytes($"user:{token}"));
            }

            static void Prettify(string outputPath)
            {
                // remove docfx.yml to ignore the diff caused by xref url for now
                // the logic can be removed while docfx.yml not generated anymore
                foreach (var configPath in Directory.GetFiles(outputPath, "docfx.yml", SearchOption.AllDirectories))
                {
                    File.Delete(configPath);
                }

                Parallel.ForEach(Directory.GetFiles(outputPath, "*.*", SearchOption.AllDirectories), PrettifyFile);

                static void PrettifyFile(string path)
                {
                    switch (Path.GetExtension(path).ToLowerInvariant())
                    {
                        case ".json":
                            File.WriteAllText(path, PrettifyJson(File.ReadAllText(path)));
                            break;

                        case ".log":
                        case ".txt":
                            File.WriteAllLines(path, File.ReadAllLines(path).OrderBy(line => line).Select(PrettifyLogJson));
                            break;
                    }
                }

                static string PrettifyJson(string json)
                {
                    return PrettfyNewLine(JToken.Parse(json).ToString());
                }

                static string PrettifyLogJson(string json)
                {
                    var obj = JObject.Parse(json);
                    obj.Remove("date_time");
                    return PrettfyNewLine(obj.ToString());
                }

                static string PrettfyNewLine(string text)
                {
                    return text.Replace("\r", "").Replace("\\n\\n", "⬇\n").Replace("\\n", "⬇\n");
                }
            }

            static Task SendPullRequestComments()
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

            static async Task SendGitHubPullRequestComments(int prNumber, string body)
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

            static async Task SendAzureDevOpsPullRequestComments(int prId, string content)
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
}
