// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Docs.Build
{
    internal class GitHubUserCacheModel
    {
        public GitHubUser[] Users { get; set; } = Array.Empty<GitHubUser>();
    }
}
