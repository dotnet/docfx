using Newtonsoft.Json;
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace ECMA2Yaml.Models.SDP
{
    public class TOCRootYamlModel
    {
        [JsonProperty("items")]
        [YamlMember(Alias = "items")]
        public List<TOCNodeYamlModel> Items { get; set; }

        [JsonProperty("splitItemsBy")]
        [YamlMember(Alias = "splitItemsBy")]
        public string SplitItemsBy { get; } = nameof(TOCNodeYamlModel.Name).ToLower();
    }
}
