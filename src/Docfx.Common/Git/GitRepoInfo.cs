﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.Common.Git;

public class GitRepoInfo
{
    public RepoType RepoType { get; set; }

    public string RepoAccount { get; set; }

    public string RepoName { get; set; }

    public string RepoProject { get; set; }

    public Uri NormalizedRepoUrl { get; set; }
}
