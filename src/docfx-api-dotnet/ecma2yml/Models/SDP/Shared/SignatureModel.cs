using Newtonsoft.Json;
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace ECMA2Yaml.Models.SDP
{
    public class SignatureModel
    {
        [JsonProperty("lang")]
        [YamlMember(Alias = "lang")]
        public string Lang { get; set; }

        [JsonProperty("value")]
        [YamlMember(Alias = "value")]
        public string Value { get; set; }
    }

    public class VersionedSignatureModel
    {
        [JsonProperty("lang")]
        [YamlMember(Alias = "lang")]
        public string Lang { get; set; }

        [JsonProperty("values")]
        [YamlMember(Alias = "values")]
        public List<VersionedString> Values { get; set; }
    }
}
