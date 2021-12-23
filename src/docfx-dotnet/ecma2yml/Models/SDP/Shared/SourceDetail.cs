using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace ECMA2Yaml.Models.SDP
{
    public class SourceDetail
    {
        [JsonProperty("path")]
        [YamlMember(Alias = "path")]
        public string RelativePath { get; set; }

        [JsonProperty("branch")]
        [YamlMember(Alias = "branch")]
        public string RemoteBranch { get; set; }

        [JsonProperty("repo")]
        [YamlMember(Alias = "repo")]
        public string RemoteRepositoryUrl { get; set; }
    }
}
