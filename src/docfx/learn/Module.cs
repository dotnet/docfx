// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build;

internal class Module
{
    public SourceInfo<string> Uid { get; init; } = new SourceInfo<string>("");

    public SourceInfo<string>[]? Units { get; set; }

    public Badge? Badge { get; set; }

    public SourceInfo<string> Achievement { get; init; } = new SourceInfo<string>("");

    public string? Title { get; set; }

    public string? Summary { get; set; }

    public string[]? Roles { get; set; }

    public string[]? Products { get; set; }

    public string? Prerequisites { get; set; }

    public string[]? Levels { get; set; }
}
