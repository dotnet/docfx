using System.Collections.Generic;

namespace ECMA2Yaml.Models
{
    //http://docs.go-mono.com/?link=man%3amdoc(5)
    public class Docs
    {
        public string Summary { get; set; }
        public string Remarks { get; set; }
        public string Examples { get; set; }
        public string ThreadSafety { get; set; }
        public ThreadSafety ThreadSafetyInfo { get; set; }
        public List<string> AltMemberCommentIds { get; set; }
        public List<TypedContent> Exceptions { get; set; }
        public List<TypedContent> Permissions { get; set; }
        public List<RelatedTag> Related { get; set; }
        public Dictionary<string, string> Parameters { get; set; }
        public Dictionary<string, string> TypeParameters { get; set; }
        public Dictionary<string, string> AdditionalNotes { get; set; }
        public string Returns { get; set; }
        public string Since { get; set; }
        public string Value { get; set; }
        public string AltCompliant { get; set; }
        public bool InternalOnly { get; set; }
        public InheritDoc Inheritdoc { get; set; }
    }
}
