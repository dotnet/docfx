// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common.Git
{
    using System;
    using System.Linq;
    using System.IO;
    using System.Text;
    using System.Collections.Concurrent;

    public static class GitUtility
    {
        private static readonly string CommandName = "git";
        private static readonly int GitTimeOut = 1000;
        private static bool? _existGitCommand;

        private static readonly string GetRepoRootCommand = "rev-parse --show-toplevel";
        private static readonly string GetLocalBranchCommand = "rev-parse --abbrev-ref HEAD";
        private static readonly string GetLocalBranchCommitIdCommand = "rev-parse HEAD";
        private static readonly string GetRemoteBranchCommand = "rev-parse --abbrev-ref @{u}";
        // TODO: only get default remote's url currently.
        private static readonly string GetOriginUrlCommand = "config --get remote.origin.url";
        private static readonly string GetLocalHeadIdCommand = "rev-parse HEAD";
        private static readonly string GetRemoteHeadIdCommand = "rev-parse @{u}";
      
        private static readonly string[] BuildSystemBranchName = new[]
        {
            "DOCFX_SOURCE_BRANCH_NAME",
            "APPVEYOR_REPO_BRANCH",   // AppVeyor
            "Git_Branch",             // Team City
            "CI_BUILD_REF_NAME",      // GitLab CI
            "GIT_LOCAL_BRANCH",       // Jenkins
            "GIT_BRANCH",             // Jenkins
            "BUILD_SOURCEBRANCHNAME"  // VSO Agent
        };

        private static readonly ConcurrentDictionary<string, GitRepoInfo> Cache = new ConcurrentDictionary<string, GitRepoInfo>();

        public static GitDetail TryGetFileDetail(string filePath)
        {
            GitDetail detail = null;
            try
            {
                detail = GetFileDetail(filePath);
            }
            catch (Exception)
            {
                // ignored
            }
            return detail;
        }

        public static GitDetail GetFileDetail(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return null;
            }

            if (!Path.IsPathRooted(filePath))
            {
                throw new GitException($"{nameof(filePath)} should be an absolute path");
            }

            if (!ExistGitCommand())
            {
                throw new GitException("Can't find git command in current environment");
            }

            var detail = GetFileDetailCore(filePath);
            return detail;
        }

        #region Private Methods
        private static bool IsGitRoot(string directory)
        {
            var gitPath = Path.Combine(directory, ".git");

            // git submodule contains only a .git file instead of a .git folder
            return Directory.Exists(gitPath) || File.Exists(gitPath);
        }

        private static GitRepoInfo GetRepoInfo(string directory)
        {
            if (IsGitRoot(directory))
            {
                return Cache.GetOrAdd(directory, GetRepoInfoCore);
            }

            var parentDirInfo = Directory.GetParent(directory);
            if (parentDirInfo == null)
            {
                return null;
            }

            return Cache.GetOrAdd(directory, GetRepoInfo(parentDirInfo.FullName));
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
                // CommitId = repoInfo.RemoteHeadCommitId,
                RemoteBranch = repoInfo.RemoteBranch,
                RemoteRepositoryUrl = repoInfo.RemoteOriginUrl,
                RelativePath = PathUtility.MakeRelativePath(repoInfo.RepoRootPath, filePath)
            };
        }

        private static GitRepoInfo GetRepoInfoCore(string directory)
        {
            var repoRootPath = RunGitCommandAndGetFirstLine(directory, GetRepoRootCommand);
          
            // Many build systems use a "detach head", which means that the normal git commands
            // to get branch names do not work.  Thankfully, they set an environment variable.
            string localBranch = BuildSystemBranchName
                .Select(Environment.GetEnvironmentVariable)
                .Where(name => !string.IsNullOrEmpty(name))
                .FirstOrDefault();
            string remoteBranch;
            if (localBranch != null)
            {
                remoteBranch = localBranch;
            }
            else
            {
                localBranch = GetLocalBranchName(repoRootPath);
                try
                {
                    remoteBranch = RunGitCommandAndGetFirstLine(repoRootPath, GetRemoteBranchCommand);
                    var index = remoteBranch.IndexOf('/');
                    if (index > 0)
                    {
                        remoteBranch = remoteBranch.Substring(index + 1);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogInfo($"Can't find remote branch in this repo and fallback to use local branch [{localBranch}]: {ex.Message}");
                    remoteBranch = localBranch;
                }
            }

            var originUrl = RunGitCommandAndGetFirstLine(repoRootPath, GetOriginUrlCommand);

            return new GitRepoInfo
            {
                LocalBranch = localBranch,
                // TODO: remove commit id to avoid config hash changed
                //LocalHeadCommitId = RunGitCommandAndGetFirstLine(repoRootPath, GetLocalHeadIdCommand),
                //RemoteHeadCommitId = TryRunGitCommandAndGetFirstLine(repoRootPath, GetRemoteHeadIdCommand),
                RemoteOriginUrl = originUrl,
                RepoRootPath = repoRootPath,
                RemoteBranch = remoteBranch,
            };
        }

        private static string GetLocalBranchName(string repoRootPath)
        {
            var localBranch = RunGitCommandAndGetFirstLine(repoRootPath, GetLocalBranchCommand);

            // Fallback to use commit id
            if (localBranch.Equals("HEAD"))
            {
                localBranch = RunGitCommandAndGetFirstLine(repoRootPath, GetLocalBranchCommitIdCommand);
                Logger.LogInfo("Fallback to use commit id as the branch name.");
            }
            return localBranch;
        }

        private static void ProcessErrorMessage(string message)
        {
            throw new GitException(message);
        }

        private static string TryRunGitCommandAndGetFirstLine(string repoPath, string arguments)
        {
            string content = null;
            try
            {
                content = RunGitCommandAndGetFirstLine(repoPath, arguments);
            }
            catch (Exception)
            {
                // ignored
            }
            return content;
        }

        private static string RunGitCommandAndGetFirstLine(string repoPath, string arguments)
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
                }

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

        private static bool ExistGitCommand()
        {
            if (_existGitCommand == null)
            {
                _existGitCommand = CommandUtility.ExistCommand(CommandName);
            }
            return _existGitCommand == true;
        }
        #endregion
    }
}
