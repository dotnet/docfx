// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine.Incrementals
{
    using System;

    using Newtonsoft.Json;

    public class ReferenceItem : IEquatable<ReferenceItem>
    {
        [JsonProperty("reference")]
        public DependencyItemSourceInfo Reference { get; }

        [JsonProperty("file")]
        public string File { get; }

        [JsonProperty("reportedBy")]
        public string ReportedBy { get; }

        [JsonConstructor]
        public ReferenceItem(DependencyItemSourceInfo reference, string file, string reportedBy)
        {
            Reference = reference;
            File = file;
            ReportedBy = reportedBy;
        }

        public bool Equals(ReferenceItem other)
        {
            if (other == null)
            {
                return false;
            }
            if (ReferenceEquals(this, other))
            {
                return true;
            }
            return Reference == other.Reference &&
                File == other.File &&
                ReportedBy == other.ReportedBy;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ReferenceItem);
        }

        public override int GetHashCode()
        {
            return Reference.GetHashCode() ^ (File.GetHashCode() >> 1) ^ (ReportedBy.GetHashCode() >> 2);
        }
    }
}
