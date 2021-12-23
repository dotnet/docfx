using Newtonsoft.Json;
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace ECMA2Yaml.Models.SDP
{
    public class TypeMemberLink
    {
        [JsonProperty("uid")]
        [YamlMember(Alias = "uid")]
        public string Uid { get; set; }

        [JsonProperty("inheritedFrom")]
        [YamlMember(Alias = "inheritedFrom")]
        public string InheritedFrom { get; set; }

        [JsonProperty("monikers")]
        [YamlMember(Alias = "monikers")]
        public IEnumerable<string> Monikers { get; set; }

        [JsonProperty("crossInheritdocsUid")]
        [YamlMember(Alias = "crossInheritdocsUid")]
        public string CrossInheritdocUid { get; set; }
    }
}
