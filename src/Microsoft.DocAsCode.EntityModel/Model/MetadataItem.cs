namespace Microsoft.DocAsCode.EntityModel
{
    using System;
    using System.Collections.Generic;
    using YamlDotNet.Serialization;

    public class MetadataItem : ICloneable
    {
        [YamlIgnore]
        public bool IsInvalid { get; set; }

        [YamlIgnore]
        public string RawComment { get; set; }

        [YamlMember(Alias = "id")]
        public string Name { get; set; }

        [YamlMember(Alias = "href")]
        public string Href { get; set; }

        [YamlMember(Alias = "language")]
        public SyntaxLanguage Language { get; set; }

        [YamlMember(Alias = "name")]
        public SortedList<SyntaxLanguage, string> DisplayNames { get; set; }

        [YamlMember(Alias = "qualifiedName")]
        public SortedList<SyntaxLanguage, string> DisplayQualifiedNames { get; set; }

        [YamlMember(Alias = "parent")]
        public MetadataItem Parent { get; set; }

        [YamlMember(Alias = "type")]
        public MemberType Type { get; set; }

        [YamlMember(Alias = "assemblies")]
        public List<string> AssemblyNameList { get; set; }

        [YamlMember(Alias = "namespace")]
        public string NamespaceName { get; set; }

        [YamlMember(Alias = "source")]
        public SourceDetail Source { get; set; }

        [YamlMember(Alias = "documentation")]
        public SourceDetail Documentation { get; set; }

        public List<LayoutItem> Layout { get; set; }

        [YamlMember(Alias = "summary")]
        public string Summary { get; set; }

        [YamlMember(Alias = "remarks")]
        public string Remarks { get; set; }

        [YamlMember(Alias = "example")]
        public string Example { get; set; }

        [YamlMember(Alias = "syntax")]
        public SyntaxDetail Syntax { get; set; }

        [YamlMember(Alias = "overridden")]
        public string Overridden { get; set; }

        [YamlMember(Alias = "exceptions")]
        public List<CrefInfo> Exceptions { get; set; }

        [YamlMember(Alias = "see")]
        public List<CrefInfo> Sees { get; set; }

        [YamlMember(Alias = "seealso")]
        public List<CrefInfo> SeeAlsos { get; set; }

        [YamlMember(Alias = "inheritance")]
        public List<string> Inheritance { get; set; }

        [YamlMember(Alias = "implements")]
        public List<string> Implements { get; set; }

        [YamlMember(Alias = "inheritedMembers")]
        public List<string> InheritedMembers { get; set; }

        [YamlMember(Alias = "items")]
        public List<MetadataItem> Items { get; set; }

        [YamlMember(Alias = "references")]
        public Dictionary<string, ReferenceItem> References { get; set; }

        public override string ToString()
        {
            return Type + ": " + Name;
        }

        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }
}
