// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Utility.Git
{
    using System;
    using System.Collections.Concurrent;

    using GitSharp.Core;
    using GitSharp.Core.RevWalk;

    using CoreRepository = GitSharp.Core.Repository;

    internal sealed class RepositoryWalker : IDisposable
    {
        private static readonly ConcurrentDictionary<ObjectId, CommitWrapper> Map = new ConcurrentDictionary<ObjectId, CommitWrapper>();
        private readonly RevWalk _walker;
        private readonly RevCommit _initCommit;
        private readonly object _locker = new object();

        public RepositoryWalker(CoreRepository repo)
        {
            if (repo == null)
            {
                throw new ArgumentNullException(nameof(repo));
            }

            _walker = new RevWalk(repo);
            _initCommit = new RevCommit(repo.Head.ObjectId);

        }

        public CommitDetail GetCommitDetail(string path)
        {
            lock (_locker)
            {
                return GetCommitDetailNoLock(path);
            }
        }

        private CommitDetail GetCommitDetailNoLock(string path)
        {
            // REset must be called according to https://github.com/henon/GitSharp/blob/master/GitSharp.Core/RevWalk/RevWalk.cs#L55
            _walker.reset();
            _walker.markStart(_initCommit);
            var currentRev = _walker.next(); // Use next to fetch the actual RevCommit
            while (currentRev != null)
            {
                var currentCommit = Map.GetOrAdd(currentRev, (s) =>
                {
                    var commit = currentRev.AsCommit(_walker);
                    return new CommitWrapper
                    {
                        Detail = CommitDetail.FromCommit(commit),
                        TreeEntry = commit.TreeEntry,
                        ParentCount = commit.ParentIds.Length
                    };
                });

                currentRev = _walker.next();
                var entry = currentCommit.TreeEntry.FindBlobMember(path);
                if (entry == null)
                {
                    return null;
                }

                if (currentRev == null || currentCommit.ParentCount == 0)
                {
                    return currentCommit.Detail;
                }

                if (currentCommit.ParentCount > 1)
                {
                    continue;
                }

                var parentCommit = Map.GetOrAdd(currentRev, (s) =>
                {
                    var commit = currentRev.AsCommit(_walker);
                    return new CommitWrapper
                    {
                        Detail = CommitDetail.FromCommit(commit),
                        TreeEntry = commit.TreeEntry,
                        ParentCount = commit.ParentIds.Length
                    };
                });
                var parentEntry = parentCommit.TreeEntry.FindBlobMember(path);
                if (parentEntry == null)
                {
                    return currentCommit.Detail;
                }

                if (entry.Id != parentEntry.Id)
                {
                    return parentCommit.Detail;
                }
            }

            return null;
        }

        public void Dispose()
        {
            _walker.Dispose();
        }

        private sealed class CommitWrapper
        {
            public CommitDetail Detail { get; set; }
            public Tree TreeEntry { get; set; }
            public int ParentCount { get; set; }
        }
    }
}
