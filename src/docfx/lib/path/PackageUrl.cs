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
    internal readonly struct PackageUrl : IEquatable<PackageUrl>, IComparable<PackageUrl>
    {
        public readonly PackageType Type;

        public readonly string Path;

        public readonly string Remote;

        public readonly string Branch;

        public readonly bool HasRefSpec;

        public PackageUrl(string url)
            : this()
        {
            if (UrlUtility.IsHttp(url))
            {
                Type = PackageType.Git;
                (Remote, Branch, HasRefSpec) = SplitGitUrl(url);
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
            Remote = remote;
            Branch = branch;
            HasRefSpec = true;
            Path = null;
        }

        public override string ToString()
        {
            switch (Type)
            {
                case PackageType.Folder:
                    return Path;

                case PackageType.Git:
                    return $"{Remote}#{Branch}";

                default:
                    return Type.ToString();
            }
        }

        public override int GetHashCode()
        {
            switch (Type)
            {
                case PackageType.Folder:
                    return Path.GetHashCode();

                case PackageType.Git:
                    return HashCode.Combine(Remote.GetHashCode(), Branch.GetHashCode());

                default:
                    return Type.GetHashCode();
            }
        }

        public int CompareTo(PackageUrl other)
        {
            var result = Type.CompareTo(other.Type);
            switch (Type)
            {
                case PackageType.Folder:
                    result = string.Compare(Path, other.Path, PathUtility.PathComparison);
                    break;

                case PackageType.Git:
                    result = Remote.CompareTo(other.Remote);
                    if (result == 0)
                        result = Branch.CompareTo(other.Branch);

                    break;

                default:
                    throw new NotSupportedException($"{Type} is not supported");
            }

            return result;
        }

        public static bool operator ==(PackageUrl a, PackageUrl b) => Equals(a, b);

        public static bool operator !=(PackageUrl a, PackageUrl b) => !Equals(a, b);

        public override bool Equals(object obj)
        {
            return obj is PackageUrl && Equals((PackageUrl)obj);
        }

        public bool Equals(PackageUrl other)
        {
            if (other.Type != Type)
            {
                return false;
            }

            switch (Type)
            {
                case PackageType.Folder:
                    return string.Equals(Path, other.Path, PathUtility.PathComparison);

                case PackageType.Git:
                    return string.Equals(Remote, other.Remote) && string.Equals(Branch, other.Branch);

                default:
                    throw new NotSupportedException($"{Type} is not supported");
            }
        }

        private static (string remote, string refspec, bool hasRefSpec) SplitGitUrl(string remoteUrl)
        {
            Debug.Assert(!string.IsNullOrEmpty(remoteUrl));

            var (path, _, fragment) = UrlUtility.SplitUrl(remoteUrl);

            path = path.TrimEnd('/', '\\');
            var hasRefSpec = !string.IsNullOrEmpty(fragment) && fragment.Length > 1;
            var refspec = hasRefSpec ? fragment.Substring(1) : "master";

            return (path, refspec, hasRefSpec);
        }
    }
}
