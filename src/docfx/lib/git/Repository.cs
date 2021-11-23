// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build;

internal class Repository
{
    public string Url { get; }

    public string? Branch { get; }

    public string Commit { get; }

    public PathString Path { get; }

    private Repository(string url, string? branch, string commit, PathString path)
    {
        // remove user name, token and .git from url like https://xxxxx@dev.azure.com/xxxx.git
        Url = GitUtility.NormalizeGitUrl(url);
        Branch = branch;
        Commit = commit;
        Path = path;
    }

    /// <summary>
    /// Create repository from environment variable(url + branch), fallback to git info if they are not set
    /// </summary>
    public static Repository? Create(string path)
    {
        var repository = Create(path, EnvironmentVariable.RepositoryBranch, EnvironmentVariable.RepositoryUrl);
        if (repository != null)
        {
            Log.Write($"Repository {repository.Url}#{repository.Branch} at committish: {repository.Commit}");
        }
        return repository;
    }

    /// <summary>
    /// Repository's branch info should NOT depend on git, unless you are pretty sure about that
    /// Repository's url can also be overwritten
    /// </summary>
    public static Repository? Create(string path, string? branch, string? repoUrl = null)
    {
        var repoPath = GitUtility.FindRepository(System.IO.Path.GetFullPath(path));
        if (repoPath is null)
        {
            return null;
        }

        var (url, repoBranch, repoCommit) = GitUtility.GetRepoInfo(repoPath);
        if (repoCommit is null)
        {
            return null;
        }

        return new Repository(repoUrl ?? url ?? "", branch ?? repoBranch, repoCommit, new PathString(repoPath));
    }
}
