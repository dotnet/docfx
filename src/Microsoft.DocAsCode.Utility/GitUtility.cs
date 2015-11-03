// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Utility
{
    using System;
    using System.IO;
    using GitSharp;

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

        [YamlIgnore]
        [JsonIgnore]
        //[YamlDotNet.Serialization.YamlMember(Alias = "local")]
        public string LocalWorkingDirectory { get; set; }

        [JsonProperty("key")]
        [YamlMember(Alias = "key")]
        public string Description { get; set; }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (this.GetType() != obj.GetType()) return false;

            return Equals(this.ToString(), obj.ToString());
        }

        public override int GetHashCode()
        {
            return this.ToString().GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("branch: {0}, url: {1}, local: {2}, desc: {3}, file: {4}", RemoteBranch, RemoteRepositoryUrl, LocalWorkingDirectory, Description, RelativePath);
        }
    }

    public static class GitUtility
    {
        /// <summary>
        /// TODO: only get GitDetail on Project level?
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static GitDetail GetGitDetail(string path)
        {
            GitDetail detail = null;
            if (string.IsNullOrEmpty(path)) return detail;
            try
            {
                var repoPath = Repository.FindRepository(path);
                if (string.IsNullOrEmpty(repoPath)) return detail;

                var repo = new Repository(repoPath);

                detail = new GitDetail();
                // Convert to forward slash
                detail.LocalWorkingDirectory = repo.WorkingDirectory.BackSlashToForwardSlash();
                if (repo.Head == null) return detail;
                
                var branch = repo.CurrentBranch;
                detail.RemoteRepositoryUrl = repo.Config["remote.origin.url"];
                detail.RemoteBranch = branch.Name;
                // detail.Description = repo.Head.CurrentCommit.ShortHash;
                detail.RelativePath = PathUtility.MakeRelativePath(Path.GetDirectoryName(repoPath), path);
            }
            catch (Exception)
            {
                // SWALLOW exception?
                // Console.Error.WriteLine(e.Message);
            }

            return detail;
        }
    }
}
