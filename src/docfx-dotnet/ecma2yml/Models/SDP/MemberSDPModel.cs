using Newtonsoft.Json;
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace ECMA2Yaml.Models.SDP
{
    public class MemberSDPModel : ItemSDPModelBase
    {
        [JsonIgnore]
        [YamlIgnore]
        public override string YamlMime { get; } = "YamlMime:NetMember";

        [JsonProperty("typeParameters")]
        [YamlMember(Alias = "typeParameters")]
        public IEnumerable<TypeParameterSDPModel> TypeParameters { get; set; }

        [JsonProperty("returnsWithMoniker")]
        [YamlMember(Alias = "returnsWithMoniker")]
        public ReturnValue ReturnsWithMoniker { get; set; }

        [JsonProperty("parameters")]
        [YamlMember(Alias = "parameters")]
        public IEnumerable<ParameterReference> Parameters { get; set; }

        [JsonProperty("threadSafety")]
        [YamlMember(Alias = "threadSafety")]
        public ThreadSafety ThreadSafety { get; set; }

        [JsonProperty("permissions")]
        [YamlMember(Alias = "permissions")]
        public IEnumerable<TypeReference> Permissions { get; set; }

        [JsonProperty("exceptions")]
        [YamlMember(Alias = "exceptions")]
        public IEnumerable<TypeReference> Exceptions { get; set; }

        [JsonProperty("implementsWithMoniker")]
        [YamlMember(Alias = "implementsWithMoniker")]
        public IEnumerable<VersionedString> ImplementsWithMoniker { get; set; }

        /// <summary>
        /// A collection of all monikers that apply to this member's implements.
        /// </summary>
        [JsonProperty("implementsMonikers")]
        [YamlMember(Alias = "implementsMonikers")]
        public IEnumerable<string> ImplementsMonikers { get; set; }

        [JsonProperty("isNotClsCompliant")]
        [YamlMember(Alias = "isNotClsCompliant")]
        public bool? IsNotClsCompliant { get; set; }

        [JsonProperty("altCompliant")]
        [YamlMember(Alias = "altCompliant")]
        public string AltCompliant { get; set; }

        [JsonProperty("type")]
        [YamlMember(Alias = "type")]
        public string Type { get; set; }
    }
}
