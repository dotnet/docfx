using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace ECMA2Yaml.Models
{
    public class MemberFilter : BasicFilter
    {
        public MemberFilter(XElement element) : base(element)
        {
        }

        public TypeFilter Parent { get; set; }
        public Dictionary<string, bool> ParameterAttributeFilters { get; set; }
        public Dictionary<string, bool> ReturnValueAttributeFilters { get; set; }
        public bool? Filter(Member m)
        {
            var parentResult = Parent.Filter(m.Parent as Type);
            if (parentResult == null || !parentResult.Value)
            {
                return parentResult;
            }

            if (Name == "*" || m.Name == Name)
            {
                bool expose = Expose;
                if (m.Attributes?.Count > 0 && AttributeFilters?.Count > 0)
                {
                    foreach (var attr in m.Attributes.Where(a => AttributeFilters.ContainsKey(a.Declaration)))
                    {
                        expose = expose && AttributeFilters[attr.Declaration];
                    }
                }
                return expose;
            }

            return null;
        }
    }
}
