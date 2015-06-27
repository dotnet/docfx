namespace Microsoft.DocAsCode.EntityModel
{
    public class ApiParameter
    {
        [YamlDotNet.Serialization.YamlMember(Alias = "id")]
        public string Name { get; set; }

        [YamlDotNet.Serialization.YamlMember(Alias = "type")]
        public string Type { get; set; }

        [YamlDotNet.Serialization.YamlMember(Alias = "description")]
        public string Description { get; set; }
    }
}
