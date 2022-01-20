using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Mono.Documentation.Framework
{
    public class FrameworkTypeModel
    {
        public string Name { get; }
        public string Id { get; }
        public List<string> Members { get; } = new List<string>();

        public FrameworkTypeModel(XElement element)
        {
            Name = element.Attributes(XName.Get("Name")).First().Value;
            Id = element.Attributes(XName.Get("Id")).First().Value;

            var children = element.Elements();
            foreach (XElement rawMember in children)
            {
                Members.Add(rawMember.Attributes(XName.Get("Id")).First().Value);
            }
        }
    }
}