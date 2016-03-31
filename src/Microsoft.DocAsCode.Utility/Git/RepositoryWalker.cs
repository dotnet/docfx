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

        private CommitWrapper GetCommit(RevCommit currentRev)
        {
            return Map.GetOrAdd(currentRev, (s) =>
            {
                var commit = currentRev.AsCommit(_walker);
                return new CommitWrapper
                {
                    Detail = CommitDetail.FromCommit(commit),
                    TreeEntry = commit.TreeEntry,
                    ParentCount = commit.ParentIds.Length
                };
            });
        }

        private CommitDetail GetCommitDetailNoLock(string path)
        {
            // REset must be called according to https://github.com/henon/GitSharp/blob/master/GitSharp.Core/RevWalk/RevWalk.cs#L55
            _walker.reset();
            _walker.markStart(_initCommit);
            var currentRev = _walker.next(); // Use next to fetch the actual RevCommit
            var currentCommit = GetCommit(currentRev);
            var currentEntry = currentCommit.TreeEntry.FindBlobMember(path);
            while (currentRev != null)
            {
                if (currentEntry == null)
                {
                    return null;
                }

                if (currentCommit.ParentCount == 0)
                {
                    return currentCommit.Detail;
                }

                if (currentCommit.ParentCount > 1)
                {
                    // For commit that contains multiple parents, e.g. when merge takes place, find the previous commit with only one parent that contains the same file
                    var parentRev = _walker.next();
                    TreeEntry parentEntry = null;
                    CommitWrapper parentCommit = null;
                    while (parentRev != null)
                    {
                        parentCommit = GetCommit(parentRev);
                        parentEntry = parentCommit.TreeEntry.FindBlobMember(path);
                        if (parentEntry == null || parentEntry.Id != currentEntry.Id)
                        {
                            parentRev = _walker.next();
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (parentRev == null)
                    {
                        return currentCommit.Detail;
                    }

                    currentCommit = parentCommit;
                    currentRev = parentRev;
                    currentEntry = parentEntry;
                }
                else
                {
                    var parentRev = _walker.next();

                    var parentCommit = GetCommit(parentRev);
                    var parentEntry = parentCommit.TreeEntry.FindBlobMember(path);
                    if (parentEntry == null || currentEntry.Id != parentEntry.Id)
                    {
                        return currentCommit.Detail;
                    }

                    currentCommit = parentCommit;
                    currentRev = parentRev;
                    currentEntry = parentEntry;
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
