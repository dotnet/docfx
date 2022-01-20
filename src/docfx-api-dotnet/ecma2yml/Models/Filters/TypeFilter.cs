using System.Linq;
using System.Xml.Linq;

namespace ECMA2Yaml.Models
{
    public class TypeFilter : BasicFilter
    {
        public TypeFilter(XElement element) : base(element)
        {
        }

        public string Namespace { get; set; }

        public bool? Filter(Type t)
        {
            if ((Namespace == "*" || t.Parent.Name == Namespace) && (Name == "*" || t.Name == Name))
            {
                bool expose = Expose;
                if (t.Attributes?.Count > 0 && AttributeFilters?.Count > 0)
                {
                    foreach (var attr in t.Attributes.Where(a => AttributeFilters.ContainsKey(a.Declaration)))
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
