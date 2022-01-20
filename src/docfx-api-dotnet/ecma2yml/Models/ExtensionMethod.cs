namespace ECMA2Yaml.Models
{
    public class ExtensionMethod
    {
        public string Uid { get; set; }
        public string MemberDocId { get; set; }
        public string TargetDocId { get; set; }
        public string ParentTypeString { get; set; }
        public ReflectionItem ParentType { get; set; }
    }
}
