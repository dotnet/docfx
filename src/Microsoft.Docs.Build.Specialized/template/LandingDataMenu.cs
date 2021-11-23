// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build;

public sealed class LandingDataMenu
{
    public string? Title { get; set; }

    public LandingDataMenuItem[] Items { get; set; } = Array.Empty<LandingDataMenuItem>();
}
