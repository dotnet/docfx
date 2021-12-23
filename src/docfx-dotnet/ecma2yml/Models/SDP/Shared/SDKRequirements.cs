
using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace ECMA2Yaml.Models
{
    public class SDKRequirements
    {
        [JsonProperty("name")]
        [YamlMember(Alias = "name")]
        public string Name { get; set; }
        [JsonProperty("url")]
        [YamlMember(Alias = "url")]
        public string Url { get; set; }
    }
}
