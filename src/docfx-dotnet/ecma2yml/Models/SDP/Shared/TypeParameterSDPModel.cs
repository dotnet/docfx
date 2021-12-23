using Newtonsoft.Json;
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace ECMA2Yaml.Models.SDP
{
    public class TypeParameterSDPModel
    {
        [JsonProperty("description")]
        [YamlMember(Alias = "description")]
        public string Description { get; set; }

        [JsonProperty("name")]
        [YamlMember(Alias = "name")]
        public string Name { get; set; }

        [JsonProperty("constraints")]
        [YamlMember(Alias = "constraints")]
        public IEnumerable<Constraint> Constraints { get; set; }

        [JsonProperty("isContravariant")]
        [YamlMember(Alias = "isContravariant")]
        public bool? IsContravariant { get; set; }

        [JsonProperty("isCovariant")]
        [YamlMember(Alias = "isCovariant")]
        public bool? IsCovariant { get; set; }

        public class Constraint
        {
            [JsonProperty("parameterAttribute")]
            [YamlMember(Alias = "parameterAttribute")]
            public string ParameterAttribute { get; set; }

            [JsonProperty("baseTypeName")]
            [YamlMember(Alias = "baseTypeName")]
            public string BaseTypeName { get; set; }
        }
    }
}
