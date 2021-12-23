using Newtonsoft.Json;
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace ECMA2Yaml.Models.SDP
{
    public class EnumSDPModel : ItemSDPModelBase
    {
        [JsonIgnore]
        [YamlIgnore]
        public override string YamlMime { get; } = "YamlMime:NetEnum";

        [JsonProperty("inheritancesWithMoniker")]
        [YamlMember(Alias = "inheritancesWithMoniker")]
        public IEnumerable<VersionedCollection<string>> InheritancesWithMoniker { get; set; }

        [JsonProperty("isFlags")]
        [YamlMember(Alias = "isFlags")]
        public bool IsFlags { get; set; }

        [JsonProperty("fields")]
        [YamlMember(Alias = "fields")]
        public IEnumerable<EnumField> Fields { get; set; }
    }
}
