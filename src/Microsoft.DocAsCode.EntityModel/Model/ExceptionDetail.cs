namespace Microsoft.DocAsCode.EntityModel
{
    public class CrefInfo
    {
        [YamlDotNet.Serialization.YamlMember(Alias = "type")]
        public string Type { get; set; }

        [YamlDotNet.Serialization.YamlMember(Alias = "description")]
        public string Description { get; set; }
    }
}
