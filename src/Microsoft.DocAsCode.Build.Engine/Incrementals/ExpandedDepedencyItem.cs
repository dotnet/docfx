// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine.Incrementals.Outputs
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.DocAsCode.Common;

    using Newtonsoft.Json;

    public sealed class ExpandedDependencyItem : IEquatable<ExpandedDependencyItem>
    {
        private static readonly StringComparer Comparer = FilePathComparer.OSPlatformSensitiveStringComparer;

        [JsonProperty("from")]
        public string From { get; }

        [JsonProperty("to")]
        public string To { get; }

        [JsonProperty("type")]
        public string Type { get; }

        [JsonConstructor]
        public ExpandedDependencyItem(string from, string to, string type)
        {
            From = from;
            To = to;
            Type = type;
        }

        public static ExpandedDependencyItem ConvertFrom(DependencyItem item)
        {
            return new ExpandedDependencyItem(item.From.Value, item.To.Value, item.Type);
        }

        public ExpandedDependencyItem ChangeFrom(string from)
        {
            return new ExpandedDependencyItem(from, this.To, this.Type);
        }

        public ExpandedDependencyItem ChangeTo(string to)
        {
            return new ExpandedDependencyItem(this.From, to, this.Type);
        }

        public ExpandedDependencyItem ChangeType(string type)
        {
            return new ExpandedDependencyItem(this.From, this.To, type);
        }

        public bool Equals(ExpandedDependencyItem dp)
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
                Type == dp.Type;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ExpandedDependencyItem);
        }

        public override string ToString()
        {
            return $"From: {From}, To: {To}, Type: {Type}.";
        }

        public override int GetHashCode()
        {
            return Comparer.GetHashCode(From) ^ (Comparer.GetHashCode(To) >> 1) ^ (Type.GetHashCode() >> 2);
        }
    }
}
