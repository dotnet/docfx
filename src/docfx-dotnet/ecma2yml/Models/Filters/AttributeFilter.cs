using System.Collections.Generic;
using System.Linq;

namespace ECMA2Yaml.Models
{
    public class AttributeFilter
    {
        public string Namespace { get; set; }
        public Dictionary<string, bool> TypeFilters { get; set; }
        public bool DefaultValue { get; set; }

        public bool? Filter(Type t)
        {
            if (t.Parent.Name == Namespace)
            {
                if (TypeFilters.ContainsKey(t.Name))
                {
                    return TypeFilters[t.Name];
                }
                return DefaultValue;
            }
            return null;
        }

        public bool? Filter(string fqn)
        {
            if (fqn.StartsWith(Namespace))
            {
                foreach (var tf in TypeFilters)
                {
                    if (fqn.EndsWith(tf.Key) && fqn == (Namespace + '.' + tf.Key))
                    {
                        return tf.Value;
                    }
                }
                fqn = fqn.Substring(Namespace.Length).TrimStart('.');
                if (!fqn.Contains('.'))
                {
                    return DefaultValue;
                }
                // does not return default value here if we can't tell which part is namespace. 
            }
            return null;
        }
    }
}
