// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common.Git
{
    using System;
    using System.IO;
    using System.Text;
    using System.Collections.Concurrent;

    public static class GitUtility
    {
        private static readonly string CommandName = "git";
        private static readonly int GitTimeOut = 1000;

        private static readonly string GetRepoRootCommand = "rev-parse --show-toplevel";
        private static readonly string GetLocalBranchCommand = "rev-parse --abbrev-ref HEAD";
        private static readonly string GetRemoteBranchCommand = "rev-parse --abbrev-ref @{u}";
        // TODO: only get default remote's url currently.
        private static readonly string GetOriginUrlCommand = "config --get remote.origin.url";
        private static readonly string GetLocalHeadIdCommand = "rev-parse HEAD";
        private static readonly string GetRemoteHeadIdCommand = "rev-parse @{u}";

        private static readonly ConcurrentDictionary<string, GitRepoInfo> Cache = new ConcurrentDictionary<string, GitRepoInfo>();

        public static GitDetail TryGetFileDetail(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return null;
            }

            GitDetail detail = null;
            try
            {
                detail = GetFileDetailCore(filePath);
            }
            catch (Exception)
            {
                // ignored
            }
            return detail;
        }

        public static GitRepoInfo GetRepoInfo(string directory)
        {
            if (directory == null)
            {
                throw new ArgumentNullException(nameof(directory));
            }

            if (!ExistGitCommand())
            {
                throw new GitException("Can't find git command in current environment");
            }

            return Cache.GetOrAdd(directory, GetRepoInfoCore);
        }

        #region Private Methods
        private static GitDetail GetFileDetailCore(string filePath)
        {
            var directory = Path.GetDirectoryName(filePath);

            GitRepoInfo repoInfo;
            if (!Cache.TryGetValue(directory, out repoInfo))
            {
                repoInfo = GetRepoInfo(directory);
            }

            return new GitDetail
            {
                CommitId = repoInfo.RemoteHeadCommitId,
                RemoteBranch = repoInfo.RemoteBranch,
                RemoteRepositoryUrl = repoInfo.RemoteOriginUrl,
                RelativePath = PathUtility.MakeRelativePath(repoInfo.RepoRootPath, filePath)
            };
        }

        private static GitRepoInfo GetRepoInfoCore(string directory)
        {
            var repoRootPath = RunGitCommandAndGetFirstLine(directory, GetRepoRootCommand);
            var localBranch = RunGitCommandAndGetFirstLine(repoRootPath, GetLocalBranchCommand);

            string remoteBranch;
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

            var originUrl = RunGitCommandAndGetFirstLine(repoRootPath, GetOriginUrlCommand);

            return new GitRepoInfo
            {
                LocalBranch = localBranch,
                LocalHeadCommitId = RunGitCommandAndGetFirstLine(repoRootPath, GetLocalHeadIdCommand),
                RemoteHeadCommitId = TryRunGitCommandAndGetFirstLine(repoRootPath, GetRemoteHeadIdCommand),
                RemoteOriginUrl = originUrl,
                RepoRootPath = repoRootPath,
                RemoteBranch = remoteBranch,
            };
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
            return CommandUtility.ExistCommand(CommandName);
        }
        #endregion
    }
}
