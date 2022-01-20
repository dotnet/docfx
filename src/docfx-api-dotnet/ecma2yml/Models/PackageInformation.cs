using System.Collections.Generic;

namespace ECMA2Yaml.Models
{
    public class PackageInformation
    {
        public string Name { get; set; }

        public string Version { get; set; }

        public string Feed { get; set; }
    }

    // moniker1
    //   |__ assembly1 => package1
    //   |__ assembly2 => package2
    public class PackageInformationMapping : Dictionary<string, Dictionary<string, PackageInformation>>
    {
        public PackageInformationMapping Merge(PackageInformationMapping pkgInfoMapping)
        {
            foreach (var kvp in pkgInfoMapping)
            {
                if (!this.ContainsKey(kvp.Key))
                {
                    this.Add(kvp.Key, kvp.Value);
                }
            }

            return this;
        }

        public string TryGetPackageDisplayString(string moniker, string assemblyName)
        {
            if (!string.IsNullOrEmpty(moniker) && !string.IsNullOrEmpty(assemblyName))
            {
                if (this.ContainsKey(moniker) && this[moniker].ContainsKey(assemblyName))
                {
                    var package = this[moniker][assemblyName];
                    return $"{package.Name} v{package.Version}";
                }
            }

            return null;
        }
    }
}
