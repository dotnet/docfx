// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using LibGit2Sharp;
using Newtonsoft.Json.Linq;
using Yunit;

namespace Microsoft.Docs.Build;

internal partial class TestUtility
{
    public static void MakeDebugAssertThrowException()
    {
        // This only works for .NET core
        // https://github.com/dotnet/corefx/blob/master/src/Common/src/CoreLib/System/Diagnostics/Debug.cs
        // https://github.com/dotnet/corefx/blob/8dbeee99ce48a46c3cee9d1b765c3b31af94e172/src/System.Diagnostics.Debug/tests/DebugTests.cs
        var showDialogHook = typeof(Debug).GetField("s_ShowDialog", BindingFlags.Static | BindingFlags.NonPublic);
        showDialogHook?.SetValue(null, new Action<string, string, string, string>(Throw));

        static void Throw(string stackTrace, string message, string detailMessage, string info)
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
            Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? ".");
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

    public static Package CreateInputDirectoryPackage(
        string docsetPath,
        DocfxTestSpec spec,
        IEnumerable<KeyValuePair<string, string>> variables = null)
    {
        Directory.CreateDirectory(docsetPath);
        var usePhysicalInput = spec.UsePhysicalInput
            || spec.Repos.Count != 0
            || spec.Inputs.Any(entry => entry.Key.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)
                || entry.Key.EndsWith("rules.json", StringComparison.OrdinalIgnoreCase)
                || entry.Key.EndsWith("allowlist.json", StringComparison.OrdinalIgnoreCase)
                || entry.Key.StartsWith("_themes", StringComparison.OrdinalIgnoreCase));

        if (usePhysicalInput)
        {
            return new LocalPackage(docsetPath);
        }

        var memoryPackage = new MemoryPackage(docsetPath);
        foreach (var file in spec.Inputs)
        {
            memoryPackage.AddOrUpdate(new PathString(file.Key), ApplyVariables(file.Value, variables)?.Replace("\r", "") ?? string.Empty);
        }

        return memoryPackage;
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
        var commitIndex = 0;

        foreach (var commit in commits.Reverse())
        {
            var tree = new TreeDefinition();

            foreach (var file in commit.Files)
            {
                var content = ApplyVariables(file.Value, variables)?.Replace("\r", "") ?? "";
                using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
                var blob = repo.ObjectDatabase.CreateBlob(stream);

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

    public static IDisposable EnsureFilesNotChanged(string path, bool skipInputCheck)
    {
        var before = GetFileLastWriteTimes(path);

        return new DelegatingDisposable(() =>
        {
            if (!skipInputCheck)
            {
                var after = GetFileLastWriteTimes(path);
                new JsonDiff().Verify(before, after, "Input files changes");
            }
        });

        static Dictionary<string, DateTime> GetFileLastWriteTimes(string dir)
        {
            return new DirectoryInfo(dir)
                .GetFiles("*", SearchOption.AllDirectories)
                .Where(file => !file.FullName.Contains(".git"))
                .ToDictionary(file => file.FullName, file => file.LastWriteTimeUtc);
        }
    }

    public static JToken ApplyVariables(JToken value, IEnumerable<KeyValuePair<string, string>> variables)
    {
        if (variables != null && value != null)
        {
            if (value is JValue && value.Type == JTokenType.String)
            {
                return ApplyVariables((string)value, variables);
            }
            else if (value is JArray array)
            {
                var newArray = new JArray();
                foreach (var item in array)
                {
                    newArray.Add(ApplyVariables(item, variables));
                }
                return newArray;
            }
            else if (value is JObject obj)
            {
                var newObj = new JObject();
                foreach (var (key, val) in obj)
                {
                    newObj[key] = ApplyVariables(val, variables);
                }
                return newObj;
            }
        }
        return value;
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

    private static void CreateZipFile(KeyValuePair<string, string> file, string filePath)
    {
        var token = YamlUtility.Parse(ErrorBuilder.Null, file.Value, null);
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
}
