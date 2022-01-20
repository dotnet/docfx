using System;

namespace ECMA2Yaml.Models
{
    public class AssemblyInfo : IEquatable<AssemblyInfo>
    {
        public string Name { get; set; }
        public string Version { get; set; }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }
            if (obj is AssemblyInfo other)
            {
                return Equals(other);
            }
            return base.Equals(obj);
        }

        public bool Equals(AssemblyInfo other)
        {
            return Name == other.Name && Version == other.Version;
        }

        public override int GetHashCode()
        {
            var id = Name + Version;
            return id.GetHashCode();
        }
    }
}
