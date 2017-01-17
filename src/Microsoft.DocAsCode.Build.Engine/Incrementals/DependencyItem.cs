﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine.Incrementals
{
    using System;

    using Microsoft.DocAsCode.Common;

    using Newtonsoft.Json;

    public sealed class DependencyItem : IEquatable<DependencyItem>
    {
        private static readonly StringComparer Comparer = FilePathComparer.OSPlatformSensitiveStringComparer;

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
            return Comparer.Equals(From, dp.From) &&
                Comparer.Equals(To, dp.To) &&
                Comparer.Equals(ReportedBy, dp.ReportedBy) &&
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
            return Comparer.GetHashCode(From) ^ (Comparer.GetHashCode(To) >> 1) ^ (Comparer.GetHashCode(ReportedBy) << 1) ^ (Type.GetHashCode() >> 2);
        }
    }
}
