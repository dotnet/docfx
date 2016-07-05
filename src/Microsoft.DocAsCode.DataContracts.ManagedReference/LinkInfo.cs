// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.DataContracts.ManagedReference
{
    using System;
    using YamlDotNet.Serialization;

    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.Utility.EntityMergers;
    using Newtonsoft.Json;

    [Serializable]
    public class LinkInfo
    {
        [YamlMember(Alias = "linkType")]
        [JsonProperty("linkType")]
        public LinkType LinkType { get; set; }

        [YamlMember(Alias = "linkId")]
        [MergeOption(MergeOption.MergeKey)]
        [JsonProperty("linkId")]
        public string LinkId { get; set; }

        [YamlMember(Alias = Constants.PropertyName.CommentId)]
        [JsonProperty(Constants.PropertyName.CommentId)]
        public string CommentId { get; set; }

        [YamlMember(Alias = "altText")]
        [JsonProperty("altText")]
        public string AltText { get; set; }
    }

    [Serializable]
    public enum LinkType
    {
        CRef,
        HRef,
    }
}
