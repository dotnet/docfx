namespace Microsoft.DocAsCode.EntityModel
{
    public class ExceptionDetail
    {
        [YamlDotNet.Serialization.YamlMember(Alias = "type")]
        public string Type { get; set; }

        [YamlDotNet.Serialization.YamlMember(Alias = "description")]
        public string Description { get; set; }
    }
}
