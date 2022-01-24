// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build;

internal class Achievement
{
    public SourceInfo<string> Uid { get; init; } = new SourceInfo<string>("");

    public AchievementType Type { get; set; }

    public string? Title { get; set; }

    public string? Summary { get; set; }
}
