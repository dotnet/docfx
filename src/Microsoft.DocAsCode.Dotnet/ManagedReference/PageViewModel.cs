// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.DataContracts.ManagedReference
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.YamlSerialization;

    [Serializable]
    public class PageViewModel
    {
        [YamlMember(Alias = "items")]
        [JsonProperty("items")]
        public List<ItemViewModel> Items { get; set; } = new List<ItemViewModel>();

        [YamlMember(Alias = "references")]
        [JsonProperty("references")]
        [UniqueIdentityReferenceIgnore]
        [MarkdownContentIgnore]
        public List<ReferenceViewModel> References { get; set; } = new List<ReferenceViewModel>();

        [YamlMember(Alias = "shouldSkipMarkup")]
        [JsonProperty("shouldSkipMarkup")]
        public bool ShouldSkipMarkup { get; set; }

        [ExtensibleMember]
        [JsonExtensionData]
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }
}
