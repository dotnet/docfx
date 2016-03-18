// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.RestApi.Swagger
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    using Microsoft.DocAsCode.YamlSerialization;

    /// <summary>
    /// Info object
    /// </summary>
    [Serializable]
    public class InfoObject
    {
        /// <summary>
        /// Required. The title of the application.
        /// </summary>
        [YamlMember(Alias = "title")]
        [JsonProperty("title")]
        public string Title { get; set; }

        /// <summary>
        /// Required. Provides the version of the application API
        /// </summary>
        [YamlMember(Alias = "version")]
        [JsonProperty("version")]
        public string Version { get; set; }

        [ExtensibleMember]
        [JsonExtensionData]
        public Dictionary<string, object> PatternedObjects { get; set; } = new Dictionary<string, object>();
    }
}
