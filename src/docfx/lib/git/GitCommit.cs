// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Docs
{
    /// <summary>
    /// The git commit information
    /// </summary>
    internal class GitCommit
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

        /// <summary>
        /// Get the git commit string representation
        /// </summary>
        /// <returns>The git commit string representation</returns>
        public override string ToString() => $"{Sha}: {AuthorName}, {AuthorEmail}";
    }
}
