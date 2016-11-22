// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common.Git
{
    using System;

    [Serializable]
    public class GitRepoInfo
    {
        public string RepoRootPath { get; set; }

        public string LocalBranch { get; set; }

        public string RemoteBranch { get; set; }

        public string RemoteOriginUrl { get; set; }

        public string RawRemoteOriginUrl { get; set; }

        public RepoType Type { get; set; }

        public string Account { get; set; }

        public string RemoteRepoName { get; set; }

        public string RemoteHeadCommitId { get; set; }

        public string LocalHeadCommitId { get; set; }
    }
}