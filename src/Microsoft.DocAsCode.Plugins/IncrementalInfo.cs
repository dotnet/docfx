// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Plugins
{
    using System.Collections.Generic;

    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    public class IncrementalInfo
    {
        private readonly Dictionary<string, IncrementalStatus> _processors = new Dictionary<string, IncrementalStatus>();

        [YamlMember(Alias = "status")]
        [JsonProperty("status")]
        public IncrementalStatus Status { get; } = new IncrementalStatus();

        [YamlMember(Alias = "processors")]
        [JsonProperty("processors")]
        public IReadOnlyDictionary<string, IncrementalStatus> Processors
        {
            get { return _processors; }
        }

        public void ReportStatus(bool canIncremental, string details = null)
        {
            Status.CanIncremental = canIncremental;
            Status.Details = details;
        }

        public void ReportProcessorStatus(string processor, bool canIncremental, string details = null)
        {
            IncrementalStatus status;
            if (!_processors.TryGetValue(processor, out status))
            {
                _processors[processor] = status = new IncrementalStatus();
            }
            status.CanIncremental = canIncremental;
            status.Details = details;
        }
    }

    public class IncrementalStatus
    {
        [YamlMember(Alias = "canIncremental")]
        [JsonProperty("can_incremental")]
        public bool CanIncremental { get; set; }

        [YamlMember(Alias = "details")]
        [JsonProperty("details")]
        public string Details { get; set; }
    }
}
