using System.Collections.Generic;

namespace ECMA2Yaml.Models
{
    public class ECMAAttribute
    {
        public string TypeFullName { get; set; }
        public string Declaration { get; set; }
        public bool Visible { get; set; }
        public HashSet<string> Monikers { get; set; }
        public Dictionary<string, string> NamesPerLanguage { get; set; }
    }
}
