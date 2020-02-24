// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

#nullable enable

namespace Microsoft.Docs.Build
{
    internal sealed class ContributionConfig
    {
        /// <summary>
        /// Specify the repository url for contribution
        /// </summary>
        public string? RepositoryUrl { get; private set; }

        /// <summary>
        /// Specify the repository branch for contribution
        /// </summary>
        public string? RepositoryBranch { get; private set; }

        /// <summary>
        /// The excluded contributors which you don't want to show
        /// </summary>
        public HashSet<string> ExcludeContributors { get; private set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }
}
