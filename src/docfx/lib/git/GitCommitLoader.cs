// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Collections.Extensions;

using static Microsoft.Docs.Build.LibGit2;

namespace Microsoft.Docs.Build;

internal sealed class GitCommitLoader : IDisposable
{
    private static readonly DictionarySlim<uint, Tree> s_emptyTree = new();

    private readonly ErrorBuilder _errors;
    private readonly Repository _repository;
    private readonly Lazy<GitCommitCache> _commitCache;

    // Commit history and a lookup table from commit hash to commit.
    // Use `long` to represent SHA2 git hashes for more efficient lookup and smaller size.
    private readonly ConcurrentDictionary<string, Lazy<(Commit[], Dictionary<long, Commit>)>> _commits = new();

    // A giant memory cache of git tree. Key is the `long` form of SHA2 tree hash, value is a string id to git SHA2 hash.
    private readonly ConcurrentDictionary<long, Tree> _treeCache = new();

    private readonly ConcurrentDictionary<(string, string?), GitCommit[]> _commitHistoryCache = new();

    private readonly IntPtr _repo;
    private int _isDisposed;
    private Task? _warmup;

    public GitCommitLoader(ErrorBuilder errors, Repository repository, string cacheFilePath)
    {
        if (git_repository_open(out _repo, repository.Path) != 0)
        {
            throw new ArgumentException($"Invalid git repo {repository.Path}");
        }

        _errors = errors;
        _repository = repository;
        _commitCache = new(() => new GitCommitCache(cacheFilePath));
    }

    public void WarmUp()
    {
        _warmup = Task.Run(() => _commits.GetOrAdd("", key => new(() => LoadCommits(key))).Value);
    }

    public GitCommit[] GetCommitHistory(string file, string? committish = null)
    {
        Debug.Assert(!file.Contains('\\'));

        return _commitHistoryCache.GetOrAdd((file, committish), GetCommitHistoryCore);
    }

    private GitCommit[] GetCommitHistoryCore((string, string?) key)
    {
        var (file, committish) = key;
        var commitCache = _commitCache.Value.ForFile(file);

        lock (commitCache)
        {
            return GetCommitHistoryCore(commitCache, file, committish);
        }
    }

    private GitCommit[] GetCommitHistoryCore(GitCommitCache.FileCommitCache commitCache, string file, string? committish = null)
    {
        const int MaxParentBlob = 32;

        var (commits, commitsById) = _commits.GetOrAdd(
            committish ?? "",
            key => new(() => LoadCommits(key))).Value;

        if (commits.Length <= 0)
        {
            return Array.Empty<GitCommit>();
        }

        var searchSteps = 0;
        var updateCache = true;
        var result = new List<Commit>();
        var parentBlobs = new long[MaxParentBlob];
        var pathSegments = Array.ConvertAll(file.Split('/'), path => HashUtility.GetFnv1A32Hash(Encoding.UTF8.GetBytes(path)));

        var headCommit = commits[0];
        var headBlob = GetBlob(commits[0].Tree, pathSegments);
        var commitsToFollow = new List<(Commit commit, long blob)> { (headCommit, headBlob) };

        // `commits` is the commit history for the current branch,
        // the commit history for a file is always a subset of commit history of a branch with the same order.
        // Reusing a single branch commit history is a performance optimization.
        foreach (var commit in commits)
        {
            // Find and remove if this commit should be followed by the tree traversal.
            if (!TryRemoveCommit(commit, commitsToFollow, out var blob))
            {
                continue;
            }

            searchSteps++;

            // Lookup and use cached commit history ONLY if there are no other commits to follow
            if (commitsToFollow.Count == 0 && commitCache.TryGetCommits(commit.Id.a, blob, out var commitIds))
            {
                // Only update cache when the cached result has changed.
                updateCache = result.Count != 0;

                for (var i = 0; i < commitIds.Length; i++)
                {
                    result.Add(commitsById[commitIds[i]]);
                }
                break;
            }

            var singleParent = false;
            var parentCount = Math.Min(MaxParentBlob, commit.Parents.Length);

            for (var i = 0; i < parentCount; i++)
            {
                parentBlobs[i] = GetBlob(commit.Parents[i].Tree, pathSegments);
                if (parentBlobs[i] == blob)
                {
                    // and it was TREESAME to one parent, follow only that parent.
                    // (Even if there are several TREESAME parents, follow only one of them.)
                    commitsToFollow.Add((commit.Parents[i], blob));
                    singleParent = true;
                    break;
                }
            }

            if (!singleParent)
            {
                // Otherwise, follow all parents.
                for (var i = 0; i < parentCount; i++)
                {
                    commitsToFollow.Add((commit.Parents[i], parentBlobs[i]));
                }
            }

            if ((parentCount == 0 && blob != 0) || (parentCount > 0 && !singleParent))
            {
                result.Add(commit);
            }
        }

        // Only update commit cache if the search takes significant amount of effort
        if (updateCache && searchSteps > 100)
        {
            commitCache.SetCommits(headCommit.Id.a, headBlob, result.Select(c => c.Id.a).ToArray());
        }

        return result.Select(c => c.GitCommit).ToArray();
    }

