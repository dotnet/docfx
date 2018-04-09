// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.Docs.Test
{
    public static class GitUtilTest
    {
        [Theory]
        [InlineData("README.md")]
        public static void GetCommitsSameAsGitLog(string file)
        {
            var repo = GitUtility.FindRepo(Path.GetFullPath(file));
            var pathToRepo = PathUtility.NormalizeFile(file);
            var exe = GetContributorsGitExe(repo, pathToRepo).ToList();
            var lib = GitUtility.GetCommits(repo, new List<string> { pathToRepo })[0].ToList();
            Assert.Equal(JsonConvert.SerializeObject(exe), JsonConvert.SerializeObject(lib));
        }

        private static GitCommit[] GetContributorsGitExe(string cwd, string path)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = $"log --format=\"%H|%cI|%an|%ae|%cn|%ce\" \"{path}\"",
                WorkingDirectory = cwd,
                UseShellExecute = false,
                RedirectStandardOutput = true,
            };
            var git = Process.Start(psi);
            var output = Task.Factory.StartNew(() => git.StandardOutput.ReadToEnd(), CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            git.WaitForExit();

            return (
                from c in output.Result.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                let split = c.Split('|')
                select new GitCommit
                {
                    Sha = split[0],
                    Time = DateTimeOffset.Parse(split[1], null),
                    AuthorName = split[2],
                    AuthorEmail = split[3],
                }).ToArray();
        }
    }
}
