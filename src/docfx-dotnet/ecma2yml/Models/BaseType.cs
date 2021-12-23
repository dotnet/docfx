using System.Collections.Generic;
using System.Linq;

namespace ECMA2Yaml.Models
{
    public class BaseTypeArgument
    {
        public string TypeParamName { get; set; }
        public string Value { get; set; }
    }

    public class BaseType : ReflectionItem
    {
        public List<BaseTypeArgument> TypeArguments { get; set; }

        public override void Build(ECMAStore store)
        {
            if (TypeArguments?.Count > 0)
            {
                var genericPart = string.Format("<{0}>", string.Join(",", TypeArguments.Select(ta => ta.Value)));
                var uidPart = "`" + TypeArguments.Count;
                Id = Name.Replace(genericPart, uidPart);
            }
            else
            {
                Id = Name;
            }
            Id = Id.Replace('+', '.');
        }
    }
}