    private static bool TryRemoveCommit(Commit commit, List<(Commit commit, long blob)> commitsToFollow, out long blob)
    {
        blob = 0L;
        for (var i = 0; i < commitsToFollow.Count; i++)
        {
            var commitToCheck = commitsToFollow[i];
            if (commitToCheck.commit == commit)
            {
                blob = commitToCheck.blob;
                commitsToFollow.RemoveAt(i);
                return true;
            }
        }
        return false;
    }

    public void Save()
    {
        if (_commitCache.IsValueCreated)
        {
            _commitCache.Value.Save();
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _isDisposed, 1) == 0)
        {
            _warmup?.GetAwaiter().GetResult();
            git_repository_free(_repo);
            GC.SuppressFinalize(this);
        }
    }

    ~GitCommitLoader()
    {
        Dispose();
    }

    private unsafe (Commit[], Dictionary<long, Commit>) LoadCommits(string? committish = null)
    {
        using (Progress.Start("Loading git commits"))
        {
            var commitIds = LoadCommitIds(committish);
            var commits = new Commit[commitIds.Count];

            // Stop warm up if build completes before warm up finished
            if (_isDisposed == 1)
            {
                return (Array.Empty<Commit>(), new());
            }

            // Build commit tree
            Parallel.For(0, commits.Length, i =>
            {
                var commitId = commitIds[i];
                git_object_lookup(out var commit, _repo, &commitId, 1 /* GIT_OBJ_COMMIT */);

                var author = git_commit_author(commit);
                var parentCount = git_commit_parentcount(commit);
                var parents = new git_oid[parentCount];

                for (var n = 0; n < parentCount; n++)
                {
                    parents[n] = *git_commit_parent_id(commit, n);
                }

                var gitCommit = new GitCommit(
                    Marshal.PtrToStringUTF8(author->name) ?? "",
                    Marshal.PtrToStringUTF8(author->email) ?? "",
                    commitId.ToString(),
                    new git_time { time = git_commit_time(commit), offset = git_commit_time_offset(commit) }.ToDateTimeOffset());

                var treeId = *git_commit_tree_id(commit);
                var tree = new Tree { Id = treeId };
                var item = new Commit(commitId, parents, tree, gitCommit);
                git_object_free(commit);

                commits[i] = item;
            });

            var commitsById = commits.ToDictionary(c => c.Id.a);

            // build parent indices
            Parallel.ForEach(commits, commit =>
            {
                for (var i = 0; i < commit.ParentIds.Length; i++)
                {
                    commit.Parents[i] = commitsById[commit.ParentIds[i].a];
                }
                commit.ParentIds = Array.Empty<git_oid>();
            });

            return (commits, commitsById);
        }
    }

    private unsafe List<git_oid> LoadCommitIds(string? committish)
    {
        if (string.IsNullOrEmpty(committish))
        {
            committish = _repository.Commit;
        }

        var headCommit = IntPtr.Zero;
        foreach (var branch in GitUtility.GetFallbackBranch(committish))
        {
            if (git_revparse_single(out headCommit, _repo, branch) == 0)
            {
                break;
            }
        }

        if (headCommit == IntPtr.Zero)
        {
            throw Errors.Config.CommittishNotFound(_repository.Url, committish).ToException();
        }

        // walk commit list
        git_revwalk_new(out var walk, _repo);
        git_revwalk_sorting(walk, (1 << 0) | (1 << 1) /* GIT_SORT_TOPOLOGICAL | GIT_SORT_TIME */);

        var lastCommitId = *git_object_id(headCommit);
        git_revwalk_push(walk, &lastCommitId);
        git_object_free(headCommit);

        var commitIds = new List<git_oid>();

        while (true)
        {
            // Stop warm up if build completes before warm up finished
            if (_isDisposed == 1)
            {
                break;
            }

            var error = git_revwalk_next(out var commitId, walk);
            if (error == -31 /* GIT_ITEROVER */)
            {
                break;
            }

            // https://github.com/libgit2/libgit2sharp/issues/1351
            if (error != 0 /* GIT_ENOTFOUND */)
            {
                _errors.Add(Errors.System.GitCloneIncomplete(_repository.Path));
                break;
            }

            commitIds.Add(commitId);
            lastCommitId = commitId;
        }

        git_revwalk_free(walk);

        return commitIds;
    }

    private long GetBlob(Tree tree, uint[] pathSegments)
    {
        var node = tree;

        for (var i = 0; i < pathSegments.Length; i++)
        {
            var children = node.Children ?? LoadChildren(node);
            if (children.Count == 0 || !children.TryGetValue(pathSegments[i], out node))
            {
                return default;
            }
        }

        return node.Id.a;
    }

    private unsafe DictionarySlim<uint, Tree> LoadChildren(Tree tree)
    {
        var treeId = tree.Id;
        if (git_object_lookup(out var pTree, _repo, &treeId, 2 /* GIT_OBJ_TREE */) != 0)
        {
            return tree.Children = s_emptyTree;
        }

        var n = (int)git_tree_entrycount(pTree);
        var children = new DictionarySlim<uint, Tree>(n);

        for (var i = 0; i < n; i++)
        {
            var entry = git_tree_entry_byindex(pTree, (IntPtr)i);
            var entryId = *git_tree_entry_id(entry);
            var pName = git_tree_entry_name(entry);
            var pNameEnd = pName;
            while (*pNameEnd != 0)
            {
                pNameEnd++;
            }

            var name = new ReadOnlySpan<byte>(pName, (int)(pNameEnd - pName));

            children.GetOrAddValueRef(HashUtility.GetFnv1A32Hash(name)) = _treeCache.GetOrAdd(entryId.a, (_, id) => new Tree { Id = id }, entryId);
        }

        git_object_free(pTree);

        return tree.Children = children;
    }

    private class Commit
    {
        public git_oid Id { get; }

        public Tree Tree { get; }

        public GitCommit GitCommit { get; }

        public Commit[] Parents { get; }

        public git_oid[] ParentIds { get; set; }

        public Commit(git_oid id, git_oid[] parentIds, Tree tree, GitCommit gitCommit)
        {
            Id = id;
            ParentIds = parentIds;
            Tree = tree;
            GitCommit = gitCommit;
            Parents = new Commit[parentIds.Length];
        }
    }

    private class Tree
    {
        public git_oid Id { get; set; }

        public DictionarySlim<uint, Tree>? Children { get; set; }
    }
}
