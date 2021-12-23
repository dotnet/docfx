
using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace ECMA2Yaml.Models
{
    public class DeviceFamily
    {
        [JsonProperty("name")]
        [YamlMember(Alias = "name")]
        public string Name { get; set; }
        [JsonProperty("version")]
        [YamlMember(Alias = "version")]
        public string Version { get; set; }
    }
}
