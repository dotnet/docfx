// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.Swagger
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    using Microsoft.DocAsCode.YamlSerialization;

    [Serializable]
    public class SwaggerModel
    {
        /// <summary>
        /// Required. Provides metadata about the API. The metadata can be used by the clients if needed.
        /// </summary>
        [YamlMember(Alias = "info")]
        [JsonProperty("info")]
        public InfoObject Info { get; set; }

        /// <summary>
        /// The host (name or ip) serving the API. This MUST be the host only and does not include the scheme nor sub-paths. It MAY include a port. If the host is not included, the host serving the documentation is to be used (including the port). The host does not support path templating.
        /// </summary>
        [YamlMember(Alias = "host")]
        [JsonProperty("host")]
        public string Host { get; set; }

        /// <summary>
        /// The base path on which the API is served, which is relative to the host. If it is not included, the API is served directly under the host. The value MUST start with a leading slash (/). The basePath does not support path templating.
        /// </summary>
        [YamlMember(Alias = "basePath")]
        [JsonProperty("basePath")]
        public string BasePath { get; set; }
        
        /// <summary>
        /// Required. The available paths and operations for the API.
        /// </summary>
        [YamlMember(Alias = "paths")]
        [JsonProperty("paths")]
        public PathsObject Paths { get; set; }

        [YamlMember(Alias = "description")]
        [JsonProperty("description")]
        public string Description { get; set; }

        [YamlMember(Alias = "summary")]
        [JsonProperty("summary")]
        public string Summary { get; set; }

        [ExtensibleMember]
        [JsonExtensionData]
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }
}
