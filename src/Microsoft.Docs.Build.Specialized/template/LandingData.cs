// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build;

public class LandingData
{
    public string? Title { get; set; }

    public string? TitleSuffix { get; set; }

    public JObject? Metadata { get; set; }

    public LandingDataAbstract? Abstract { get; set; }

    public LandingDataSection[]? Sections { get; set; }

    public string? DocumentType { get; set; }
}
