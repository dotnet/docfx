// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build;

/// <summary>
/// The git commit information
/// </summary>
internal class GitCommit
{
    /// <summary>
    /// Gets or sets the git commit author name
    /// </summary>
    public string AuthorName { get; }

    /// <summary>
    /// Gets or sets the git commit author email
    /// </summary>
    public string AuthorEmail { get; }

    /// <summary>
    /// Gets or sets the git commit sha
    /// </summary>
    public string Sha { get; }

    /// <summary>
    /// Gets or sets the git commit time
    /// </summary>
    public DateTimeOffset Time { get; }

    public GitCommit(string authorName, string authorEmail, string sha, DateTimeOffset time)
    {
        AuthorName = authorName;
        AuthorEmail = authorEmail;
        Sha = sha;
        Time = time;
    }
}
