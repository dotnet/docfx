// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build;

public sealed class LandingDataAbstract
{
    public string? Description { get; set; }

    public LandingDataAside? Aside { get; set; }

    public LandingDataMenu? Menu { get; set; }
}
