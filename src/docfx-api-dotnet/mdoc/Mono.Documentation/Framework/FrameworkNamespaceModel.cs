using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Mono.Documentation.Framework
{
    public class FrameworkNamespaceModel
    {
        public string Name { get; }
        public List<FrameworkTypeModel> Types { get; } = new List<FrameworkTypeModel>();

        public FrameworkNamespaceModel(XNode node)
        {
            var element = node as XElement;
            Name = element.Attributes(XName.Get("Name")).First().Value;
            var children = element.Elements();
            foreach (XElement rawType in children)
            {
                Types.Add(new FrameworkTypeModel(rawType));
            }
        }
    }
}