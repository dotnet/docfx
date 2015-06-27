namespace Microsoft.DocAsCode.EntityModel
{
    public class ItemType
    {
        [YamlDotNet.Serialization.YamlMember(Alias = "name")]
        public string Name { get; set; }
        [YamlDotNet.Serialization.YamlMember(Alias = "description")]
        public string Description { get; set; }
    }
}
