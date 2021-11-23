// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build;

internal class ModuleUnit
{
    public SourceInfo<string> Uid { get; init; } = new SourceInfo<string>("");

    public bool AzureSandbox { get; set; }

    public int DurationInMinutes { get; set; }

    public string? Title { get; set; }

    public string? Content { get; set; }
}
