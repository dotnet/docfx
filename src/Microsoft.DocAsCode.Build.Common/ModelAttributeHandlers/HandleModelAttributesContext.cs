// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Common
{
    using System.Collections.Generic;
    using System.Collections.Immutable;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;

    public class HandleModelAttributesContext
    {
        public IHostService Host { get; set; }
        public bool SkipMarkup { get; set; }
        public bool EnableContentPlaceholder { get; set; }
        public string PlaceholderContent { get; set; }
        public bool ContainsPlaceholder { get; set; }

        public FileAndType FileAndType { get; set; }
        public HashSet<string> LinkToFiles { get; set; } = new HashSet<string>(FilePathComparer.OSPlatformSensitiveStringComparer);
        public HashSet<string> LinkToUids { get; set; } = new HashSet<string>();
        public List<UidDefinition> Uids { get; set; } = new List<UidDefinition>();
        public Dictionary<string, List<LinkSourceInfo>> UidLinkSources { get; set; } = new Dictionary<string, List<LinkSourceInfo>>();
        public Dictionary<string, List<LinkSourceInfo>> FileLinkSources { get; set; } = new Dictionary<string, List<LinkSourceInfo>>();
    }
}
