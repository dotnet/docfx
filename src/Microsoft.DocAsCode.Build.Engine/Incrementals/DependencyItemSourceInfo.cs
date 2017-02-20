namespace Microsoft.DocAsCode.Build.Engine.Incrementals
{
    using System;

    using Microsoft.DocAsCode.Common;

    using Newtonsoft.Json;

    public class DependencyItemSourceInfo : IEquatable<DependencyItemSourceInfo>
    {
        private StringComparer Comparer
        {
            get
            {
                return SourceType == DependencyItemSourceType.File ? FilePathComparer.OSPlatformSensitiveStringComparer : StringComparer.Ordinal;
            }
        }

        [JsonProperty("sourceType")]
        public DependencyItemSourceType SourceType { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }

        public DependencyItemSourceInfo ChangeSourceType(DependencyItemSourceType type)
        {
            return new DependencyItemSourceInfo { SourceType = type, Value = this.Value };
        }

        public DependencyItemSourceInfo ChangeValue(string value)
        {
            return new DependencyItemSourceInfo { SourceType = this.SourceType, Value = value };
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
            return Comparer.Equals(Value, other.Value) &&
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
            return Comparer.GetHashCode(Value) ^ (SourceType.GetHashCode() >> 1);
        }

        public static implicit operator DependencyItemSourceInfo(string info)
        {
            return info == null ? null : new DependencyItemSourceInfo { Value = info, SourceType = DependencyItemSourceType.File };
        }
    }

    public enum DependencyItemSourceType
    {
        File,
        Reference,
    }
}
