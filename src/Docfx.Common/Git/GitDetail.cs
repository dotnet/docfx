// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace Docfx.Common.Git;

public record GitDetail
{
    /// <summary>
    /// Relative path of current file to the Git Root Directory
    /// </summary>
    [YamlMember(Alias = "path")]
    [JsonPropertyName("path")]
    public string Path { get; set; }

    [YamlMember(Alias = "branch")]
    [JsonPropertyName("branch")]
    public string Branch { get; set; }

    [YamlMember(Alias = "repo")]
    [JsonPropertyName("repo")]
    public string Repo { get; set; }
}
