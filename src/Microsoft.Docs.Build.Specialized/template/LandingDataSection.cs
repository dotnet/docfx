// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build;

public sealed class LandingDataSection
{
    public string? Title { get; set; }

    public string? Type { get; set; }

    public string? Text { get; set; }

    public LandingDataItem[]? Items { get; set; }
}
