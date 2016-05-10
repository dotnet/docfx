// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections.Generic;

    using YamlDotNet.Serialization;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.YamlSerialization;

    public class XRefMap
    {
        [YamlMember(Alias = "sorted")]
        public bool? Sorted { get; set; }

        [YamlMember(Alias = "hrefUpdated")]
        public bool? HrefUpdated { get; set; }

        [YamlMember(Alias = "baseUrl")]
        public string BaseUrl { get; set; }

        [YamlMember(Alias = "redirections")]
        public List<XRefMapRedirection> Redirections { get; set; }

        [YamlMember(Alias = "references")]
        public List<XRefSpec> References { get; set; }

        [ExtensibleMember]
        public Dictionary<string, object> Others { get; set; } = new Dictionary<string, object>();

        public void Sort()
        {
            if (Sorted == true)
            {
                return;
            }
            if (References != null)
            {
                References.Sort(XRefSpecComparer.Instance);
            }
            Sorted = true;
        }

        public void UpdateHref(Uri baseUri)
        {
            if (HrefUpdated == true)
            {
                return;
            }
            if (References == null)
            {
                return;
            }
            var list = new List<XRefSpec>(References.Count);
            foreach (var r in References)
            {
                Uri uri;
                if (!Uri.TryCreate(r.Href, UriKind.RelativeOrAbsolute, out uri))
                {
                    Logger.LogWarning($"Bad uri in xref map: {r.Href}");
                    continue;
                }
                if (uri.IsAbsoluteUri)
                {
                    list.Add(r);
                }
                else
                {
                    list.Add(new XRefSpec(r) { Href = new Uri(baseUri, uri).AbsoluteUri });
                }
            }
            References = list;
            HrefUpdated = true;
        }

        public virtual XRefSpec Find(string uid)
        {
            if (References == null)
            {
                return null;
            }
            if (Sorted == true)
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
