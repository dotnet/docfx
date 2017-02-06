// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Plugins
{
    using System.Collections.Generic;

    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    public class IncrementalInfo
    {
        private readonly object _syncRoot = new object();
        private readonly Dictionary<string, IncrementalStatus> _processors = new Dictionary<string, IncrementalStatus>();

        [YamlMember(Alias = "status")]
        [JsonProperty("status")]
        public IncrementalStatus Status { get; } = new IncrementalStatus();

        [YamlMember(Alias = "processors")]
        [JsonProperty("processors")]
        public IReadOnlyDictionary<string, IncrementalStatus> Processors => _processors;

        public void ReportStatus(bool canIncremental, IncrementalPhase incrementalPhase, string details = null)
        {
            lock (_syncRoot)
            {
                Status.CanIncremental = canIncremental;
                Status.Details = details;
                Status.IncrementalPhase = incrementalPhase;
            }
        }

        public void ReportProcessorStatus(string processor, bool canIncremental, string details = null)
        {
            lock (_syncRoot)
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
    }

    public class IncrementalStatus
    {
        [YamlMember(Alias = "canIncremental")]
        [JsonProperty("can_incremental")]
        public bool CanIncremental { get; set; }

        [YamlMember(Alias = "details")]
        [JsonProperty("details")]
        public string Details { get; set; }

        [YamlMember(Alias = "incrementalPhase")]
        [JsonProperty("incrementalPhase")]
        public IncrementalPhase IncrementalPhase { get; set; }
    }
}
