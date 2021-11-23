// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build;

public sealed class LandingDataItem
{
    public LandingDataType Type { get; set; }

    public string? Text { get; set; }

    public string? Title { get; set; }

    public string? Style { get; set; }

    public string? ClassName { get; set; }

    public LandingDataItem[] Items { get; set; } = Array.Empty<LandingDataItem>();

    public string? Content { get; set; }

    public JToken? Columns { get; set; }

    public LandingDataRow[]? Rows { get; set; }

    public LandingDataImage? Image { get; set; }

    public string? Html { get; set; }

    public string? Href { get; set; }
}
