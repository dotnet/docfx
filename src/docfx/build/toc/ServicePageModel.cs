// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build;

internal class ServicePageModel
{
    public SourceInfo<string?> Name { get; init; }

    public SourceInfo<string?> FullName { get; init; }

    public List<ServicePageItem>? Children { get; init; }

    public List<string?>? Langs { get; init; }

    public LandingPageType? PageType { get; init; }

    public JObject Metadata { get; set; } = new JObject();
}
