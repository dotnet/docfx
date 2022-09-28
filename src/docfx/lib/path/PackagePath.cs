// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Microsoft.Docs.Build;

/// <summary>
/// Define the location of a package.
///
/// A package is a collection of files, it can be any of the following:
///   a) A folder
///   b) A git url in the form of `{remote_url}#{commit-ish}`, when cloned, results in a)
/// The commit-ish can be any tag, sha, or branch. The default commit-ish is master.
/// </summary>
[JsonConverter(typeof(ShortHandConverter))]
internal record PackagePath
{
    [JsonIgnore]
    public PackageType Type { get; private set; }

    public string Path { get; private set; } = "";

    public string Url { get; private set; } = "";

    public string Branch { get; private set; } = "main";

    public bool IsMainOrMaster => Branch == "main" || Branch == "master";

    public bool IsMainOrMasterOrDefault => Branch == "main" || Branch == "master" || Branch == "";

    public PackagePath()
    {
    }

    public PackagePath(string value)
    {
        if (UrlUtility.IsHttp(value))
        {
            if (value.StartsWith("https://static.docs.com/ui/"))
            {
                Type = PackageType.PublicTemplate;
                Url = value;
            }
            else
            {
                Type = PackageType.Git;
                (Url, Branch) = SplitGitUrl(value);
            }
        }
        else
        {
            Type = PackageType.Folder;
            Path = value;
        }
    }

    public PackagePath(string url, string? branch)
    {
        Type = PackageType.Git;
        Url = url;
        Branch = branch ?? "main";
    }

    public override string? ToString() => Type switch
    {
        PackageType.Folder => Path,
        PackageType.Git => $"{Url}#{Branch}",
        _ => $"{Url}, (type: {Type})",
    };

    private static (string url, string refspec) SplitGitUrl(string repositoryUrl)
    {
        var (path, _, fragment) = UrlUtility.SplitUrl(repositoryUrl);

        path = path.TrimEnd('/', '\\');
        var hasRefSpec = !string.IsNullOrEmpty(fragment) && fragment.Length > 1;

        return (path, hasRefSpec ? fragment[1..] : "main");
    }

    [OnDeserialized]
    private void OnDeserialized(StreamingContext context)
    {
        if (!string.IsNullOrEmpty(Url))
        {
            Type = PackageType.Git;

            // Explicitly reset path here,
            // we might want to represent a subfolder inside a repository by setting both url and path,
            // but for now it is not supported.
            Path = "";
        }
        else if (Path.Length > 0)
        {
            Type = PackageType.Folder;
        }
    }
}
