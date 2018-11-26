// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common.Git
{
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.IO;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Web;

    using Microsoft.DocAsCode.Plugins;

    public static class GitUtility
    {
        private static readonly string CommandName = "git";
        private static readonly int GitTimeOut = 1000;

        private static readonly string GetRepoRootCommand = "rev-parse --show-toplevel";
        private static readonly string GetLocalBranchCommand = "rev-parse --abbrev-ref HEAD";
        private static readonly string GetLocalBranchCommitIdCommand = "rev-parse HEAD";
        private static readonly string GetRemoteBranchCommand = "rev-parse --abbrev-ref @{u}";

        private static readonly Regex GitHubRepoUrlRegex =
            new Regex(@"^((https|http):\/\/(.+@)?github\.com\/|git@github\.com:)(?<account>\S+)\/(?<repository>[A-Za-z0-9_.-]+)(\.git)?\/?$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.RightToLeft);

        private static readonly Regex VsoGitRepoUrlRegex =
            new Regex(@"^(((https|http):\/\/(?<account>\S+))|((ssh:\/\/)(?<account>\S+)@(?:\S+)))\.visualstudio\.com(?<port>:\d+)?(?:\/DefaultCollection)?(\/(?<project>[^\/]+)(\/.*)*)*\/(?:_git|_ssh)\/(?<repository>([^._,]|[^._,][^@~;{}'+=,<>|\/\\?:&$*""#[\]]*[^.,]))$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly string GitHubNormalizedRepoUrlTemplate = "https://github.com/{0}/{1}";
        private static readonly string VsoNormalizedRepoUrlTemplate = "https://{0}.visualstudio.com/DefaultCollection/{1}/_git/{2}";

        // TODO: only get default remote's url currently.
        private static readonly string GetOriginUrlCommand = "config --get remote.origin.url";

        private static readonly string[] BuildSystemBranchName = new[]
        {
            "APPVEYOR_REPO_BRANCH",   // AppVeyor
            "Git_Branch",             // Team City
            "CI_BUILD_REF_NAME",      // GitLab CI
            "GIT_LOCAL_BRANCH",       // Jenkins
            "GIT_BRANCH",             // Jenkins
            "BUILD_SOURCEBRANCHNAME"  // VSO Agent
        };

        private static readonly ConcurrentDictionary<string, GitRepoInfo> Cache = new ConcurrentDictionary<string, GitRepoInfo>();

        private static bool? GitCommandExists = null;
        private static object SyncRoot = new object();

        public static GitDetail TryGetFileDetail(string filePath)
        {
            if (EnvironmentContext.GitFeaturesDisabled)
            {
                return null;
            }

            try
            {
                return GetFileDetail(filePath);
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Skipping GetFileDetail. Exception found: {ex.GetType()}, Message: {ex.Message}");
                Logger.LogVerbose(ex.ToString());
            }

            return null;
        }

        public static GitRepoInfo Parse(string repoUrl)
        {
            if (string.IsNullOrEmpty(repoUrl))
            {
                throw new ArgumentNullException(nameof(repoUrl));
            }

            var githubMatch = GitHubRepoUrlRegex.Match(repoUrl);
            if (githubMatch.Success)
            {
                var gitRepositoryAccount = githubMatch.Groups["account"].Value;
                var gitRepositoryName = githubMatch.Groups["repository"].Value;
                return new GitRepoInfo
                {
                    RepoType = RepoType.GitHub,
                    RepoAccount = gitRepositoryAccount,
                    RepoName = gitRepositoryName,
                    RepoProject = null,
                    NormalizedRepoUrl = new Uri(string.Format(GitHubNormalizedRepoUrlTemplate, gitRepositoryAccount, gitRepositoryName))
                };
            }

            var vsoMatch = VsoGitRepoUrlRegex.Match(repoUrl);
            if (vsoMatch.Success)
            {
                var gitRepositoryAccount = vsoMatch.Groups["account"].Value;
                var gitRepositoryName = vsoMatch.Groups["repository"].Value;

                // VSO has this logic: if the project name and repository name are same, then VSO will return the url without project name.
                // Sample: if you visit https://cpubwin.visualstudio.com/drivers/_git/drivers, it will return https://cpubwin.visualstudio.com/_git/drivers
                // We need to normalize it to keep same behavior with other projects. Always return https://<account>.visualstudio.com/<collection>/<project>/_git/<repository>
                var gitRepositoryProject = string.IsNullOrEmpty(vsoMatch.Groups["project"].Value) ? gitRepositoryName : vsoMatch.Groups["project"].Value;
                return new GitRepoInfo
                {
                    RepoType = RepoType.Vso,
                    RepoAccount = gitRepositoryAccount,
                    RepoName = Uri.UnescapeDataString(gitRepositoryName),
                    RepoProject = gitRepositoryProject,
                    NormalizedRepoUrl = new Uri(string.Format(VsoNormalizedRepoUrlTemplate, gitRepositoryAccount, gitRepositoryProject, gitRepositoryName))
                };
            }

            throw new NotSupportedException($"'{repoUrl}' is not a valid Vso/GitHub repository url");
        }

        public static Uri CombineUrl(string normalizedRepoUrl, string refName, string relativePathToRepoRoot, RepoType repoType)
        {
            switch (repoType)
            {
                case RepoType.GitHub:
                    return new Uri(Path.Combine(normalizedRepoUrl, "blob", refName, relativePathToRepoRoot));
                case RepoType.Vso:
                    var rootedPathToRepo = "/" + relativePathToRepoRoot.ToNormalizedPath();
                    return new Uri($"{normalizedRepoUrl}?path={HttpUtility.UrlEncode(rootedPathToRepo)}&version=GB{HttpUtility.UrlEncode(refName)}&_a=contents");
                default:
                    throw new NotSupportedException($"RepoType '{repoType}' is not supported.");
            }
        }

        #region Private Methods

        private static GitDetail GetFileDetail(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !ExistGitCommand())
            {
                return null;
            }

            var path = Path.Combine(EnvironmentContext.BaseDirectory, filePath).ToNormalizedPath();

            var detail = GetFileDetailCore(path);
            return detail;
        }

        private static GitRepoInfo GetRepoInfo(string directory)
        {
            if (directory == null)
            {
                return null;
            }

            if (IsGitRoot(directory))
            {
                return Cache.GetOrAdd(directory, GetRepoInfoCore);
            }

            var parentDirInfo = Directory.GetParent(directory);
            if (parentDirInfo == null)
            {
                return null;
            }

            return Cache.GetOrAdd(directory, d => GetRepoInfo(parentDirInfo.FullName));
        }

        private static bool IsGitRoot(string directory)
        {
            var gitPath = Path.Combine(directory, ".git");

            // git submodule contains only a .git file instead of a .git folder
            return Directory.Exists(gitPath) || File.Exists(gitPath);
        }

        private static GitDetail GetFileDetailCore(string filePath)
        {
            string directory;
            if (PathUtility.IsDirectory(filePath))
            {
                directory = filePath;
            }
            else
            {
                directory = Path.GetDirectoryName(filePath);
            }

            var repoInfo = Cache.GetOrAdd(directory, GetRepoInfo);

            return new GitDetail
            {
                // TODO: remove commit id to avoid config hash changed
                // CommitId = repoInfo?.RemoteHeadCommitId,
                RemoteBranch = repoInfo?.RemoteBranch,
                RemoteRepositoryUrl = repoInfo?.RemoteOriginUrl,
                RelativePath = PathUtility.MakeRelativePath(repoInfo?.RepoRootPath, filePath)
            };
        }

        private static GitRepoInfo GetRepoInfoCore(string directory)
        {
            var repoRootPath = RunGitCommandAndGetLastLine(directory, GetRepoRootCommand);

            // the path of repo root got from git config file should be the same with path got from git command
            Debug.Assert(FilePathComparer.OSPlatformSensitiveComparer.Equals(repoRootPath, directory));

            var branchNames = GetBranchNames(repoRootPath);

            var originUrl = RunGitCommandAndGetLastLine(repoRootPath, GetOriginUrlCommand);
            var repoInfo = new GitRepoInfo
            {
                // TODO: remove commit id to avoid config hash changed
                //LocalHeadCommitId = RunGitCommandAndGetFirstLine(repoRootPath, GetLocalHeadIdCommand),
                //RemoteHeadCommitId = TryRunGitCommandAndGetFirstLine(repoRootPath, GetRemoteHeadIdCommand),
                RemoteOriginUrl = originUrl,
                RepoRootPath = repoRootPath,
                LocalBranch = branchNames.Item1,
                RemoteBranch = branchNames.Item2
            };

            return repoInfo;
        }

        private static Tuple<string, string> GetBranchNames(string repoRootPath)
        {
            // Use the branch name specified by the environment variable.
            var localBranch = Environment.GetEnvironmentVariable("DOCFX_SOURCE_BRANCH_NAME");
            if (!string.IsNullOrEmpty(localBranch))
            {
                Logger.LogInfo($"For git repo <{repoRootPath}>, using branch '{localBranch}' from the environment variable DOCFX_SOURCE_BRANCH_NAME.");
                return Tuple.Create(localBranch, localBranch);
            }

            var isDetachedHead = "HEAD" == RunGitCommandAndGetLastLine(repoRootPath, GetLocalBranchCommand);
            if (isDetachedHead)
            {
                return GetBranchNamesFromDetachedHead(repoRootPath);
            }

            localBranch = RunGitCommandAndGetLastLine(repoRootPath, GetLocalBranchCommand);
            string remoteBranch;
            try
            {
                remoteBranch = RunGitCommandAndGetLastLine(repoRootPath, GetRemoteBranchCommand);
                var index = remoteBranch.IndexOf('/');
                if (index > 0)
                {
                    remoteBranch = remoteBranch.Substring(index + 1);
                }
            }
            catch (Exception ex)
            {
                Logger.LogInfo($"For git repo <{repoRootPath}>, can't find remote branch in this repo and fallback to use local branch [{localBranch}]: {ex.Message}");
                remoteBranch = localBranch;
            }
            return Tuple.Create(localBranch, remoteBranch);
        }

        // Many build systems use a "detached head", which means that the normal git commands
        // to get branch names do not work. Thankfully, they set an environment variable.
        private static Tuple<string, string> GetBranchNamesFromDetachedHead(string repoRootPath)
        {
            foreach (var name in BuildSystemBranchName)
            {
                var branchName = Environment.GetEnvironmentVariable(name);
                if (!string.IsNullOrEmpty(branchName))
                {
                    Logger.LogInfo($"For git repo <{repoRootPath}>, using branch '{branchName}' from the environment variable {name}.");
                    return Tuple.Create(branchName, branchName);
                }
            }

            // Use the comment id as the branch name.
            var commitId = RunGitCommandAndGetLastLine(repoRootPath, GetLocalBranchCommitIdCommand);
            Logger.LogInfo($"For git repo <{repoRootPath}>, using commit id {commitId} as the branch name.");
            return Tuple.Create(commitId, commitId);
        }

        private static void ProcessErrorMessage(string message)
        {
            throw new GitException(message);
        }

        private static string TryRunGitCommand(string repoPath, string arguments)
        {
            var content = new StringBuilder();
            try
            {
                RunGitCommand(repoPath, arguments, output => content.AppendLine(output));
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Skipping RunGitCommand. Exception found: {ex.GetType()}, Message: {ex.Message}");
                Logger.LogVerbose(ex.ToString());
            }
            return content.Length == 0 ? null : content.ToString();
        }

        private static string TryRunGitCommandAndGetLastLine(string repoPath, string arguments)
        {
            string content = null;
            try
            {
                content = RunGitCommandAndGetLastLine(repoPath, arguments);
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Skipping RunGitCommandAndGetLastLine. Exception found: {ex.GetType()}, Message: {ex.Message}");
                Logger.LogVerbose(ex.ToString());
            }
            return content;
        }

        private static string RunGitCommandAndGetLastLine(string repoPath, string arguments)
        {
            string content = null;
            RunGitCommand(repoPath, arguments, output => content = output);

            if (string.IsNullOrEmpty(content))
            {
                throw new GitException("The result can't be null or empty string");
            }
            return content;
        }

        private static void RunGitCommand(string repoPath, string arguments, Action<string> processOutput)
        {
            var encoding = Encoding.UTF8;
            const int bufferSize = 4096;

            if (!Directory.Exists(repoPath))
            {
                throw new ArgumentException($"Can't find repo: {repoPath}");
            }

            using (var outputStream = new MemoryStream())
            using (var errorStream = new MemoryStream())
            {
                int exitCode;

                using (var outputStreamWriter = new StreamWriter(outputStream, encoding, bufferSize, true))
                using (var errorStreamWriter = new StreamWriter(errorStream, encoding, bufferSize, true))
                {
                    exitCode = CommandUtility.RunCommand(new CommandInfo
                    {
                        Name = CommandName,
                        Arguments = arguments,
                        WorkingDirectory = repoPath,
                    }, outputStreamWriter, errorStreamWriter, GitTimeOut);

                    // writer streams have to be flushed before reading from memory streams
                    // make sure that streamwriter is not closed before reading from memory stream
                    outputStreamWriter.Flush();
                    errorStreamWriter.Flush();

                    if (exitCode != 0)
                    {
                        errorStream.Position = 0;
                        using (var errorStreamReader = new StreamReader(errorStream, encoding, false, bufferSize, true))
                        {
                            ProcessErrorMessage(errorStreamReader.ReadToEnd());
                        }
                    }
                    else
                    {
                        outputStream.Position = 0;
                        using (var streamReader = new StreamReader(outputStream, encoding, false, bufferSize, true))
                        {
                            string line;
                            while ((line = streamReader.ReadLine()) != null)
                            {
                                processOutput(line);
                            }
                        }
                    }
                }
            }
        }

        private static bool ExistGitCommand()
        {
            if (GitCommandExists == null)
            {
                lock (SyncRoot)
                {
                    if (GitCommandExists == null)
                    {
                        GitCommandExists = CommandUtility.ExistCommand(CommandName);
                        if (GitCommandExists != true)
                        {
                            Logger.LogInfo("Looks like Git is not installed globally. We depend on Git to extract repository information for source code and files.");
                        }
                    }
                }
            }

            return GitCommandExists.Value;
        }

        #endregion
    }
}