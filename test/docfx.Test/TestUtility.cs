// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using LibGit2Sharp;

namespace Microsoft.Docs.Build
{
    public partial class TestGitCommit
    {
        public string Message { get; set; }

        public string Author { get; set; } = "docfx";

        public string Email { get; set; } = "docfx@microsoft.com";

        public DateTimeOffset Time { get; set; } = new DateTimeOffset(2018, 10, 30, 0, 0, 0, TimeSpan.Zero);

        public Dictionary<string, string> Files { get; } = new Dictionary<string, string>();
    }

    internal partial class TestUtility
    {
        public static void CreateFiles(
            string path,
            IEnumerable<KeyValuePair<string, string>> files,
            IEnumerable<KeyValuePair<string, string>> variables = null)
        {
            foreach (var file in files)
            {
                var filePath = Path.GetFullPath(Path.Combine(path, file.Key));
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                File.WriteAllText(filePath, ApplyVariables(file.Value, variables)?.Replace("\r", "") ?? "");
            }
        }

        public static void CreateGitRepository(
            string path,
            TestGitCommit[] commits,
            string remote,
            string branch,
            IEnumerable<KeyValuePair<string, string>> variables = null)
        {
            Directory.CreateDirectory(path);

            if (!LibGit2Sharp.Repository.IsValid(path))
            {
                LibGit2Sharp.Repository.Init(path);
            }

            using (var repo = new LibGit2Sharp.Repository(path))
            {
                if (!string.IsNullOrEmpty(remote))
                {
                    repo.Network.Remotes.Update("origin", r => r.Url = remote);
                }

                var lastCommit = default(Commit);

                foreach (var commit in commits.Reverse())
                {
                    var commitIndex = 0;
                    var tree = new TreeDefinition();

                    foreach (var file in commit.Files)
                    {
                        var content = ApplyVariables(file.Value, variables)?.Replace("\r", "") ?? "";
                        var blob = repo.ObjectDatabase.CreateBlob(
                            new MemoryStream(Encoding.UTF8.GetBytes(content)));

                        tree.Add(file.Key, blob, Mode.NonExecutableFile);
                    }

                    var author = new Signature(commit.Author, commit.Email, commit.Time);
                    var currentCommit = repo.ObjectDatabase.CreateCommit(
                        author,
                        author,
                        commit.Message ?? $"Commit {commitIndex++}",
                        repo.ObjectDatabase.CreateTree(tree),
                        lastCommit != null ? new[] { lastCommit } : Array.Empty<Commit>(),
                        prettifyMessage: false);

                    lastCommit = currentCommit;
                }

                if (!string.IsNullOrEmpty(branch))
                {
                    Commands.Checkout(repo, repo.Branches.Add(branch, lastCommit, allowOverwrite: true));
                }
                else
                {
                    Commands.Checkout(repo, lastCommit);
                }
            }
        }

        private static string ApplyVariables(string value, IEnumerable<KeyValuePair<string, string>> variables)
        {
            if (variables != null && value != null)
            {
                foreach (var variable in variables)
                {
                    value = value.Replace($"{{{variable.Key}}}", variable.Value);
                }
            }
            return value;
        }
    }
}
