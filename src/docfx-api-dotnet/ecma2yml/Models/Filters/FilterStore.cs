using System.Collections.Generic;

namespace ECMA2Yaml.Models
{
    public class FilterStore
    {
        public List<AttributeFilter> AttributeFilters { get; set; }
        public List<TypeFilter> TypeFilters { get; set; }
        public List<MemberFilter> MemberFilters { get; set; }
    }
}
