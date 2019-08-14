// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using YamlDotNet.Serialization;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.YamlSerialization;
    using Newtonsoft.Json.Serialization;
    using Newtonsoft.Json;

    public class XRefMap : IXRefContainer
    {
        [YamlMember(Alias = "sorted")]
        [JsonProperty("sorted")]
        public bool? Sorted { get; set; }

        [YamlMember(Alias = "hrefUpdated")]
        [JsonProperty("hrefUpdated")]
        public bool? HrefUpdated { get; set; }

        [YamlMember(Alias = "baseUrl")]
        [JsonProperty("baseUrl")]
        public string BaseUrl { get; set; }

        [YamlMember(Alias = "tags")]
        [JsonProperty("tags")]
        public List<string> Tags { get; set; }

        [YamlMember(Alias = "redirections")]
        [JsonProperty("redirections")]
        public List<XRefMapRedirection> Redirections { get; set; }

        [YamlMember(Alias = "references")]
        [JsonProperty("references")]
        public List<XRefSpec> References { get; set; }

        [ExtensibleMember]
        [JsonExtensionData]
        public Dictionary<string, object> Others { get; set; } = new Dictionary<string, object>();

        public void Sort()
        {
            if (Sorted == true)
            {
                return;
            }
            if (References != null)
            {
                References.Sort(XRefSpecUidComparer.Instance);
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
                if (!Uri.TryCreate(r.Href, UriKind.RelativeOrAbsolute, out Uri uri))
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

        [YamlIgnore]
        [JsonIgnore]
        public bool IsEmbeddedRedirections => false;

        public IEnumerable<XRefMapRedirection> GetRedirections() =>
            Redirections ?? Enumerable.Empty<XRefMapRedirection>();

        public IXRefContainerReader GetReader()
        {
            return new BasicXRefMapReader(this);
        }
    }
}
