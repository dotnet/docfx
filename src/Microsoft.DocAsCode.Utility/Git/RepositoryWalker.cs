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
        private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private readonly RevWalk _walker;
        private readonly RevWalk _assistantWalker;
        private readonly RevCommit _initCommit;
        private readonly object _locker = new object();

        public RepositoryWalker(CoreRepository repo)
        {
            if (repo == null)
            {
                throw new ArgumentNullException(nameof(repo));
            }

            _walker = new RevWalk(repo);
            _assistantWalker = new RevWalk(repo);
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
            for (var currentRev = _walker.next(); currentRev != null; currentRev = _walker.next())
            {
                var currentCommit = GetCommit(currentRev);
                var currentEntry = currentCommit.TreeEntry.FindBlobMember(path);
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
                    continue;
                }

                var parentCommit = GetCommitFromObjectId(currentCommit.Commit.ParentIds[0]);
                var parentEntry = parentCommit.TreeEntry.FindBlobMember(path);
                if (parentEntry == null || currentEntry.Id != parentEntry.Id)
                {
                    return currentCommit.Detail;
                }
            }

            return null;
        }

        public void Dispose()
        {
            _walker.Dispose();
        }

        private CommitWrapper GetCommit(RevCommit currentRev)
        {
            return Map.GetOrAdd(currentRev, (s) =>
            {
                var commit = currentRev.AsCommit(_walker);
                return new CommitWrapper
                {
                    Commit = commit,
                    Detail = ConvertToCommitDetail(commit),
                    TreeEntry = commit.TreeEntry,
                    ParentCount = commit.ParentIds.Length
                };
            });
        }

        private CommitWrapper GetCommitFromObjectId(ObjectId commitId)
        {
            return Map.GetOrAdd(commitId, (s) =>
            {
                var commit = GetRevCommit(commitId);
                return new CommitWrapper
                {
                    Commit = commit,
                    Detail = ConvertToCommitDetail(commit),
                    TreeEntry = commit.TreeEntry,
                    ParentCount = commit.ParentIds.Length
                };
            });
        }

        private Commit GetRevCommit(ObjectId commitId)
        {
            _assistantWalker.reset();
            _assistantWalker.markStart(new RevCommit(commitId));
            return _assistantWalker.next().AsCommit(_assistantWalker);
        }

        private static CommitDetail ConvertToCommitDetail(Commit commit)
        {
            var commitId = commit.CommitId;

            var author = commit.Author;
            var committer = commit.Committer;
            return new CommitDetail
            {
                CommitId = commitId.Name,
                Author = new UserInfo
                {
                    Name = author.Name,
                    Email = author.EmailAddress,
                    Date = Epoch.AddMilliseconds(author.When)
                },
                Committer = new UserInfo
                {
                    Name = committer.Name,
                    Email = committer.EmailAddress,
                    Date = Epoch.AddMilliseconds(committer.When)
                },
                ShortMessage = commit.Message,
            };
        }

        private sealed class CommitWrapper
        {
            public Commit Commit { get; set; }
            public CommitDetail Detail { get; set; }
            public Tree TreeEntry { get; set; }
            public int ParentCount { get; set; }
        }
    }
}
