// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Common
{
    using System.Collections.Generic;
    using System.IO;

    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.YamlSerialization;

    public class OverwriteDocumentModel
    {
        [ExtensibleMember]
        [JsonExtensionData]
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// The uid for this overwrite document, as defined in YAML header
        /// </summary>
        [YamlMember(Alias = Constants.PropertyName.Uid)]
        [JsonProperty(Constants.PropertyName.Uid)]
        public string Uid { get; set; }

        /// <summary>
        /// The markdown content from the overwrite document
        /// </summary>
        [YamlMember(Alias = Constants.PropertyName.Conceptual)]
        [JsonProperty(Constants.PropertyName.Conceptual)]
        public string Conceptual { get; set; }

        /// <summary>
        /// The details for current overwrite document, containing the start/end line numbers, file path, and git info.
        /// </summary>
        [YamlMember(Alias = Constants.PropertyName.Documentation)]
        [JsonProperty(Constants.PropertyName.Documentation)]
        public SourceDetail Documentation { get; set; }

        public T ConvertTo<T>() where T : class
        {
            using (var sw = new StringWriter())
            {
                YamlUtility.Serialize(sw, this);
                using (var sr = new StringReader(sw.ToString()))
                {
                    return YamlUtility.Deserialize<T>(sr);
                }
            }
        }
    }
}
