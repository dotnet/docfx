// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build;

public partial class TestGitCommit
{
    public string Message { get; set; }

    public string Author { get; set; } = "docfx";

    public string Email { get; set; } = "docfx@microsoft.com";

    public DateTimeOffset Time { get; set; } = new DateTimeOffset(2018, 10, 30, 0, 0, 0, TimeSpan.Zero);

    public Dictionary<string, string> Files { get; } = new Dictionary<string, string>();
}
