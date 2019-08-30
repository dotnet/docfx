// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Docs.Build
{
    /// <summary>
    /// The git commit information
    /// </summary>
    internal class GitCommit : IComparable<GitCommit>
    {
        /// <summary>
        /// Gets or sets the git commmit author name
        /// </summary>
        public string AuthorName { get; set; }

        /// <summary>
        /// Gets or sets the git commit author email
        /// </summary>
        public string AuthorEmail { get; set; }

        /// <summary>
        /// Gets or sets the git commit sha
        /// </summary>
        public string Sha { get; set; }

        /// <summary>
        /// Gets or sets the git commit time
        /// </summary>
        public DateTimeOffset Time { get; set; }

        public int CompareTo(GitCommit other) => other.Time.CompareTo(Time);
    }
}
