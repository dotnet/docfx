using Newtonsoft.Json;
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace ECMA2Yaml.Models.SDP
{
    public class TOCNodeYamlModel
    {
        [JsonProperty("uid")]
        [YamlMember(Alias = "uid")]
        public string Uid { get; set; }

        [JsonProperty("name")]
        [YamlMember(Alias = "name")]
        public string Name { get; set; }

        [JsonProperty("displayName")]
        [YamlMember(Alias = "displayName")]
        public string DisplayName { get; set; }

        [JsonProperty("items")]
        [YamlMember(Alias = "items")]
        public List<TOCNodeYamlModel> Items { get; set; }

        [JsonProperty("href")]
        [YamlMember(Alias = "href")]
        public string Href { get; set; }

        [JsonProperty("type")]
        [YamlMember(Alias = "type")]
        public string Type { get; set; }

        [JsonProperty("monikers")]
        [YamlMember(Alias = "monikers")]
        public IEnumerable<string> Monikers { get; set; }

        [JsonProperty("isEii")]
        [YamlMember(Alias = "isEii")]
        public bool IsEII { get; set; }
    }
}
