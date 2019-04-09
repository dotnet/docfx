// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Docs.Build
{
    internal sealed class ContributionConfig
    {
        /// <summary>
        /// Specify the repository url for contribution:
        /// <protocol>://<hostname>[:<port>][:][/]<path>[#<branch>]
        /// Fallback to git origin if not set.
        /// </summary>
        public readonly string Repository;

        /// <summary>
        /// The address of commit time history file, which contains the time each commit being pushed.
        /// It should be an absolute url or a relative path
        /// </summary>
        public readonly SourceInfo<string> GitCommitsTime = new SourceInfo<string>(string.Empty, null, default);

        /// <summary>
        /// The excluded contributors which you don't want to show
        /// </summary>
        public readonly HashSet<string> ExcludedContributors = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }
}
