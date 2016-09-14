﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine.Incrementals
{
    using System;

    using Newtonsoft.Json;

    public sealed class DependencyItem : IEquatable<DependencyItem>
    {
        [JsonProperty("from")]
        public string From { get; }

        [JsonProperty("to")]
        public string To { get; }

        [JsonProperty("reportedBy")]
        public string ReportedBy { get; }

        [JsonProperty("type")]
        public string Type { get; }

        [JsonConstructor]
        public DependencyItem(string from, string to, string reportedBy, string type)
        {
            From = from;
            To = to;
            ReportedBy = reportedBy;
            Type = type;
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
