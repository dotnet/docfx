// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using LibGit2Sharp;
using Newtonsoft.Json.Linq;
using Yunit;

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
        public static void MakeDebugAssertThrowException()
        {
            // This only works for .NET core
            // https://github.com/dotnet/corefx/blob/master/src/Common/src/CoreLib/System/Diagnostics/Debug.cs
            // https://github.com/dotnet/corefx/blob/8dbeee99ce48a46c3cee9d1b765c3b31af94e172/src/System.Diagnostics.Debug/tests/DebugTests.cs
            var showDialogHook = typeof(Debug).GetField("s_ShowDialog", BindingFlags.Static | BindingFlags.NonPublic);
            showDialogHook?.SetValue(null, new Action<string, string, string, string>(Throw));

            void Throw(string stackTrace, string message, string detailMessage, string info)
            {
                throw new Exception($"Debug.Assert failed: {message} {detailMessage}\n{stackTrace}");
            }
        }

        public static void CreateFiles(
            string path,
            IEnumerable<KeyValuePair<string, string>> files,
            IEnumerable<KeyValuePair<string, string>> variables = null)
        {
            foreach (var file in files)
            {
                var filePath = Path.GetFullPath(Path.Combine(path, file.Key));
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                if (file.Key.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    CreateZipFile(file, filePath);
                }
                else
                {
                    File.WriteAllText(filePath, ApplyVariables(file.Value, variables)?.Replace("\r", "") ?? "");
                }
            }
        }

        private static void CreateZipFile(KeyValuePair<string, string> file, string filePath)
        {
            var token = YamlUtility.ToJToken(file.Value);
            if (token is JObject obj)
            {
                using var memoryStream = new MemoryStream();
                using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                {
                    foreach (JProperty child in obj.Children())
                    {
                        var entry = archive.CreateEntry(child.Name);

                        using var entryStream = entry.Open();
                        using var sw = new StreamWriter(entryStream);
                        sw.Write(child.Value);
                    }
                }

                using var fileStream = new FileStream(filePath, FileMode.Create);
                memoryStream.Seek(0, SeekOrigin.Begin);
                memoryStream.CopyTo(fileStream);
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

            using var repo = new LibGit2Sharp.Repository(path);
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

        public static IDisposable EnsureFilesNotChanged(string path)
        {
            var before = GetFileLastWriteTimes(path);

            return new Disposable(() =>
            {
                var after = GetFileLastWriteTimes(path);

                //new JsonDiff().Verify(before, after, "Input files changes");
            });

            Dictionary<string, DateTime> GetFileLastWriteTimes(string dir)
            {
                return new DirectoryInfo(dir)
                    .GetFiles("*", SearchOption.AllDirectories)
                    .Where(file => !file.FullName.Contains(".git"))
                    .ToDictionary(file => file.FullName, file => file.LastWriteTimeUtc);
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

        private class Disposable : IDisposable
        {
            private readonly Action _dispose;

            public Disposable(Action dispose) => _dispose = dispose;

            public void Dispose() => _dispose();
        }
    }
}
