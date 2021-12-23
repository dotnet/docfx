using System.Collections.Generic;

namespace ECMA2Yaml.Models
{
    public class FrameworkIndex
    {
        public Dictionary<string, List<string>> DocIdToFrameworkDict { get; set; }

        public Dictionary<string, Dictionary<string, AssemblyInfo>> FrameworkAssemblies { get; set; }

        public HashSet<string> AllFrameworks { get; set; }
    }
}
