using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace ECMA2Yaml.Models
{
    public class ParameterBase
    {
        public List<VersionedString> VersionedNames { get; set; }
        public string Name { get; set; }
        public string RefType { get; set; }
        public string Description { get; set; }
        public int? Index { get; set; }

        public virtual void LoadFromXElement(XElement p)
        {
            Name = p.Attribute("Name")?.Value;
            VersionedNames = new List<VersionedString>() { new VersionedString(ECMALoader.LoadFrameworkAlternate(p), Name) };
            RefType = p.Attribute("RefType")?.Value;
            var indexStr = p.Attribute("Index")?.Value;
            if (!string.IsNullOrEmpty(indexStr) && int.TryParse(indexStr, out int i))
            {
                Index = i;
            }
        }

        public static List<PType> LoadVersionedParameters<PType>(IEnumerable<XElement> pElements) where PType : ParameterBase, new()
        {
            if (pElements == null)
            {
                return null;
            }
            var pList = pElements?.Select(pEle =>
            {
                PType p = new PType();
                p.LoadFromXElement(pEle);
                return p;
            }).ToList();

            if (!pList.All(p => p.Index.HasValue))
            {
                return pList;
            }
            return pList.GroupBy(p => p.Index.Value)
                    .OrderBy(g => g.Key) // index 0,1,2,3,4...
                    .Select(g =>
                    {
                        //choose the first name as default name
                        var rval = g.First();
                        if (g.Count() == 1)
                        {
                            //clear monikers if there's only 1 version of this parameter
                            rval.VersionedNames.First().Monikers = null;
                        }
                        else
                        {
                            rval.VersionedNames = g.SelectMany(p => p.VersionedNames).ToList();
                        }
                        return rval;
                    })
                    .ToList();
        }
    }

    public class Parameter : ParameterBase, IEquatable<Parameter>
    {
        public string Type { get; set; }
        public string OriginalTypeString { get; set; }

        public bool Equals(Parameter other)
        {
            return other != null && other.Type == this.Type && other.OriginalTypeString == this.OriginalTypeString;
        }

        public override void LoadFromXElement(XElement p)
        {
            base.LoadFromXElement(p);
            var typeStr = p.Attribute("Type")?.Value;
            OriginalTypeString = typeStr;
            Type = typeStr?.TrimEnd('&');
        }
    }

    public class TypeParameter : ParameterBase
    {
        public bool? IsContravariant { get; set; }
        public bool? IsCovariant { get; set; }

        public override void LoadFromXElement(XElement p)
        {
            base.LoadFromXElement(p);
            var parameterAttributes = p.Element("Constraints")?.Elements("ParameterAttribute")?.ToArray();
            IsContravariant = parameterAttributes?.Any(pa => pa.Value == "Contravariant") == true ? true : (bool?)null;
            IsCovariant = parameterAttributes?.Any(pa => pa.Value == "Covariant") == true ? true : (bool?)null;
        }
    }
}
