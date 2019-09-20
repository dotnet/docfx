// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Microsoft.Docs.Build
{
    /// <summary>
    /// Define the location of a package.
    /// A package is any of the following:
    ///   a) A folder
    ///   b) A git url in the form of `{remote_url}#{commit-ish}`, when cloned, results in a)
    /// The commit-sh can be any tag, sha, or branch. The default commit-ish is master.
    /// </summary>
    [JsonConverter(typeof(ShortHandConverter))]
    internal class PackageUrl
    {
        private static StreamingContext s_streamingContext = default;

        [JsonIgnore]
        public PackageType Type { get; set; }

        public string Path { get; set; }

        public string Url { get; set; }

        public string Branch { get; set; } = "master";

        public PackageUrl()
        {
        }

        public PackageUrl(string url)
        {
            Url = url;
            OnDeserialized(s_streamingContext);
        }

        public PackageUrl(string remote, string branch)
        {
            Debug.Assert(remote != null);
            Debug.Assert(branch != null);

            Type = PackageType.Git;
            Url = remote;
            Branch = branch;
            Path = null;
        }

        public override string ToString()
        {
            switch (Type)
            {
                case PackageType.Folder:
                    return Path;

                case PackageType.Git:
                    return $"{Url}#{Branch}";

                default:
                    return $"{Url}, (type: {Type.ToString()})";
            }
        }

        private static (string remote, string refspec) SplitGitUrl(string remoteUrl)
        {
            Debug.Assert(!string.IsNullOrEmpty(remoteUrl));

            var (path, _, fragment) = UrlUtility.SplitUrl(remoteUrl);

            path = path.TrimEnd('/', '\\');
            var hasRefSpec = !string.IsNullOrEmpty(fragment) && fragment.Length > 1;

            return (path, hasRefSpec ? fragment.Substring(1) : default);
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            if (UrlUtility.IsHttp(Url))
            {
                Type = PackageType.Git;
                var (url, branch) = SplitGitUrl(Url);
                Url = url;
                Branch = branch ?? Branch;
            }
            else
            {
                Type = PackageType.Folder;
                Path = Url;
            }
        }
    }
}
