using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TripleCrownValidation.DependencyMap
{
    public class DependencyItem
    {
        [JsonProperty("from_file_path")]
        public string FromFilePath { get; set; }

        [JsonProperty("to_file_path")]
        public string ToFilePath { get; set; }

        [JsonProperty("dependency_type")]
        public string DependencyType { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }
    }
}
