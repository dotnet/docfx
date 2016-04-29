// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using YamlDotNet.Serialization;

    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.YamlSerialization;

    public class XRefMap
    {
        [YamlMember(Alias = "sorted")]
        public bool Sorted { get; set; }

        [YamlMember(Alias = "baseUrl")]
        public string BaseUrl { get; set; }

        [YamlMember(Alias = "references")]
        public List<XRefSpec> References { get; set; }

        [ExtensibleMember]
        public Dictionary<string, object> Others { get; set; } = new Dictionary<string, object>();

        public void Sort()
        {
            if (Sorted)
            {
                return;
            }
            if (References != null)
            {
                References.Sort(XRefSpecComparer.Instance);
            }
            Sorted = true;
        }

        public void UpdateHref(Uri baseuri)
        {
            if (References == null)
            {
                return;
            }
            References = (from r in References
                          let uri = new Uri(r.Href)
                          select uri.IsAbsoluteUri ? r : new XRefSpec(r) { Href = new Uri(baseuri, uri).AbsoluteUri }).ToList();
        }

        public XRefSpec Find(string uid)
        {
            if (References == null)
            {
                return null;
            }
            if (Sorted)
            {
                var index = References.BinarySearch(new XRefSpec { Uid = uid }, XRefSpecComparer.Instance);
                if (index >= 0)
                {
                    return References[index];
                }
                return null;
            }
            else
            {
                return References.Find(x => x.Uid == uid);
            }
        }

        private sealed class XRefSpecComparer : Comparer<XRefSpec>
        {
            public static readonly XRefSpecComparer Instance = new XRefSpecComparer();

            public override int Compare(XRefSpec x, XRefSpec y)
            {
                return string.Compare(x.Uid, y.Uid);
            }
        }
    }
}
