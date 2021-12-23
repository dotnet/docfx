using Newtonsoft.Json;
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace ECMA2Yaml.Models.SDP
{
    public class NamespaceSDPModel : ItemSDPModelBase
    {
        [JsonIgnore]
        [YamlIgnore]
        public override string YamlMime { get; } = "YamlMime:NetNamespace";

        [JsonIgnore]
        [YamlIgnore]
        new public string Namespace { get; set; }

        [JsonProperty("delegates")]
        [YamlMember(Alias = "delegates")]
        public IEnumerable<NamespaceTypeLink> Delegates { get; set; }

        [JsonProperty("classes")]
        [YamlMember(Alias = "classes")]
        public IEnumerable<NamespaceTypeLink> Classes { get; set; }

        [JsonProperty("enums")]
        [YamlMember(Alias = "enums")]
        public IEnumerable<NamespaceTypeLink> Enums { get; set; }

        [JsonProperty("interfaces")]
        [YamlMember(Alias = "interfaces")]
        public IEnumerable<NamespaceTypeLink> Interfaces { get; set; }

        [JsonProperty("structs")]
        [YamlMember(Alias = "structs")]
        public IEnumerable<NamespaceTypeLink> Structs { get; set; }
    }
}
