using Newtonsoft.Json;

namespace ECMA2Yaml
{
    public class ECMA2YamlRepoConfig
    {
        [JsonProperty("id")]
        public string BatchId { get; set; }

        [JsonProperty("SourceXmlFolder")]
        public string SourceXmlFolder { get; set; }

        [JsonProperty("OutputYamlFolder")]
        public string OutputYamlFolder { get; set; }

        [JsonProperty("Flatten")]
        public bool Flatten { get; set; }

        [JsonProperty("UWP")]
        public bool UWP { get; set; }
    }
}
