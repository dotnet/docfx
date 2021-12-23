using System.Collections.Generic;

namespace ECMA2Yaml.Models
{
    public class Namespace : ReflectionItem
    {
        public List<Type> Types { get; set; }

        public override void Build(ECMAStore store)
        {
            Id = Name;
        }
    }
}
