// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
    internal readonly struct PackageUrl
    {
        public readonly PackageType Type;

        public readonly string Path;

        public readonly string Remote;

        public readonly string Committish;

        public PackageUrl(string url)
            : this()
        {
            if (UrlUtility.IsHttp(url))
            {
                Type = PackageType.Git;
                (Remote, Committish, _) = UrlUtility.SplitGitUrl(url);
            }
            else
            {
                Type = PackageType.Folder;
                Path = url;
            }
        }

        public override string ToString()
        {
            switch (Type)
            {
                case PackageType.Folder:
                    return Path;

                case PackageType.Git:
                    return $"{Remote}:{Committish}";

                default:
                    return Type.ToString();
            }
        }
    }
}
