// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Plugins
{
    using System.Collections.Immutable;

    public class MarkupResult
    {
        public string Html { get; set; }
        public ImmutableDictionary<string, object> YamlHeader { get; set; } = ImmutableDictionary<string, object>.Empty;
        public ImmutableArray<string> LinkToFiles { get; set; } = ImmutableArray<string>.Empty;
        public ImmutableHashSet<string> LinkToUids { get; set; } = ImmutableHashSet<string>.Empty;
        public ImmutableArray<string> Dependency { get; set; } = ImmutableArray<string>.Empty;
        public ImmutableDictionary<string, ImmutableList<LinkSourceInfo>> UidLinkSources { get; set; } = ImmutableDictionary<string, ImmutableList<LinkSourceInfo>>.Empty;
        public ImmutableDictionary<string, ImmutableList<LinkSourceInfo>> FileLinkSources { get; set; } = ImmutableDictionary<string, ImmutableList<LinkSourceInfo>>.Empty;

        public MarkupResult Clone()
        {
            return (MarkupResult)MemberwiseClone();
        }
    }
}