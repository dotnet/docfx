namespace Microsoft.DocAsCode.EntityModel
{
    using System.Collections.Generic;
    using System.Linq;
    using YamlDotNet.Serialization;

    public class ReferenceItem
    {
        [YamlMember(Alias = "name")]
        public Dictionary<SyntaxLanguage, List<LinkItem>> Parts { get; set; }

        [YamlMember(Alias = "type")]
        public MemberType? Type { get; set; }

        [YamlMember(Alias = "summary")]
        public string Summary { get; set; }

        [YamlMember(Alias = "isDefinition")]
        public bool? IsDefinition { get; set; }

        [YamlMember(Alias = "definition")]
        public string Definition { get; set; }

        [YamlMember(Alias = "parent")]
        public string Parent { get; set; }

        public ReferenceItem Clone()
        {
            var result = (ReferenceItem)MemberwiseClone();
            if (Parts != null)
            {
                var dict = new Dictionary<SyntaxLanguage, List<LinkItem>>(Parts.Count);
                foreach (var item in Parts)
                {
                    dict.Add(item.Key, (from x in item.Value select x.Clone()).ToList());
                }
                result.Parts = dict;
            }

            return result;
        }
    }

    public class LinkItem
    {
        [YamlMember(Alias = "id")]
        public string Name { get; set; }

        [YamlMember(Alias = "name")]
        public string DisplayName { get; set; }

        [YamlMember(Alias = "qualifiedName")]
        public string DisplayQualifiedNames { get; set; }

        /// <summary>
        /// The external path for current source if it is not locally available
        /// </summary>
        [YamlMember(Alias = "isExternal")]
        public bool IsExternalPath { get; set; }

        /// <summary>
        /// The url path for current source, should be resolved at some late stage
        /// </summary>
        [YamlMember(Alias = "href")]
        public string Href { get; set; }

        public LinkItem Clone()
        {
            return (LinkItem)MemberwiseClone();
        }
    }
}
