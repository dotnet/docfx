// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
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
    internal class PackageUrl : IEquatable<PackageUrl>
    {
        public PackageType Type { get; set; }

        public string Path { get; set; }

        public string RemoteUrl { get; set; }

        public string Branch { get; set; } = "master";

        public PackageUrl()
        {
        }

        public PackageUrl(string url)
        {
            if (UrlUtility.IsHttp(url))
            {
                Type = PackageType.Git;
                (RemoteUrl, Branch) = SplitGitUrl(url);
            }
            else
            {
                Type = PackageType.Folder;
                Path = url;
            }
        }

        public PackageUrl(string remote, string branch)
        {
            Debug.Assert(remote != null);
            Debug.Assert(branch != null);

            Type = PackageType.Git;
            RemoteUrl = remote;
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
                    return $"{RemoteUrl}#{Branch}";

                default:
                    return Type.ToString();
            }
        }

        private static (string remote, string refspec) SplitGitUrl(string remoteUrl)
        {
            Debug.Assert(!string.IsNullOrEmpty(remoteUrl));

            var (path, _, fragment) = UrlUtility.SplitUrl(remoteUrl);

            path = path.TrimEnd('/', '\\');
            var hasRefSpec = !string.IsNullOrEmpty(fragment) && fragment.Length > 1;
            var refspec = hasRefSpec ? fragment.Substring(1) : "master";

            return (path, refspec);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Type, Path, RemoteUrl, Branch);
        }

        public bool Equals(PackageUrl other)
        {
            if (other == null)
            {
                return false;
            }

            return Type == other.Type &&
                   string.Equals(Path, other.Path, PathUtility.PathComparison) &&
                   RemoteUrl == other.RemoteUrl &&
                   Branch == other.Branch;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as PackageUrl);
        }
    }
}
