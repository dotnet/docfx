// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.DataContracts.ManagedReference
{
    using System;

    using Microsoft.DocAsCode.DataContracts.Common;

    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    [Serializable]
    public class AdditionalNotes
    {
        [JsonProperty("caller")]
        [YamlMember(Alias = "caller")]
        [MarkdownContent]
        public string Caller { get; set; }

        [JsonProperty("implementer")]
        [YamlMember(Alias = "implementer")]
        [MarkdownContent]
        public string Implementer { get; set; }

        [JsonProperty("inheritor")]
        [YamlMember(Alias = "inheritor")]
        [MarkdownContent]
        public string Inheritor { get; set; }
    }
}
