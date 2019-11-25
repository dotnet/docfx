// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Docs.Build
{
    internal sealed class ContributionConfig
    {
        /// <summary>
        /// Specify the repository url for contribution
        /// </summary>
        public readonly string RepositoryUrl;

        /// <summary>
        /// Specify the repository branch for contribution
        /// </summary>
        public readonly string RepositoryBranch;

        /// <summary>
        /// The excluded contributors which you don't want to show
        /// </summary>
        public readonly HashSet<string> ExcludeContributors = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }
}
