using Newtonsoft.Json;
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace ECMA2Yaml.Models.SDP
{
    public class ParameterReference : TypeReference
    {
        [JsonProperty("namesWithMoniker")]
        [YamlMember(Alias = "namesWithMoniker")]
        public List<VersionedString> NamesWithMoniker { get; set; }
    }
}
