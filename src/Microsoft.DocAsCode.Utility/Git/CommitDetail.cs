// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Utility.Git
{
    using System;
    using System.Collections.Concurrent;

    using GitSharp.Core;
    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    [Serializable]
    public sealed class CommitDetail
    {
        private static readonly ConcurrentDictionary<ObjectId, CommitDetail> _cache = new ConcurrentDictionary<ObjectId, CommitDetail>();
        private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        [YamlMember(Alias = "committer")]
        [JsonProperty("committer")]
        public UserInfo Committer { get; set; }

        [YamlMember(Alias = "author")]
        [JsonProperty("author")]
        public UserInfo Author { get; set; }

        [YamlMember(Alias = "id")]
        [JsonProperty("id")]
        public string CommitId { get; set; }

        [YamlMember(Alias = "message")]
        [JsonProperty("message")]
        public string ShortMessage { get; set; }

        internal static CommitDetail FromCommit(Commit commit)
        {
            var commitId = commit.CommitId;
            return _cache.GetOrAdd(commitId, (s) =>
            {
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
            });
        }
    }
}
