// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common.Git
{
    using System;

    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    [Serializable]
    public class GitDetail
    {
        /// <summary>
        /// Relative path of current file to the Git Root Directory
        /// </summary>
        [YamlMember(Alias = "path")]
        [JsonProperty("path")]
        public string RelativePath { get; set; }

        [YamlMember(Alias = "branch")]
        [JsonProperty("branch")]
        public string RemoteBranch { get; set; }

        [YamlMember(Alias = "repo")]
        [JsonProperty("repo")]
        public string RemoteRepositoryUrl { get; set; }

        // remove it to avoid config hash changed
        //[YamlMember(Alias = "commit")]
        //[JsonProperty("commit")]
        //public string CommitId { get; set; }

        // remove it to avoid config hash changed
        //[JsonProperty("key")]
        //[YamlMember(Alias = "key")]
        //public string Description { get; set; }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (this.GetType() != obj.GetType()) return false;

            return Equals(this.ToString(), obj.ToString());
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("branch: {0}, url: {1}, file: {2}", RemoteBranch, RemoteRepositoryUrl, RelativePath);
        }
    }
}