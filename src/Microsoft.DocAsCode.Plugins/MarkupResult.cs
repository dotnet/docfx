// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Plugins
{
    using System.Collections.Immutable;

    public class MarkupResult
    {
        public string Html { get; set; }
        public ImmutableDictionary<string, object> YamlHeader { get; set; }
        public ImmutableArray<string> LinkToFiles { get; set; }
        public ImmutableArray<string> LinkToUids { get; set; }
    }
}
