// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Tools.AzureMarkdownRewriterTool
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json;

    public class AzureVideoRawInformation
    {
        [JsonProperty("namePropertyName")]
        public string NamePropertyName { get; set; }

        [JsonProperty("displayNamePropertyName")]
        public string DisplayNamePropertyName { get; set; }

        [JsonProperty("collectionNamePropertyName")]
        public string CollectionNamePropertyName { get; set; }

        [JsonProperty("pollingInterval")]
        public int PollingInterval { get; set; }

        [JsonProperty("fastPollingInterval")]
        public int FastPollingInterval { get; set; }

        [JsonProperty("slowPollingInterval")]
        public int SlowPollingInterval { get; set; }

        [JsonProperty("isComplete")]
        public bool IsComplete { get; set; }

        [JsonProperty("partialErrors")]
        public string PartialErrors { get; set; }

        [JsonProperty("data")]
        public List<AzureVideoDataItem> Data { get; set; }
    }

    public class AzureVideoDataItem
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("submissionStatus")]
        public string SubmissionStatus { get; set; }

        [JsonProperty("published")]
        public DateTime Published { get; set; }

        [JsonProperty("acomUrl")]
        public string AcomUrl { get; set; }

        [JsonProperty("channel9PlayerUrl")]
        public string Channel9PlayerUrl { get; set; }

        [JsonProperty("owner")]
        public string Owner { get; set; }

        [JsonProperty("order")]
        public string Order { get; set; }

        [JsonProperty("channel9Url")]
        public string Channel9Url { get; set; }
    }
}
