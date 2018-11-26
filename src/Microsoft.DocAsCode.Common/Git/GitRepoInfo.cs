// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common.Git
{
    using System;

    [Serializable]
    public class GitRepoInfo
    {
        public RepoType RepoType { get; set; }

        public string RepoAccount { get; set; }

        public string RepoName { get; set; }

        public string RepoProject { get; set; }

        public Uri NormalizedRepoUrl { get; set; }

        public string RepoRootPath { get; set; }

        public string LocalBranch { get; set; }

        public string RemoteBranch { get; set; }

        public string RemoteOriginUrl { get; set; }

        public string RemoteHeadCommitId { get; set; }

        public string LocalHeadCommitId { get; set; }
    }
}