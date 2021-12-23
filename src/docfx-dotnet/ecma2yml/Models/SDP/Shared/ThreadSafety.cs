using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace ECMA2Yaml.Models.SDP
{
    public class ThreadSafety
    {
        [JsonProperty("customizedContent")]
        [YamlMember(Alias = "customizedContent")]
        public string CustomizedContent { get; set; }

        [JsonProperty("isSupported")]
        [YamlMember(Alias = "isSupported")]
        public bool? IsSupported { get; set; }

        [JsonProperty("memberScope")]
        [YamlMember(Alias = "memberScope")]
        public string MemberScope { get; set; }
    }
}
