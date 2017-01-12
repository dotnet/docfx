// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DfmHttpService
{
    using Newtonsoft.Json;

    internal class CommandMessage
    {
        [JsonProperty("name")]
        public CommandName Name { get; set; }

        [JsonProperty("filePath")]
        public string FilePath { get; set; }

        [JsonProperty("workspacePath")]
        public string WorkspacePath { get; set; }

        [JsonProperty("documentation")]
        public string Documentation { get; set; }
    }
}