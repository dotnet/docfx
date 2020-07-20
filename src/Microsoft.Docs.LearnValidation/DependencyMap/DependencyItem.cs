// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Docs.LearnValidation
{
    public class DependencyItem
    {
        [JsonProperty("from_file_path")]
        public string FromFilePath { get; set; }

        [JsonProperty("to_file_path")]
        public string ToFilePath { get; set; }

        [JsonProperty("dependency_type")]
        public string DependencyType { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }
    }
}
