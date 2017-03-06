// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine.Incrementals
{
    using System;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;

    using Newtonsoft.Json;

    public class DependencyItemSourceInfo : IEquatable<DependencyItemSourceInfo>
    {
        private StringComparer ValueComparer => SourceType == DependencyItemSourceType.File ? FilePathComparer.OSPlatformSensitiveStringComparer : StringComparer.Ordinal;

        [JsonProperty("sourceType")]
        public string SourceType { get; }

        [JsonProperty("value")]
        public string Value { get; }

        [JsonConstructor]
        public DependencyItemSourceInfo(string sourceType, string value)
        {
            SourceType = sourceType;
            Value = value;
        }

        public DependencyItemSourceInfo ChangeSourceType(string type)
        {
            return new DependencyItemSourceInfo(type, this.Value);
        }

        public DependencyItemSourceInfo ChangeValue(string value)
        {
            return new DependencyItemSourceInfo(this.SourceType, value);
        }

        public bool Equals(DependencyItemSourceInfo other)
        {
            if (other == null)
            {
                return false;
            }
            if (ReferenceEquals(this, other))
            {
                return true;
            }
            return ValueComparer.Equals(Value, other.Value) &&
                SourceType == other.SourceType;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as DependencyItemSourceInfo);
        }

        public override string ToString()
        {
            return $"SourceType: {SourceType}, Value: {Value}.";
        }

        public override int GetHashCode()
        {
            return ValueComparer.GetHashCode(Value) ^ (SourceType.GetHashCode() >> 1);
        }

        public static implicit operator DependencyItemSourceInfo(string info)
        {
            return info == null ? null : new DependencyItemSourceInfo(DependencyItemSourceType.File, info);
        }

        public static bool operator ==(DependencyItemSourceInfo a, DependencyItemSourceInfo b)
        {
            return object.Equals(a, b);
        }

        public static bool operator !=(DependencyItemSourceInfo a, DependencyItemSourceInfo b)
        {
            return !(a == b);
        }
    }
}
