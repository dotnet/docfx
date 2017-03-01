// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine.Incrementals
{
    using System;

    using Newtonsoft.Json;

    public sealed class DependencyItem : IEquatable<DependencyItem>
    {
        [JsonProperty("from")]
        public DependencyItemSourceInfo From { get; private set; }

        [JsonProperty("to")]
        public DependencyItemSourceInfo To { get; private set; }

        [JsonProperty("reportedBy")]
        public DependencyItemSourceInfo ReportedBy { get; private set; }

        [JsonProperty("type")]
        public string Type { get; }

        [JsonConstructor]
        public DependencyItem(DependencyItemSourceInfo from, DependencyItemSourceInfo to, DependencyItemSourceInfo reportedBy, string type)
        {
            From = from;
            To = to;
            ReportedBy = reportedBy;
            Type = type;
        }

        public DependencyItem ChangeFrom(DependencyItemSourceInfo f)
        {
            return new DependencyItem(f, this.To, this.ReportedBy, this.Type);
        }

        public DependencyItem ChangeTo(DependencyItemSourceInfo t)
        {
            return new DependencyItem(this.From, t, this.ReportedBy, this.Type);
        }

        public DependencyItem ChangeReportedBy(DependencyItemSourceInfo r)
        {
            return new DependencyItem(this.From, this.To, r, this.Type);
        }

        public bool Equals(DependencyItem dp)
        {
            if (dp == null)
            {
                return false;
            }
            if (ReferenceEquals(this, dp))
            {
                return true;
            }
            return From == dp.From &&
                To == dp.To &&
                ReportedBy == dp.ReportedBy &&
                Type == dp.Type;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as DependencyItem);
        }

        public override string ToString()
        {
            return $"From: {From}, To: {To}, ReportedBy: {ReportedBy}, Type: {Type}.";
        }

        public override int GetHashCode()
        {
            return From.GetHashCode() ^ (To.GetHashCode() >> 1) ^ (ReportedBy.GetHashCode() << 1) ^ (Type.GetHashCode() >> 2);
        }
    }
}
