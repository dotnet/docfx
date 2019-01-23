// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Plugins
{
    using System.Collections.Generic;

    using Newtonsoft.Json;

    public class IncrementalInfo
    {
        private readonly object _syncRoot = new object();
        private readonly Dictionary<string, IncrementalStatus> _processors = new Dictionary<string, IncrementalStatus>();

        [JsonProperty("status")]
        public IncrementalStatus Status { get; } = new IncrementalStatus();

        [JsonProperty("processors")]
        public IReadOnlyDictionary<string, IncrementalStatus> Processors => _processors;

        public void ReportStatus(bool canIncremental, IncrementalPhase incrementalPhase, string details = null, string fullBuildReasonCode = null)
        {
            lock (_syncRoot)
            {
                Status.CanIncremental = canIncremental;
                Status.Details = details;
                Status.IncrementalPhase = incrementalPhase;
                Status.FullBuildReasonCode = fullBuildReasonCode;
            }
        }

        public void ReportProcessorStatus(string processor, bool canIncremental, string details = null, string fullBuildReasonCode = null)
        {
            lock (_syncRoot)
            {
                if (!_processors.TryGetValue(processor, out IncrementalStatus status))
                {
                    _processors[processor] = status = new IncrementalStatus();
                }
                status.CanIncremental = canIncremental;
                status.Details = details;
                status.FullBuildReasonCode = fullBuildReasonCode;
            }
        }

        public void ReportProcessorFileCount(string processor, long totalFileCount, long skippedFileCount)
        {
            lock (_syncRoot)
            {
                var status = _processors[processor];
                status.TotalFileCount = totalFileCount;
                status.SkippedFileCount = skippedFileCount;
            }
        }
    }

    public class IncrementalStatus
    {
        [JsonProperty("can_incremental")]
        public bool CanIncremental { get; set; }

        [JsonProperty("details")]
        public string Details { get; set; }

        [JsonProperty("incrementalPhase")]
        public IncrementalPhase IncrementalPhase { get; set; }

        [JsonProperty("total_file_count")]
        public long TotalFileCount { get; set; }

        [JsonProperty("skipped_file_count")]
        public long SkippedFileCount { get; set; }

        [JsonProperty("full_build_reason_code")]
        public string FullBuildReasonCode { get; set; }
    }
}
