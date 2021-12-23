using Newtonsoft.Json;
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace ECMA2Yaml.Models.SDP
{
    public class DelegateSDPModel : ItemSDPModelBase
    {
        [JsonIgnore]
        [YamlIgnore]
        public override string YamlMime { get; } = "YamlMime:NetDelegate";

        [JsonProperty("typeParameters")]
        [YamlMember(Alias = "typeParameters")]
        public IEnumerable<TypeParameterSDPModel> TypeParameters { get; set; }

        [JsonProperty("returnsWithMoniker")]
        [YamlMember(Alias = "returnsWithMoniker")]
        public ReturnValue ReturnsWithMoniker { get; set; }

        [JsonProperty("parameters")]
        [YamlMember(Alias = "parameters")]
        public IEnumerable<ParameterReference> Parameters { get; set; }

        [JsonProperty("inheritances")]
        [YamlMember(Alias = "inheritances")]
        public IEnumerable<string> Inheritances { get; set; }

        [JsonProperty("extensionMethods")]
        [YamlMember(Alias = "extensionMethods")]
        public IEnumerable<TypeMemberLink> ExtensionMethods { get; set; }

        [JsonProperty("isNotClsCompliant")]
        [YamlMember(Alias = "isNotClsCompliant")]
        public bool? IsNotClsCompliant { get; set; }

        [JsonProperty("altCompliant")]
        [YamlMember(Alias = "altCompliant")]
        public string AltCompliant { get; set; }
    }
}
