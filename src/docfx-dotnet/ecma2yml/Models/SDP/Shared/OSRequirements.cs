
using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace ECMA2Yaml.Models
{
    public class OSRequirements
    {
        [JsonProperty("name")]
        [YamlMember(Alias = "name")]
        public string Name { get; set; }
        [JsonProperty("minVersion")]
        [YamlMember(Alias = "minVersion")]
        public string MinVer { get; set; }
    }
}
