// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.RestApi.Swagger
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    using Microsoft.DocAsCode.YamlSerialization;

    [Serializable]
    public class TagItemObject
    {
        /// <summary>
        /// Required. The name of the tag.
        /// </summary>
        [YamlMember(Alias = "name")]
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// A short description for the tag. GFM syntax can be used for rich text representation.
        /// </summary>
        [YamlMember(Alias = "description")]
        [JsonProperty("description")]
        public string Description { get; set; }

        /// <summary>
        /// Define the bookmark id for the tag. It's extensions to the Swagger Schema, which MUST begin with 'x-'.
        /// </summary>
        [YamlMember(Alias = "x-bookmark-id")]
        [JsonProperty("x-bookmark-id")]
        public string BookmarkId { get; set; }

        [ExtensibleMember]
        [JsonExtensionData]
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }
}
