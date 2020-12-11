// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal record ServicePageModel(
        SourceInfo<string?> Name, SourceInfo<string?> FullName, List<ServicePageItem> Children, List<string?>? Langs, LandingPageType? PageType)
    {
        public JObject Metadata { get; set; } = new JObject();
    }
}
